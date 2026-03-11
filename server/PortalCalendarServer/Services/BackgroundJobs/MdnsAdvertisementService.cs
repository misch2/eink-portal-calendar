using Makaretu.Dns;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Advertises the Portal Calendar server via mDNS/DNS-SD so that
/// ESP32 clients can discover it using <c>MDNS.queryService("portal-calendar", "tcp")</c>.
///
/// Each mDNS response contains only the A record for the local IP address that
/// belongs to the same subnet as the querier, so clients on different networks
/// (VirtualBox, WSL, real LAN, …) each see the correct server address.
/// </summary>
public class MdnsAdvertisementService : BackgroundService
{
    private const string ServiceType = "_portal-calendar._tcp";

    private readonly ILogger<MdnsAdvertisementService> _logger;
    private readonly IServer _server;
    private readonly IHostApplicationLifetime _lifetime;

    private MulticastService? _mdns;

    public MdnsAdvertisementService(
        ILogger<MdnsAdvertisementService> logger,
        IServer server,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _server = server;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the server has fully started so that IServerAddressesFeature is populated.
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _lifetime.ApplicationStarted.Register(() => started.SetResult());
        await Task.WhenAny(started.Task, Task.Delay(Timeout.Infinite, stoppingToken));

        if (stoppingToken.IsCancellationRequested)
            return;

        var port = GetHttpPort();
        if (port is null)
        {
            _logger.LogWarning("mDNS advertisement skipped: could not determine HTTP listen port from Kestrel configuration");
            return;
        }

        var nicAddresses = GetLanNicAddresses();
        if (nicAddresses.Count == 0)
        {
            _logger.LogWarning("mDNS advertisement skipped: no suitable LAN IPv4 addresses found");
            return;
        }

        foreach (var entry in nicAddresses)
        {
            _logger.LogInformation(
                "mDNS: will advertise {Service} on {NicName} ({Ip}/{PrefixLength}, port {Port})",
                ServiceType, entry.NicName, entry.Address, entry.PrefixLength, port.Value);
        }

        var hostname = Dns.GetHostName();
        var hostDomainName = new DomainName(hostname + ".local");
        var serviceQsn = new DomainName(ServiceType + ".local");
        var instanceFqn = new DomainName(hostname + "." + ServiceType + ".local");

        try
        {
            _mdns = new MulticastService { UseIpv6 = false };

            _mdns.QueryReceived += (_, args) =>
            {
                var query = args.Message;

                foreach (var question in query.Questions)
                {
                    if (!IsOurQuestion(question, serviceQsn, instanceFqn, hostDomainName))
                        continue;

                    // Determine which local IP to put in the A record:
                    // pick the one whose subnet contains the querier's address.
                    var remoteIp = args.RemoteEndPoint.Address;
                    var localIp = FindMatchingLocalAddress(remoteIp, nicAddresses);
                    if (localIp is null)
                    {
                        _logger.LogDebug(
                            "mDNS: ignoring query from {RemoteIp} — no matching local subnet",
                            remoteIp);
                        continue;
                    }

                    var answer = BuildAnswer(
                        question, serviceQsn, instanceFqn, hostDomainName,
                        localIp, (ushort)port.Value);

                    if (answer is not null)
                    {
                        _mdns.SendAnswer(answer, args, checkDuplicate: false);

                        _logger.LogDebug(
                            "mDNS: answered {QuestionName} from {RemoteIp} with {LocalIp}:{Port}",
                            question.Name, remoteIp, localIp, port.Value);
                    }
                }
            };

            _mdns.Start();

            // Keep running until the application stops
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "mDNS advertisement failed");
        }
        finally
        {
            _mdns?.Dispose();
            _mdns = null;
            _logger.LogInformation("mDNS advertisement stopped");
        }
    }

    /// <summary>
    /// Returns true if this DNS question is something we should answer
    /// (PTR browse for the service type, SRV/TXT for our instance, or A for our host).
    /// </summary>
    private static bool IsOurQuestion(
        Question question,
        DomainName serviceQsn,
        DomainName instanceFqn,
        DomainName hostDomainName)
    {
        // PTR: "_portal-calendar._tcp.local" → service type browse
        if (question.Type == DnsType.PTR && question.Name == serviceQsn)
            return true;

        // PTR: DNS-SD meta-query "_services._dns-sd._udp.local"
        if (question.Type == DnsType.PTR &&
            question.Name == new DomainName("_services._dns-sd._udp.local"))
            return true;

        // SRV or TXT for our instance
        if ((question.Type == DnsType.SRV || question.Type == DnsType.TXT || question.Type == DnsType.ANY)
            && question.Name == instanceFqn)
            return true;

        // A record for our hostname
        if ((question.Type == DnsType.A || question.Type == DnsType.ANY)
            && question.Name == hostDomainName)
            return true;

        return false;
    }

    /// <summary>
    /// Builds the appropriate mDNS answer for a given question type,
    /// always including the correct A record as an additional record.
    /// </summary>
    private static Message? BuildAnswer(
        Question question,
        DomainName serviceQsn,
        DomainName instanceFqn,
        DomainName hostDomainName,
        IPAddress localIp,
        ushort port)
    {
        var answer = new Message { QR = true, AA = true };
        var ttl = TimeSpan.FromMinutes(2);

        // PTR browse for "_services._dns-sd._udp.local"
        if (question.Type == DnsType.PTR &&
            question.Name == new DomainName("_services._dns-sd._udp.local"))
        {
            answer.Answers.Add(new PTRRecord
            {
                Name = question.Name,
                DomainName = serviceQsn,
                TTL = ttl
            });
            return answer;
        }

        // PTR browse for the service type → our instance
        if (question.Type == DnsType.PTR && question.Name == serviceQsn)
        {
            answer.Answers.Add(new PTRRecord
            {
                Name = serviceQsn,
                DomainName = instanceFqn,
                TTL = ttl
            });

            // Include SRV + TXT + A as additional records so the client
            // can resolve everything in a single round-trip.
            answer.AdditionalRecords.Add(new SRVRecord
            {
                Name = instanceFqn,
                Target = hostDomainName,
                Port = port,
                TTL = ttl
            });
            answer.AdditionalRecords.Add(new TXTRecord
            {
                Name = instanceFqn,
                TTL = ttl
            });
            answer.AdditionalRecords.Add(new ARecord
            {
                Name = hostDomainName,
                Address = localIp,
                TTL = ttl
            });
            return answer;
        }

        // SRV / TXT / ANY for our instance
        if (question.Name == instanceFqn)
        {
            if (question.Type is DnsType.SRV or DnsType.ANY)
            {
                answer.Answers.Add(new SRVRecord
                {
                    Name = instanceFqn,
                    Target = hostDomainName,
                    Port = port,
                    TTL = ttl
                });
            }
            if (question.Type is DnsType.TXT or DnsType.ANY)
            {
                answer.Answers.Add(new TXTRecord
                {
                    Name = instanceFqn,
                    TTL = ttl
                });
            }
            answer.AdditionalRecords.Add(new ARecord
            {
                Name = hostDomainName,
                Address = localIp,
                TTL = ttl
            });
            return answer;
        }

        // A for our hostname
        if (question.Name == hostDomainName &&
            question.Type is DnsType.A or DnsType.ANY)
        {
            answer.Answers.Add(new ARecord
            {
                Name = hostDomainName,
                Address = localIp,
                TTL = ttl
            });
            return answer;
        }

        return null;
    }

    /// <summary>
    /// Finds the local address whose subnet contains <paramref name="remoteIp"/>.
    /// </summary>
    private static IPAddress? FindMatchingLocalAddress(
        IPAddress remoteIp, List<NicAddress> nicAddresses)
    {
        if (remoteIp.AddressFamily != AddressFamily.InterNetwork)
            return null;

        var remoteBytes = remoteIp.GetAddressBytes();

        foreach (var entry in nicAddresses)
        {
            var localBytes = entry.Address.GetAddressBytes();
            var mask = PrefixLengthToMask(entry.PrefixLength);

            bool sameSubnet = true;
            for (int i = 0; i < 4; i++)
            {
                if ((remoteBytes[i] & mask[i]) != (localBytes[i] & mask[i]))
                {
                    sameSubnet = false;
                    break;
                }
            }

            if (sameSubnet)
                return entry.Address;
        }

        return null;
    }

    private static byte[] PrefixLengthToMask(int prefixLength)
    {
        var mask = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            if (prefixLength >= 8)
            {
                mask[i] = 0xFF;
                prefixLength -= 8;
            }
            else
            {
                mask[i] = (byte)(0xFF << (8 - prefixLength));
                prefixLength = 0;
            }
        }
        return mask;
    }

    /// <summary>
    /// Reads the HTTP port from Kestrel's <see cref="IServerAddressesFeature"/>
    /// (populated after the server starts listening).
    /// </summary>
    private int? GetHttpPort()
    {
        var addressesFeature = _server.Features.Get<IServerAddressesFeature>();
        if (addressesFeature is null)
            return null;

        foreach (var address in addressesFeature.Addresses)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) &&
                uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                return uri.Port;
            }
        }

        return null;
    }

    private record NicAddress(string NicName, IPAddress Address, int PrefixLength);

    /// <summary>
    /// Returns IPv4 addresses with subnet info from physical LAN network interfaces.
    /// </summary>
    private static List<NicAddress> GetLanNicAddresses()
    {
        var result = new List<NicAddress>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            if (nic.NetworkInterfaceType is
                NetworkInterfaceType.Loopback or
                NetworkInterfaceType.Tunnel)
                continue;

            foreach (var ua in nic.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                result.Add(new NicAddress(nic.Name, ua.Address, ua.PrefixLength));
            }
        }

        return result;
    }

}

namespace PortalCalendarServer.Infrastructure;

/// <summary>
/// Configuration provider that replaces placeholder tokens in all string values
/// loaded by preceding configuration sources, then normalizes path separators in
/// any value that contained a substituted placeholder.
/// </summary>
public class PlaceholderConfigurationProvider(IConfigurationRoot baseConfig, Dictionary<string, string> replacements)
    : ConfigurationProvider
{
    public override void Load()
    {
        foreach (var kvp in baseConfig.AsEnumerable())
        {
            if (kvp.Value is null)
                continue;

            var value = kvp.Value;
            bool substituted = false;

            foreach (var (placeholder, replacement) in replacements)
            {
                if (value.Contains(placeholder, StringComparison.Ordinal))
                {
                    value = value.Replace(placeholder, replacement, StringComparison.Ordinal);
                    substituted = true;
                }
            }

            // Normalize separators only in values that contained a path placeholder,
            // so we don't accidentally mangle non-path strings.
            if (substituted)
                value = NormalizePathSeparators(value);

            Data[kvp.Key] = value;
        }
    }

    /// <summary>
    /// Replaces both / and \ with the OS path separator, then collapses any
    /// doubled separators that can appear after joining segments.
    /// Leaves the "Data Source=" prefix in SQLite connection strings untouched.
    /// </summary>
    private static string NormalizePathSeparators(string value)
    {
        // SQLite connection strings start with "Data Source=<path>"
        // Split off the prefix so we only normalize the path portion.
        const string dataSourcePrefix = "Data Source=";
        if (value.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var pathPart = value[dataSourcePrefix.Length..];
            return dataSourcePrefix + NormalizePath(pathPart);
        }

        return NormalizePath(value);
    }

    private static string NormalizePath(string path)
    {
        // Replace both separators with the OS one, then resolve . and ..
        path = path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(path);
    }
}

public class PlaceholderConfigurationSource(IConfigurationRoot baseConfig, Dictionary<string, string> replacements)
    : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new PlaceholderConfigurationProvider(baseConfig, replacements);
}

public static class PlaceholderConfigurationExtensions
{
    /// <summary>
    /// Adds a configuration layer that replaces all occurrences of the given placeholder tokens
    /// in values already loaded by preceding sources, and normalizes path separators.
    /// </summary>
    public static IConfigurationBuilder AddPlaceholderReplacements(
        this IConfigurationBuilder builder,
        Dictionary<string, string> replacements)
    {
        var baseConfig = (builder as IConfigurationRoot) ?? builder.Build();
        builder.Add(new PlaceholderConfigurationSource(baseConfig, replacements));
        return builder;
    }
}

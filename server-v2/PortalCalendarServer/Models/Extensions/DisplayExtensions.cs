using System.Globalization;
using PortalCalendarServer.Models.POCOs.Display;

namespace PortalCalendarServer.Models
{

    public partial class Display
    {
        public bool IsDefault()
        {
            return Id == 0;
        }

        public int MissedConnects()
        {
            var missedStr = Configs?.FirstOrDefault(c => c.Name == "_missed_connects")?.Value;
            return int.TryParse(missedStr, out var missed) ? missed : 0;
        }

        public DateTime? LastVisit()
        {
            var lastVisitStr = Configs?.FirstOrDefault(c => c.Name == "_last_visit")?.Value;
            if (string.IsNullOrEmpty(lastVisitStr))
            {
                return null;
            }

            if (DateTime.TryParse(lastVisitStr, null, DateTimeStyles.RoundtripKind, out var lastVisit))
            {
                return lastVisit.ToUniversalTime();
            }

            return null;
        }

        public decimal? Voltage()
        {
            var voltageStr = Configs?.FirstOrDefault(c => c.Name == "_last_voltage")?.Value;
            if (string.IsNullOrEmpty(voltageStr))
            {
                return null;
            }

            if (decimal.TryParse(voltageStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var voltage))
            {
                return Math.Round(voltage, 2);
            }

            return null;
        }

        public decimal? BatteryPercent()
        {
            var minStr = Configs?.FirstOrDefault(c => c.Name == "_min_linear_voltage")?.Value;
            var maxStr = Configs?.FirstOrDefault(c => c.Name == "_max_linear_voltage")?.Value;
            var voltage = Voltage();
            if (string.IsNullOrEmpty(minStr) || string.IsNullOrEmpty(maxStr) || !voltage.HasValue)
            {
                return null;
            }

            if (!decimal.TryParse(minStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var min) ||
                !decimal.TryParse(maxStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
            {
                return null;
            }

            var percentage = 100 * (voltage.Value - min) / (max - min);
            percentage = Math.Max(0, Math.Min(100, percentage)); // Clip to 0-100

            return Math.Round(percentage, 1);
        }

        public WakeUpInfo NextWakeupTime()
        {
            var now = DateTime.UtcNow; // TODO: Use display timezone
            var schedule = Configs?.FirstOrDefault(c => c.Name == "wakeup_schedule")?.Value ?? string.Empty;
            DateTime nextWakeup;
            if (string.IsNullOrEmpty(schedule))
            {
                // No schedule, wake up tomorrow
                nextWakeup = now.Date.AddDays(1);
            }
            else
            {
                // TODO: Implement crontab schedule parsing
                // For now, default to next hour
                nextWakeup = now.AddHours(1).Date.AddHours(now.AddHours(1).Hour);
            }

            var sleepInSeconds = (int)(nextWakeup - now).TotalSeconds;

            return new WakeUpInfo
            {
                NextWakeup = nextWakeup,
                SleepInSeconds = sleepInSeconds,
                Schedule = schedule
            };
        }


        public int VirtualWidth()
        {
            return Rotation % 180 == 0 ? Width : Height;
        }

        public int VirtualHeight()
        {
            return Rotation % 180 == 0 ? Height : Width;
        }

        public Dictionary<string, string> CssColorMap(bool forPreview)
        {
            var variants = new Dictionary<string, (string preview, string pure)>
            {
                ["epd_black"] = ("#111111", "#000000"),
                ["epd_white"] = ("#dddddd", "#ffffff"),
                ["epd_red"] = ("#aa0000", "#ff0000"),
                ["epd_yellow"] = ("#dddd00", "#ffff00")
            };

            var colors = new Dictionary<string, string>();
            foreach (var (key, (preview, pure)) in variants)
            {
                colors[key] = forPreview ? preview : pure;
            }

            return colors;
        }
    }
}

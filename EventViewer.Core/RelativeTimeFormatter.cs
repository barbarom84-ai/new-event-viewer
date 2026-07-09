using System.Globalization;

namespace EventViewer.Core;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime? timestamp)
    {
        if (timestamp is null)
        {
            return Loc.T("Time.Unknown");
        }

        var delta = DateTime.Now - timestamp.Value;
        if (delta.TotalMinutes < 1)
        {
            return Loc.T("Time.JustNow");
        }

        if (delta.TotalMinutes < 60)
        {
            var minutes = (int)delta.TotalMinutes;
            return minutes <= 1 ? Loc.T("Time.Minute") : Loc.T("Time.Minutes", minutes);
        }

        if (delta.TotalHours < 24)
        {
            var hours = (int)delta.TotalHours;
            return hours <= 1 ? Loc.T("Time.Hour") : Loc.T("Time.Hours", hours);
        }

        if (delta.TotalDays < 7)
        {
            var days = (int)delta.TotalDays;
            return days <= 1 ? Loc.T("Time.Yesterday") : Loc.T("Time.Days", days);
        }

        return timestamp.Value.ToString("dd MMM yyyy", CultureInfo.CurrentUICulture);
    }
}

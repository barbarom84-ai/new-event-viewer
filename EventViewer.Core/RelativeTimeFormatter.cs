using System.Globalization;

namespace EventViewer.Core;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime? timestamp)
    {
        if (timestamp is null)
        {
            return "date inconnue";
        }

        var delta = DateTime.Now - timestamp.Value;
        if (delta.TotalMinutes < 1)
        {
            return "à l'instant";
        }

        if (delta.TotalMinutes < 60)
        {
            var minutes = (int)delta.TotalMinutes;
            return minutes <= 1 ? "il y a 1 min" : $"il y a {minutes} min";
        }

        if (delta.TotalHours < 24)
        {
            var hours = (int)delta.TotalHours;
            return hours <= 1 ? "il y a 1 h" : $"il y a {hours} h";
        }

        if (delta.TotalDays < 7)
        {
            var days = (int)delta.TotalDays;
            return days <= 1 ? "hier" : $"il y a {days} jours";
        }

        return timestamp.Value.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));
    }
}

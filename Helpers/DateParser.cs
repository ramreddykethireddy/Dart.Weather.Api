using System.Globalization;

namespace Dart.Weather.Api.Helpers
{
    public static class DateParser
    {
        private static readonly string[] Formats = new[]
        {
            "MM/dd/yyyy",
            "M/d/yyyy",
            "MMMM d, yyyy",
            "MMM-d-yyyy",
            "MMM-d-yy",
            "MMM-dd-yyyy",
            "yyyy-MM-dd",
            "MMMM dd, yyyy",
            "MMM dd, yyyy"
        };

        public static bool TryParse(string input, out DateTime date, out string? error)
        {
            error = null;
            date = default;
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Empty input";
                return false;
            }

            // First try invariant exact formats
            foreach (var f in Formats)
            {
                if (DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            // Try general parse with en-US culture
            if (DateTime.TryParse(input, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return true;
            }

            // Last attempt: try strict parse and catch invalid dates like April 31
            try
            {
                var dt = DateTime.Parse(input, CultureInfo.InvariantCulture, DateTimeStyles.None);
                date = dt;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
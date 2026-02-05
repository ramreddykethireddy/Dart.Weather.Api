namespace Dart.Weather.Api.Application.DTOs
{
    // DTO returned by FetchDailyNormalizedAsync and FetchArchiveJsonAsync
    public class NormalizedWeatherDto
    {
        // ISO date string (yyyy-MM-dd) requested/normalized
        public string Date { get; set; } = string.Empty;

        // Minimum temperature in Celsius (nullable)
        public double? MinTemperature { get; set; }

        // Maximum temperature in Celsius (nullable)
        public double? MaxTemperature { get; set; }

        // Precipitation sum in mm (nullable)
        public double? Precipitation { get; set; }

        // HTTP status code returned by the Open‑Meteo request
        public int StatusCode { get; set; }
    }
}

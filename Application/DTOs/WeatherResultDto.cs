namespace Dart.Weather.Api.Application.DTOs
{
    public class WeatherResultDto
    {
        public string Date { get; set; } = default!; // yyyy-MM-dd
        public double? MinTemperatureC { get; set; }
        public double? MaxTemperatureC { get; set; }
        public double? PrecipitationMm { get; set; }
        public string? Status { get; set; } // "OK" or error message
        public string? SourceFile { get; set; } // local filename if stored
    }
}
namespace Dart.Weather.Api.Application.DTOs
{
    public class WeatherResultDto
    {
        public string Date { get; set; } = default!; // yyyy-MM-dd
        public double? MinTemperature { get; set; }
        public double? MaxTemperature { get; set; }
        public double? Precipitation { get; set; }
        public string? Status { get; set; } // "OK" or error message
        public string? SourceFile { get; set; } // local filename if stored
    }
}
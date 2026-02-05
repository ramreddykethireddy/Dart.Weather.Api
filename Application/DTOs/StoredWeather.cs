namespace Dart.Weather.Api.Application.DTOs
{
    // Helper type to deserialize the minimal stored JSON
    public class StoredWeather
    {
        public string? date { get; set; }
        public double? minTemperature { get; set; }
        public double? maxTemperature { get; set; }
        public double? precipitationMm { get; set; }
    }
}

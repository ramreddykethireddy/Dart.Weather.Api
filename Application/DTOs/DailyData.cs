using System.Text.Json.Serialization;

namespace Dart.Weather.Api.Application.DTOs
{
    public class DailyData
    {
        [JsonPropertyName("time")]
        public string[] Time { get; set; } = Array.Empty<string>();

        [JsonPropertyName("temperature_2m_min")]
        public double?[] Temperature2mMin { get; set; } = Array.Empty<double?>();

        [JsonPropertyName("temperature_2m_max")]
        public double?[] Temperature2mMax { get; set; } = Array.Empty<double?>();

        [JsonPropertyName("precipitation_sum")]
        public double?[] PrecipitationSum { get; set; } = Array.Empty<double?>();
    }
}

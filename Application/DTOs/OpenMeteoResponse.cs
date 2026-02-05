using System.Text.Json.Serialization;

namespace Dart.Weather.Api.Application.DTOs
{
    public class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dart.Weather.Api.Application.DTOs;
using Microsoft.Extensions.Configuration;

namespace Dart.Weather.Api.Infrastructure
{
    public class OpenMeteoClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        // Dallas, TX coordinates (used by FetchDailyJsonAsync overload)
        private const double DefaultLatitude = 32.78;
        private const double DefaultLongitude = -96.8;

        public OpenMeteoClient(HttpClient http, IConfiguration configuration)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            // Read base URL from appsettings: "OpenMeteo": { "BaseUrl": "https://archive-api.open-meteo.com/v1" }
            var configured = configuration?.GetValue<string>("OpenMeteo:BaseUrl");
            _baseUrl = string.IsNullOrWhiteSpace(configured)
                ? "https://archive-api.open-meteo.com/v1"
                : configured.TrimEnd('/');
        }

        /// <summary>
        /// Fetches archive data for a single date and returns NormalizedWeatherDto.
        /// </summary>
        public async Task<NormalizedWeatherDto> FetchDailyJsonAsync(DateTime date)
        {
            var iso = date.ToString("yyyy-MM-dd");
            // reuse same parameters as previous implementation
            var daily = "temperature_2m_max,temperature_2m_min,precipitation_sum";
            var timezone = "auto";

            return await FetchArchiveJsonAsync(DefaultLatitude, DefaultLongitude, iso, iso, daily, timezone);
        }

        /// <summary>
        /// Fetches archive data from Open‑Meteo for supplied parameters and returns a NormalizedWeatherDto.
        /// This method does NOT throw on non-success status codes; it returns the status code inside the DTO.
        /// </summary>
        public async Task<NormalizedWeatherDto> FetchArchiveJsonAsync(double latitude, double longitude, string startDate, string endDate, string daily, string timezone)
        {
            if (string.IsNullOrWhiteSpace(startDate)) throw new ArgumentException("startDate is required", nameof(startDate));
            if (string.IsNullOrWhiteSpace(endDate)) throw new ArgumentException("endDate is required", nameof(endDate));

            // Build URL and escape query portions where appropriate
            var dailyEscaped = Uri.EscapeDataString(daily ?? string.Empty);
            var timezoneEscaped = Uri.EscapeDataString(timezone ?? "auto");

            var url = $"{_baseUrl}/archive?latitude={latitude}&longitude={longitude}&start_date={startDate}&end_date={endDate}&daily={dailyEscaped}&timezone={timezoneEscaped}";

            using var resp = await _http.GetAsync(url);
            var statusCode = (int)resp.StatusCode;
            string content = string.Empty;

            if (resp.Content != null)
                content = await resp.Content.ReadAsStringAsync();

            var openResp = DeserializeResponse(content);

            var dto = new NormalizedWeatherDto
            {
                Date = startDate,
                StatusCode = statusCode
            };

            if (openResp?.Daily?.Time != null && openResp.Daily.Time.Length > 0)
            {
                int idx = Array.IndexOf(openResp.Daily.Time, startDate);
                if (idx < 0) idx = 0;

                if (openResp.Daily.Temperature2mMin != null && idx < openResp.Daily.Temperature2mMin.Length)
                    dto.MinTemperatureC = openResp.Daily.Temperature2mMin[idx];

                if (openResp.Daily.Temperature2mMax != null && idx < openResp.Daily.Temperature2mMax.Length)
                    dto.MaxTemperatureC = openResp.Daily.Temperature2mMax[idx];

                if (openResp.Daily.PrecipitationSum != null && idx < openResp.Daily.PrecipitationSum.Length)
                    dto.PrecipitationMm = openResp.Daily.PrecipitationSum[idx];
            }

            return dto;
        }

        /// <summary>
        /// Fetches normalized weather values for a single date and returns a DTO containing:
        /// Date (normalized ISO), MinTemperatureC, MaxTemperatureC, PrecipitationMm and the HTTP StatusCode.
        /// This method does NOT throw on non-success status codes; it returns the status code inside the DTO.
        /// NOTE: This method is retained for compatibility; it performs similarly to FetchDailyJsonAsync.
        /// </summary>
        public async Task<NormalizedWeatherDto> FetchDailyNormalizedAsync(DateTime date)
        {
            var iso = date.ToString("yyyy-MM-dd");
            // reuse same parameters as FetchDailyJsonAsync
            var daily = "temperature_2m_max,temperature_2m_min,precipitation_sum";
            var timezone = "auto";

            var dailyEscaped = Uri.EscapeDataString(daily);
            var timezoneEscaped = Uri.EscapeDataString(timezone);

            var url = $"{_baseUrl}/archive?latitude={DefaultLatitude}&longitude={DefaultLongitude}&start_date={iso}&end_date={iso}&daily={dailyEscaped}&timezone={timezoneEscaped}";

            using var resp = await _http.GetAsync(url);
            var statusCode = (int)resp.StatusCode;
            string content = string.Empty;

            if (resp.Content != null)
                content = await resp.Content.ReadAsStringAsync();

            var openResp = DeserializeResponse(content);

            var dto = new NormalizedWeatherDto
            {
                Date = iso,
                StatusCode = statusCode
            };

            if (openResp?.Daily?.Time != null && openResp.Daily.Time.Length > 0)
            {
                int idx = Array.IndexOf(openResp.Daily.Time, iso);
                if (idx < 0) idx = 0;

                if (openResp.Daily.Temperature2mMin != null && idx < openResp.Daily.Temperature2mMin.Length)
                    dto.MinTemperatureC = openResp.Daily.Temperature2mMin[idx];

                if (openResp.Daily.Temperature2mMax != null && idx < openResp.Daily.Temperature2mMax.Length)
                    dto.MaxTemperatureC = openResp.Daily.Temperature2mMax[idx];

                if (openResp.Daily.PrecipitationSum != null && idx < openResp.Daily.PrecipitationSum.Length)
                    dto.PrecipitationMm = openResp.Daily.PrecipitationSum[idx];
            }

            return dto;
        }

        // --- NEW: raw JSON fetchers (return the response body) ---
        /// <summary>
        /// Returns the raw archive JSON for the supplied parameters (throws on non-success status).
        /// Used by the service layer when saving raw API responses.
        /// </summary>
        public async Task<string> FetchArchiveRawAsync(double latitude, double longitude, string startDate, string endDate, string daily, string timezone)
        {
            if (string.IsNullOrWhiteSpace(startDate)) throw new ArgumentException("startDate is required", nameof(startDate));
            if (string.IsNullOrWhiteSpace(endDate)) throw new ArgumentException("endDate is required", nameof(endDate));

            var dailyEscaped = Uri.EscapeDataString(daily ?? string.Empty);
            var timezoneEscaped = Uri.EscapeDataString(timezone ?? "auto");

            var url = $"{_baseUrl}/archive?latitude={latitude}&longitude={longitude}&start_date={startDate}&end_date={endDate}&daily={dailyEscaped}&timezone={timezoneEscaped}";

            using var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Returns the raw archive JSON for a single date at the default Dallas coordinates.
        /// </summary>
        public async Task<string> FetchDailyRawJsonAsync(DateTime date)
        {
            var iso = date.ToString("yyyy-MM-dd");
            var daily = "temperature_2m_max,temperature_2m_min,precipitation_sum";
            var timezone = "auto";
            return await FetchArchiveRawAsync(DefaultLatitude, DefaultLongitude, iso, iso, daily, timezone);
        }
        // --- end new methods ---

        public static OpenMeteoResponse DeserializeResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new OpenMeteoResponse();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var resp = new OpenMeteoResponse();

                if (!root.TryGetProperty("daily", out var daily) || daily.ValueKind != JsonValueKind.Object)
                    return resp;

                var dailyData = new DailyData();

                // time -> string[]
                if (daily.TryGetProperty("time", out var timeEl) && timeEl.ValueKind == JsonValueKind.Array)
                {
                    var times = new List<string>();
                    foreach (var item in timeEl.EnumerateArray())
                        times.Add(item.GetString() ?? string.Empty);
                    dailyData.Time = times.ToArray();
                }

                // helper to parse nullable double arrays
                static double?[] ParseNullableDoubleArray(JsonElement el)
                {
                    if (el.ValueKind != JsonValueKind.Array) return Array.Empty<double?>();
                    var list = new List<double?>();
                    foreach (var item in el.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Null)
                        {
                            list.Add(null);
                        }
                        else if (item.TryGetDouble(out var d))
                        {
                            list.Add(d);
                        }
                        else if (item.ValueKind == JsonValueKind.Number && item.TryGetDecimal(out var dec))
                        {
                            list.Add((double)dec);
                        }
                        else
                        {
                            // Non-numeric or unexpected value -> treat as null
                            list.Add(null);
                        }
                    }
                    return list.ToArray();
                }

                if (daily.TryGetProperty("temperature_2m_min", out var tminEl))
                    dailyData.Temperature2mMin = ParseNullableDoubleArray(tminEl);

                if (daily.TryGetProperty("temperature_2m_max", out var tmaxEl))
                    dailyData.Temperature2mMax = ParseNullableDoubleArray(tmaxEl);

                if (daily.TryGetProperty("precipitation_sum", out var precipEl))
                    dailyData.PrecipitationSum = ParseNullableDoubleArray(precipEl);

                resp.Daily = dailyData;
                return resp;
            }
            catch (JsonException)
            {
                // Malformed JSON: return empty response rather than throwing.
                return new OpenMeteoResponse();
            }
        }

        public static WeatherResultDto MapToDto(OpenMeteoResponse resp, string isoDate)
        {
            var dto = new WeatherResultDto
            {
                Date = isoDate,
                Status = "OK"
            };

            if (resp?.Daily?.Time == null || resp.Daily.Time.Length == 0)
            {
                dto.Status = "No daily data returned";
                return dto;
            }

            int idx = Array.IndexOf(resp.Daily.Time, isoDate);
            if (idx < 0) idx = 0;

            if (resp.Daily.Temperature2mMin != null && idx < resp.Daily.Temperature2mMin.Length)
                dto.MinTemperatureC = resp.Daily.Temperature2mMin[idx];

            if (resp.Daily.Temperature2mMax != null && idx < resp.Daily.Temperature2mMax.Length)
                dto.MaxTemperatureC = resp.Daily.Temperature2mMax[idx];

            if (resp.Daily.PrecipitationSum != null && idx < resp.Daily.PrecipitationSum.Length)
                dto.PrecipitationMm = resp.Daily.PrecipitationSum[idx];

            return dto;
        }

        public class OpenMeteoResponse
        {
            [JsonPropertyName("daily")]
            public DailyData? Daily { get; set; }
        }

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
}

namespace Dart.Weather.Api.Application.DTOs
{
    // DTO returned by FetchDailyNormalizedAsync and FetchArchiveJsonAsync
    public class NormalizedWeatherDto
    {
        // ISO date string (yyyy-MM-dd) requested/normalized
        public string Date { get; set; } = string.Empty;

        // Minimum temperature in Celsius (nullable)
        public double? MinTemperatureC { get; set; }

        // Maximum temperature in Celsius (nullable)
        public double? MaxTemperatureC { get; set; }

        // Precipitation sum in mm (nullable)
        public double? PrecipitationMm { get; set; }

        // HTTP status code returned by the Open‑Meteo request
        public int StatusCode { get; set; }
    }
}
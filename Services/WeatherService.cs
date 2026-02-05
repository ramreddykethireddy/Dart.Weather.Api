using System.Globalization;
using System.Text.Json;
using Dart.Weather.Api.Application.DTOs;
using Dart.Weather.Api.Application.Interfaces;
using Dart.Weather.Api.Infrastructure;
using Dart.Weather.Api.Helpers;


namespace Dart.Weather.Api.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly OpenMeteoClient _client;
        private readonly WeatherRepository _repo;
        private readonly ILogger<WeatherService> _logger;
        private readonly IHostEnvironment _env;

        public WeatherService(OpenMeteoClient client, WeatherRepository repo, ILogger<WeatherService> logger, IHostEnvironment env)
        {
            _client = client;
            _repo = repo;
            _logger = logger;
            _env = env;
        }

        public async Task<List<WeatherResultDto>> GetAllAsync()
        {
            var results = new List<WeatherResultDto>();
            var datesFile = Path.Combine(_env.ContentRootPath, "Services", "dates.txt");

            if (!File.Exists(datesFile))
            {
                _logger.LogWarning("dates.txt not found at {path}", datesFile);
                return results;
            }

            var lines = await File.ReadAllLinesAsync(datesFile);
            foreach (var raw in lines.Select(l => l?.Trim()).Where(s => !string.IsNullOrEmpty(s)))
            {
                var dto = new WeatherResultDto();
                var parse = DateParser.TryParse(raw!, out var date, out var parseError);
                if (!parse)
                {
                    dto.Date = raw!;
                    dto.Status = $"Invalid date: {parseError}";
                    results.Add(dto);
                    continue;
                }

                var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                dto.Date = iso;

                // If saved locally, read the minimal file and map to the DTO
                if (_repo.Exists(iso))
                {
                    try
                    {
                        var saved = await _repo.ReadJsonAsync(iso);

                        // Minimal file structure: { date, minTemperature, maxTemperature, precipitationMm }
                        var stored = JsonSerializer.Deserialize<StoredWeather>(saved, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (stored != null)
                        {
                            var mapped = new WeatherResultDto
                            {
                                Date = stored.date ?? iso,
                                MinTemperature = stored.minTemperature,
                                MaxTemperature = stored.maxTemperature,
                                Precipitation = stored.precipitationMm,
                                Status = "OK",
                                SourceFile = _repo.GetFilePath(iso)
                            };
                            results.Add(mapped);
                            continue;
                        }

                        _logger.LogWarning("Saved file for {date} could not be parsed", iso);
                        // fall through to attempt network fetch
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read saved file for {date}", iso);
                        // fall through to attempt network fetch
                    }
                }

                // Fetch from API
                try
                {
                    // Use the raw JSON fetcher to both save a minimal file and parse it for response
                    var responseJson = await _client.FetchDailyRawJsonAsync(date);
                    if (string.IsNullOrEmpty(responseJson))
                    {
                        dto.Status = "Empty response from API";
                        results.Add(dto);
                        continue;
                    }

                    // Save a minimal file (only date, minTemperature, maxTemperature, precipitationMm)
                    await _repo.SaveJsonAsync(iso, responseJson);

                    // Parse API response for this request's return payload
                    var parsed = OpenMeteoClient.DeserializeResponse(responseJson);
                    var mapped = OpenMeteoClient.MapToDto(parsed, iso);
                    mapped.SourceFile = _repo.GetFilePath(iso);
                    results.Add(mapped);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch data for {date}", iso);
                    dto.Status = $"Fetch failed: {ex.Message}";
                    results.Add(dto);
                }
            }

            return results;
        }

      
    }
}
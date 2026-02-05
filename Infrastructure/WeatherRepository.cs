using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dart.Weather.Api.Application.DTOs;

namespace Dart.Weather.Api.Infrastructure
{
    public class WeatherRepository
    {
        private readonly string _basePath;

        public WeatherRepository(string contentRoot)
        {
            _basePath = Path.Combine(contentRoot, "weather-data");
            Directory.CreateDirectory(_basePath);
        }

        public bool Exists(string isoDate)
        {
            return File.Exists(GetFilePath(isoDate));
        }

        public string GetFilePath(string isoDate)
        {
            return Path.Combine(_basePath, $"{isoDate}.json");
        }

        /// <summary>
        /// Save a minimal JSON file containing only:
        ///   date, minTemperature, maxTemperature, precipitationMm
        /// The method accepts the raw Open-Meteo JSON, extracts the required values
        /// and writes the minimized representation to disk.
        /// </summary>
        public async Task SaveJsonAsync(string isoDate, string rawApiJson)
        {
            // Try to map raw API JSON into the WeatherResultDto, then write a minimal file
            var openResp = OpenMeteoClient.DeserializeResponse(rawApiJson);
            var mapped = OpenMeteoClient.MapToDto(openResp, isoDate);

            var minimal = new
            {
                date = isoDate,
                minTemperature = mapped.MinTemperature,
                maxTemperature = mapped.MaxTemperature,
                precipitationMm = mapped.Precipitation
            };

            var json = JsonSerializer.Serialize(minimal, new JsonSerializerOptions { WriteIndented = true });

            var file = GetFilePath(isoDate);
            await File.WriteAllTextAsync(file, json);
        }

        public async Task<string> ReadJsonAsync(string isoDate)
        {
            var file = GetFilePath(isoDate);
            return await File.ReadAllTextAsync(file);
        }
    }
}
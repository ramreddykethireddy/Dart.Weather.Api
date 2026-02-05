using Microsoft.AspNetCore.Mvc;
using Dart.Weather.Api.Application.Interfaces;
using Dart.Weather.Api.Infrastructure;

namespace Dart.Weather.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _service;
        private readonly OpenMeteoClient _openMeteo;

        public WeatherController(IWeatherService service, OpenMeteoClient openMeteo)
        {
            _service = service;
            _openMeteo = openMeteo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var results = await _service.GetAllAsync();
            return Ok(results);
        }

        /// <summary>
        /// Proxy a request to the Open‑Meteo Archive API.
        /// Query parameters:
        /// - latitude (double)
        /// - longitude (double)
        /// - start_date (yyyy-MM-dd)
        /// - end_date (yyyy-MM-dd)
        /// - daily (comma-separated variables, e.g. temperature_2m_max,temperature_2m_min,precipitation_sum)
        /// - timezone (e.g. auto)
        /// </summary>
        [HttpGet("archive")]
        public async Task<IActionResult> QueryArchive(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string start_date,
            [FromQuery] string end_date,
            [FromQuery] string daily = "temperature_2m_max,temperature_2m_min,precipitation_sum",
            [FromQuery] string timezone = "auto")
        {
            if (string.IsNullOrWhiteSpace(start_date) || string.IsNullOrWhiteSpace(end_date))
                return BadRequest("start_date and end_date are required and must be in yyyy-MM-dd format.");

            try
            {
                var result = await _openMeteo.FetchArchiveJsonAsync(latitude, longitude, start_date, end_date, daily, timezone);
                if (result == null)
                    return NoContent();

                // If result is a NormalizedWeatherDto, return as JSON
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                // Network / API failure
                return StatusCode(503, new { error = "Upstream API request failed", detail = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "Internal error", detail = ex.Message });
            }
        }
    }
}
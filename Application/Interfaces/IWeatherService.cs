using Dart.Weather.Api.Application.DTOs;

namespace Dart.Weather.Api.Application.Interfaces
{
    public interface IWeatherService
    {
        Task<List<WeatherResultDto>> GetAllAsync();
    }
}
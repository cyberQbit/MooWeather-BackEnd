namespace MooWeather.API.DTOs;

public class CreateCityDto
{
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}
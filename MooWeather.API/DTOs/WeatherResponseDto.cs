namespace MooWeather.API.DTOs;

public class WeatherResponseDto
{
    public string Name { get; set; } = string.Empty;
    public double Temp { get; set; }
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int Pressure { get; set; }
}

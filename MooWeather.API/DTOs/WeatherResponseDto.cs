namespace MooWeather.API.DTOs
{
    public class WeatherResponseDto
    {
        public string Name { get; set; } = string.Empty;
        public double Temp { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
        public string Description { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        
        // YENİ EKLENEN VİTRİN BİLGİLERİ!
        public int Visibility { get; set; }
        public int Clouds { get; set; }
        public long Sunrise { get; set; }
        public long Sunset { get; set; }
    }
}
namespace MooWeather.API.DTOs;

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    // Dikkat: AddedAt (Eklenme Tarihi) kısmını kullanıcıya göndermiyoruz, gizledik!
}
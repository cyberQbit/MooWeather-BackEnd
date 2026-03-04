namespace MooWeather.API.Models;

public class City
{
    // Şehrin benzersiz numarası
    public int Id { get; set; } 
    
    // Şehrin adı
    public string Name { get; set; } = string.Empty; 
    
    // Ülke kodu (TR, EN vb.)
    public string CountryCode { get; set; } = string.Empty; 
    
    // Favorilere eklenme tarihi (Otomatik olarak şu anki zamanı alır)
    public DateTime AddedAt { get; set; } = DateTime.UtcNow; 
    
    // YENİ EKLENEN KISIM: Bu şehir hangi kullanıcıya (User.Id) ait?
    public int UserId { get; set; }
}


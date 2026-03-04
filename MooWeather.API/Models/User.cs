namespace MooWeather.API.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // Google'ın her kullanıcıya verdiği eşsiz ID (Kullanıcı adını değiştirse bile bu değişmez)
    public string GoogleSubjectId { get; set; } = string.Empty; 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
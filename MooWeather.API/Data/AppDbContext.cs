using Microsoft.EntityFrameworkCore;
using MooWeather.API.Models; // City sınıfımızı kullanabilmek için

namespace MooWeather.API.Data;

// Sınıfımızın EF Core özelliklerini kazanması için DbContext'ten miras almasını sağlıyoruz
public class AppDbContext : DbContext
{
    // Veritabanı ayarlarını içeri almak için gereken yapıcı metot (Constructor)
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Veritabanımızdaki 'Cities' (Şehirler) tablosunu temsil edecek olan liste
    public DbSet<City> Cities { get; set; }
    
    // YENİ EKLENEN KISIM: Veritabanına Kullanıcılar tablosunu tanıtıyoruz
    public DbSet<User> Users { get; set; }
}
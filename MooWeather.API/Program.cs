using MooWeather.API.Models;
using MooWeather.API.Data;
using MooWeather.API.DTOs; // DTO'larımızı içeri aldık
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Bu iki sihirli satırı ekliyoruz:
builder.Services.AddMemoryCache(); // Sunucumuza geçici hafıza yeteneği verdik
builder.Services.AddHttpClient();  // Sunucumuza başka sitelere istek atma yeteneği verdik

// Veritabanı bağlantımız (SQLite kullanıyoruz)
builder.Services.AddSqlite<AppDbContext>("Data Source=mooweather.db");

// --- YENİ EKLENEN KORUMA (AUTH) AYARLARI ---
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? "MooWeather_Cok_Gizli_Sifre_123456789";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization(); // Yetkilendirme servisini açıyoruz
// ------------------------------------------

var app = builder.Build();

// --- YENİ EKLENEN KORUMALAR ---
app.UseAuthentication(); // "Kimliğini (Token) göster bakayım"
app.UseAuthorization();  // "Geçebilirsin / Geçemezsin"
// ------------------------------

// DİKKAT: Eski 'var favoriteCities = new List<City> {...}' listesini SİLDİK!

// 1. GET Uç Noktası (Artık Async)
// 1. GET: Şehirleri Getir (Veritabanından City al, CityDto'ya çevirip yolla)
// 1. GET: Şehirleri Getir (KORUMALI)
app.MapGet("/api/cities", async (AppDbContext db, ClaimsPrincipal currentUser) =>
{
    var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var cities = await db.Cities.Where(c => c.UserId == userId).ToListAsync();
    
    var cityDtos = cities.Select(c => new CityDto { Id = c.Id, Name = c.Name, CountryCode = c.CountryCode }).ToList();
    return Results.Ok(cityDtos);
}).RequireAuthorization();


// 2. POST: Şehir Ekle (KORUMALI)
app.MapPost("/api/cities", async (CreateCityDto newCityDto, AppDbContext db, ClaimsPrincipal currentUser) =>
{
    var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(newCityDto.Name)) return Results.BadRequest("Şehir adı boş olamaz!"); 

    var newCity = new City
    {
        Name = newCityDto.Name,
        CountryCode = newCityDto.CountryCode,
        AddedAt = DateTime.UtcNow,
        UserId = userId
    };

    await db.Cities.AddAsync(newCity);
    await db.SaveChangesAsync();

    var resultDto = new CityDto { Id = newCity.Id, Name = newCity.Name, CountryCode = newCity.CountryCode };
    return Results.Ok(resultDto);
}).RequireAuthorization();


// 3. DELETE: Şehir Sil (KORUMALI)
app.MapDelete("/api/cities/{id}", async (int id, AppDbContext db, ClaimsPrincipal currentUser) =>
{
    var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var cityToRemove = await db.Cities.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (cityToRemove == null) return Results.NotFound("Şehir bulunamadı veya bunu silmeye yetkiniz yok.");

    db.Cities.Remove(cityToRemove);
    await db.SaveChangesAsync();
    
    return Results.Ok($"{cityToRemove.Name} silindi.");
}).RequireAuthorization();

// 4. GET: Hava Durumunu Getir (Proxy, Cache & Multi-API Key Fallback)
app.MapGet("/api/weather/{cityName}", async (
    string cityName,
    // `lang` query parametresini alıyoruz; default olarak 'tr'
    string? lang,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration config) => // DİKKAT: IConfiguration eklendi!
{
    // 1. Önce Cache'de var mı diye bakıyoruz
    // Eğer query parametresi boş gelirse default 'tr' kullan
    lang = string.IsNullOrWhiteSpace(lang) ? "tr" : lang;
    string cacheKey = $"weather_{cityName.ToLower()}_{lang.ToLower()}"; 
    
    if (cache.TryGetValue(cacheKey, out string? cachedWeather))
    {
        return Results.Content(cachedWeather, "application/json");
    }

   // 2. Render'dan (Tekli) veya ayarlardan (Çoklu) şifreleri topla
    var singleKey = config["OpenWeather:ApiKey"];
    var multiKeys = config.GetSection("OpenWeather:ApiKeys").Get<string[]>();

    var apiKeys = new List<string>();
    if (!string.IsNullOrWhiteSpace(singleKey)) apiKeys.Add(singleKey);
    if (multiKeys != null) apiKeys.AddRange(multiKeys);

    if (apiKeys.Count == 0)
    {
        return Results.StatusCode(500); 
    }

    var client = httpClientFactory.CreateClient();
    
    // 3. Sırayla tüm API Key'leri deniyoruz (İşte senin MooWeather'daki mantığın C# hali)
    foreach (var apiKey in apiKeys)
    {
        string url = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={apiKey}&units=metric&lang={lang}";
        var response = await client.GetAsync(url);

        // Eğer istek BAŞARILIYSA (200 OK), döngüyü kırıp kullanıcıya cevabı döneriz
        if (response.IsSuccessStatusCode)
        {
            string weatherData = await response.Content.ReadAsStringAsync();

            try
            {
                var json = JObject.Parse(weatherData);

                var weatherDto = new WeatherResponseDto
                {
                    Name = (string?)json["name"] ?? string.Empty,
                    Temp = (double?)(json["main"]?["temp"]) ?? 0,
                    TempMax = (double?)(json["main"]?["temp_max"]) ?? 0,
                    TempMin = (double?)(json["main"]?["temp_min"]) ?? 0,
                    FeelsLike = (double?)(json["main"]?["feels_like"]) ?? 0,
                    Humidity = (int?)(json["main"]?["humidity"]) ?? 0,
                    Pressure = (int?)(json["main"]?["pressure"]) ?? 0,
                    Description = (string?)(json["weather"]?[0]?["description"]) ?? string.Empty,
                    WindSpeed = (double?)(json["wind"]?["speed"]) ?? 0
                };

                var weatherJson = JsonConvert.SerializeObject(weatherDto);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                cache.Set(cacheKey, weatherJson, cacheOptions);

                return Results.Content(weatherJson, "application/json");
            }
            catch
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                cache.Set(cacheKey, weatherData, cacheOptions);

                return Results.Content(weatherData, "application/json");
            }
        }
        
        // Eğer istek BAŞARISIZSA (Örn: Limit doldu - 429 veya Key yanlış - 401), 
        // return YAPMIYORUZ. Döngü devam ediyor ve sıradaki apiKey deneniyor.
    }

    // 4. Eğer kod buraya kadar geldiyse, döngü bitmiş ve hiçbir API Key çalışmamış demektir!
    return Results.BadRequest("Hava durumu çekilemedi. Tüm API anahtarları denendi ancak yanıt alınamadı (Muhtemelen kotalar doldu).");
});

// 4.5 GET: Koordinat (GPS) ile Hava Durumu Getir
app.MapGet("/api/weather/location", async (
    double lat, 
    double lon, 
    string? lang,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration config) => 
{
    lang = string.IsNullOrWhiteSpace(lang) ? "tr" : lang;
    string cacheKey = $"weather_gps_{lat}_{lon}_{lang.ToLower()}"; 
    
    if (cache.TryGetValue(cacheKey, out string? cachedWeather)) return Results.Content(cachedWeather, "application/json");

    var singleKey = config["OpenWeather:ApiKey"];
    var multiKeys = config.GetSection("OpenWeather:ApiKeys").Get<string[]>();
    var apiKeys = new List<string>();
    if (!string.IsNullOrWhiteSpace(singleKey)) apiKeys.Add(singleKey);
    if (multiKeys != null) apiKeys.AddRange(multiKeys);

    if (apiKeys.Count == 0) return Results.StatusCode(500); 

    var client = httpClientFactory.CreateClient();
    
    foreach (var apiKey in apiKeys)
    {
        string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric&lang={lang}";
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string weatherData = await response.Content.ReadAsStringAsync();
            try
            {
                var json = JObject.Parse(weatherData);
                var weatherDto = new WeatherResponseDto
                {
                    Name = (string?)json["name"] ?? string.Empty,
                    Temp = (double?)(json["main"]?["temp"]) ?? 0,
                    TempMax = (double?)(json["main"]?["temp_max"]) ?? 0,
                    TempMin = (double?)(json["main"]?["temp_min"]) ?? 0,
                    FeelsLike = (double?)(json["main"]?["feels_like"]) ?? 0,
                    Humidity = (int?)(json["main"]?["humidity"]) ?? 0,
                    Pressure = (int?)(json["main"]?["pressure"]) ?? 0,
                    Description = (string?)(json["weather"]?[0]?["description"]) ?? string.Empty,
                    WindSpeed = (double?)(json["wind"]?["speed"]) ?? 0
                };
                var weatherJson = JsonConvert.SerializeObject(weatherDto);
                cache.Set(cacheKey, weatherJson, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
                return Results.Content(weatherJson, "application/json");
            }
            catch { return Results.Content(weatherData, "application/json"); }
        }
    }
    return Results.BadRequest("Konum hava durumu çekilemedi.");
});

// 5. POST: Google Login ve JWT Üretimi (VIP Kart Masası)
app.MapPost("/api/auth/google", async (GoogleLoginDto loginDto, AppDbContext db, IConfiguration config) =>
{
    try
    {
        // 1. ADIM: Sadece Google'ın gerçek imzası var mı diye bak (Şifre eşleşmesini boşver)
        var payload = await GoogleJsonWebSignature.ValidateAsync(loginDto.IdToken);

        // 2. ADIM: Mektup gerçek! Kullanıcıyı veritabanında arıyoruz
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubjectId == payload.Subject);

        if (user == null)
        {
            // Kullanıcı kulübümüze ilk defa geliyor, onu kaydedelim!
            user = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                GoogleSubjectId = payload.Subject,
                CreatedAt = DateTime.UtcNow
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        // 3. ADIM: Kendi VIP Kartımızı (JWT) Üretiyoruz
        var jwtSecret = config["JwtSettings:SecretKey"] ?? "MooWeather_Cok_Gizli_Sifre_123456789";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Kartın içine kullanıcının kimliğini (ID ve Email) basıyoruz
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Kartın geçerlilik süresini (Örn: 30 gün) ve diğer ayarlarını yapıyoruz
        var token = new JwtSecurityToken(
            issuer: config["JwtSettings:Issuer"],
            audience: config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

        // Müşteriye VIP kartını (Token) teslim ediyoruz!
        return Results.Ok(new { Token = jwtString, Message = "Giriş Başarılı!" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"GOOGLE LOGIN HATASI: {ex.Message}"); // <-- BU SATIRI EKLE!
        // Eğer referans mektubu (Google Token) sahteyse veya süresi geçmişse:
        return Results.Unauthorized(); 
    }
});

// UptimeRobot için ping noktası
app.MapGet("/", () => "MooWeather API Ayakta ve Calisiyor! 🚀");

app.Run();
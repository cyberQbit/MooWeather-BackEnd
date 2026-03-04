# 1. Aşama: İnşa (Build)
# Not: Önceki loglarında .NET 10 gördüm, o yüzden 10.0 yazdım. Eğer hata verirse buraları 8.0 veya 9.0 yapabilirsin.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Proje dosyalarını kopyala ve restore et
COPY ["MooWeather.API/MooWeather.API.csproj", "MooWeather.API/"]
RUN dotnet restore "MooWeather.API/MooWeather.API.csproj"

# Tüm kodları kopyala ve yayınla (publish)
COPY . .
WORKDIR "/src/MooWeather.API"
RUN dotnet publish "MooWeather.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Aşama: Çalıştırma (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

# Dockerfile içindeki ENV satırını şununla değiştir:
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MooWeather.API.dll"]
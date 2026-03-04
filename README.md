### 2. MooWeather Backend (C# .NET) İçin `README.md`

# ⚙️ MooWeather API - Backend Service

![.NET Core](https://img.shields.io/badge/.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge&logo=JSON%20web%20tokens)

This is the backend service for the **MooWeather** mobile application. Built with C# and .NET, it acts as a secure bridge between the mobile app, the user database, and third-party weather providers (OpenWeatherMap).

> 📱 **Note:** This repository contains the backend code. For the Flutter mobile app, visit:
> 👉 **[MooWeather Mobile Repository](https://github.com/SENIN_KULLANICI_ADIN/MooWeather-Mobile)**

## ⚡ Architecture & Features

* **Custom Data Parsing:** Intercepts complex JSON from OpenWeatherMap, cleans it, and sends formatted, PascalCase JSON to the mobile client.
* **JWT Authentication:** Secure endpoint protection for users signing in via Google on the mobile app.
* **Cloud Sync:** Allows users to save, retrieve, and delete their favorite cities across multiple devices.
* **Localization Passthrough:** Accepts language parameters (`?lang=tr`, `?lang=es`) from the frontend and fetches accurately localized weather descriptions.

## 🛠️ Setup & Running Locally

1. Clone the repository:
   ```bash
   git clone [https://github.com/SENIN_KULLANICI_ADIN/MooWeather-Backend.git](https://github.com/SENIN_KULLANICI_ADIN/MooWeather-Backend.git)


2. Set your OpenWeatherMap API Key in `appsettings.json`.
3. To allow external devices (like a physical mobile phone) to connect to your localhost API, run the project with the following command:
```bash
dotnet run --urls "[http://0.0.0.0:5149](http://0.0.0.0:5149)"

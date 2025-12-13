# Peksan İzlenebilirlik Sistemi

Bu proje, üretim izlenebilirliği için geliştirilmiş bir web uygulamasıdır.

## Proje Yapısı

- **Peksan_İzle_BE**: ASP.NET Core Web API (Backend)
- **Peksan_İzle_FE**: React + Vite (Frontend)

## Kurulum

### Backend Kurulumu

1. `Peksan_izlenebilirlik 2/Peksan_izlenebilirlik 2/Peksan_İzle_BE/Peksan.Izle.API` dizinine gidin
2. `appsettings.Example.json` dosyasını kopyalayın ve `appsettings.json` olarak adlandırın
3. `appsettings.json` içindeki veritabanı bağlantı bilgilerini kendi sunucu bilgilerinizle güncelleyin:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;Encrypt=false;"
     }
   }
   ```
4. Projeyi çalıştırın:
   ```bash
   dotnet restore
   dotnet run
   ```

### Frontend Kurulumu

1. `Peksan_izlenebilirlik 2/Peksan_izlenebilirlik 2/Peksan_İzle_FE` dizinine gidin
2. Bağımlılıkları yükleyin:
   ```bash
   npm install
   ```
3. Geliştirme sunucusunu başlatın:
   ```bash
   npm run dev
   ```

## Güvenlik Notları

- `appsettings.json` dosyası Git'e eklenmemiştir (hassas bilgi içerir)
- Kendi sunucu bilgilerinizi kullanarak `appsettings.json` dosyasını oluşturun
- Production ortamında environment variables kullanılması önerilir

## Teknolojiler

### Backend
- ASP.NET Core 7.0
- Entity Framework Core
- SQL Server

### Frontend
- React 18
- Vite
- D3.js (Görselleştirme)

## Lisans

Bu proje Peksan için özel olarak geliştirilmiştir.


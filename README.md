# Otobus Bilet Rezervasyon Sistemi

ASP.NET Core ve MySQL kullanilarak gelistirilen kapsamli bir otobus bilet rezervasyon sistemi.

## Proje Durumu

| Katman | Durum |
|--------|-------|
| Models | ✅ Tamamlandi |
| DTOs | ✅ Tamamlandi |
| Repositories | ✅ Tamamlandi |
| Services | ✅ Tamamlandi |
| Controllers | ❌ Yapilmadi |
| Views | ❌ Yapilmadi |
| Database | ✅ Sema hazir |

## Mimari Yapi

```
├── Models/                     # Entity siniflari (13 dosya)
│   ├── Role.cs
│   ├── User.cs
│   ├── Station.cs
│   ├── Route.cs
│   ├── RouteStation.cs
│   ├── Bus.cs
│   ├── Departure.cs
│   ├── Seat.cs
│   ├── Ticket.cs
│   ├── Passenger.cs
│   ├── Payment.cs
│   ├── Log.cs
│   └── PasswordReset.cs
│
├── DTOs/                       # Data Transfer Objects (7 dosya)
│   ├── Auth/
│   │   ├── LoginDto.cs
│   │   ├── RegisterDto.cs
│   │   └── AuthResponseDto.cs
│   ├── Ticket/
│   │   ├── CreateTicketDto.cs
│   │   └── TicketResponseDto.cs
│   └── Search/
│       ├── SearchQueryDto.cs
│       └── DepartureResponseDto.cs
│
├── Repositories/               # Veritabani erisim katmani (10 dosya)
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── ITicketRepository.cs
│   │   ├── IDepartureRepository.cs
│   │   ├── ISeatRepository.cs
│   │   └── ILogRepository.cs
│   ├── UserRepository.cs
│   ├── TicketRepository.cs
│   ├── DepartureRepository.cs
│   ├── SeatRepository.cs
│   └── LogRepository.cs
│
├── Services/                   # Is mantigi katmani (10 dosya)
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── ITicketService.cs
│   │   ├── ISearchService.cs
│   │   ├── ILogService.cs
│   │   └── IAdminService.cs
│   ├── AuthService.cs
│   ├── TicketService.cs
│   ├── SearchService.cs
│   ├── LogService.cs
│   └── AdminService.cs
│
├── Controllers/                # HTTP istek yonetimi (bos)
├── Views/                      # Kullanici arayuzu (bos)
├── AppDbContext.cs             # Entity Framework DbContext
├── database.md                 # Veritabani semasi
└── instruction.md              # Proje talimatlari
```

## Veritabani Tablolari

| Tablo | Aciklama |
|-------|----------|
| `roles` | Kullanici rolleri (admin, user, staff) |
| `users` | Kullanici bilgileri |
| `stations` | Otobus terminalleri |
| `routes` | Seferler arasi rotalar |
| `route_stations` | Ara duraklar |
| `buses` | Otobus bilgileri |
| `departures` | Sefer bilgileri |
| `seats` | Koltuk bilgileri |
| `tickets` | Bilet kayitlari |
| `passengers` | Yolcu bilgileri |
| `payments` | Odeme kayitlari |
| `logs` | Sistem loglari |
| `password_resets` | Sifre sifirlama tokenlari |

## Servis Ozellikleri

### AuthService
- Kullanici kaydi (Register)
- Giris yapma (Login)
- JWT token olusturma ve dogrulama
- Beni hatirla (Remember Me) tokeni
- Sifre sifirlama
- Sifre degistirme

### TicketService
- Atomik bilet satin alma (transaction)
- Koltuk musaitlik kontrolu
- Bilet iptali ve iade

### SearchService
- Kalkis/varis/tarihe gore sefer arama
- Istasyon arama
- Koltuk bilgisi sorgulama

### LogService
- Otomatik log kaydi (giris, cikis, kayit, satin alma, iptal vb.)
- Tarih araligina gore log sorgulama
- Kullaniciya gore log sorgulama

### AdminService
- Kullanici CRUD islemleri
- Otobus CRUD islemleri
- Rota CRUD islemleri
- Istasyon CRUD islemleri
- Sefer CRUD islemleri
- Dashboard istatistikleri

## Teknolojiler

- **Framework:** ASP.NET Core
- **ORM:** Entity Framework Core
- **Veritabani:** MySQL (Pomelo.EntityFrameworkCore.MySql)
- **Kimlik Dogrulama:** JWT (JSON Web Token)
- **Sifreleme:** BCrypt.Net-Next
- **Mimari:** Repository Pattern + Service Layer

## Veritabani Normalizasyonu

- ✅ 1NF - Atomik degerler
- ✅ 2NF - Kismi bagimlilik yok
- ✅ 3NF - Gecisli bagimlilik yok

### Eklenen Kisitlamalar
- `stations(name, city)` - Ayni sehirde ayni isimde istasyon engellenir
- `route_stations(route_id, stop_order)` - Ayni rotada ayni durak sirasi engellenir
- `passengers(seat_id)` - Ayni koltuga birden fazla yolcu engellenir

## Sonraki Adimlar

1. [ ] Controllers olustur (AuthController, SearchController, TicketController, DashboardController, AdminController)
2. [ ] Program.cs'de Dependency Injection ayarla
3. [ ] JWT yapilandirmasi ekle
4. [ ] Views olustur (Razor veya API olarak birak)
5. [ ] Birim testleri yaz

## Kurulum

```bash
# Paketleri yukle
dotnet restore

# Veritabanini olustur
dotnet ef database update

# Uygulamayi calistir
dotnet run
```

## Gerekli NuGet Paketleri

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

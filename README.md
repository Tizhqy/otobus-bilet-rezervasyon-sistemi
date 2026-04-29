# Otobus Bilet Rezervasyon Sistemi

ASP.NET Core 8, EF Core ve MySQL ile gelistirilen MVC tabanli otobus bilet rezervasyon uygulamasi.

## Guncel Durum

| Katman | Durum |
|--------|-------|
| Models | ✅ Tamamlandi |
| DTOs / ViewModels | ✅ Tamamlandi |
| Repositories | ✅ Tamamlandi |
| Services + Flow Services | ✅ Tamamlandi |
| Controllers | ✅ Tamamlandi |
| Razor Views | ✅ Tamamlandi |
| Middleware | ✅ Tamamlandi |
| Database + Migrations + Seeder | ✅ Tamamlandi |

## Mimari (SoC)

Proje sorumluluklari net ayrilmistir:

- **Controllers**: HTTP giris/cikis, yonlendirme, TempData mesajlari.
- **Flow Services** (`BiletFlowService`, `OdemeFlowService`, `SeferFlowService`, `AdminFlowService`): use-case orkestrasyonu.
- **Core Services** (`AuthService`, `TicketService`, `SearchService`, `PaymentService`, ...): is kurallari.
- **Repositories**: yalnizca veri erisimi.
- **Middleware**: cross-cutting konular (guvenlik header, request logging).

## Mevcut Moduller

- **Auth**: kayit, giris/cikis, sifre sifirlama, token/oturum guvenligi.
- **Sefer**: arama, filtreleme, detay, dinamik koltuk haritasi.
- **Bilet**: satin alma, detay, listeleme, iptal.
- **Odeme**: kupon uygulama, odeme tamamlama, timeout yonetimi.
- **Admin**: dashboard, loglar, kullanici/rota/otobus/sefer yonetimi, tekli-toplu sefer fiyat guncelleme.
- **Pages**: About/Careers vb. statik sayfalar.

## Odeme ve Koltuk Guvenligi (Guncel)

- Satin alma ve odeme akislarinda transaction kullanilir.
- Ayni istegin tekrar gonderilmesine karsi **idempotency key** kullanilir.
- Odeme referansi deterministik olarak `ticketId + idempotencyKey` uzerinden uretilir.
- Odeme tamamlama adiminda `Pending -> Completed/Confirmed` gecisi kosullu/atomik update ile yapilir.
- Koltuk rezervasyonu atomik status update ile yapilir (race kosullarini azaltir).
- Kupon uygulama/mark-as-used akisi idempotent tekrar cagrilarla uyumludur.

## Middleware ve Platform Guvenligi

- **Security headers + CSP nonce**
- **Request logging middleware** (`Middleware/RequestLoggingMiddleware.cs`)
  - `Method`, `Path`, `StatusCode`, `Duration`, `TraceId`, `UserId`, `IP`
  - `X-Correlation-ID` response header
  - Hassas payload (kart/token/sifre) loglanmaz
- **Global anti-forgery** (`AutoValidateAntiforgeryToken`)
- **Rate limiting**
  - `AuthLoginPolicy`
  - `PasswordResetPolicy`
  - `PasswordResetConfirmPolicy`
  - `PasswordChangePolicy`
  - `AdminLogCleanupPolicy`
- **Response compression** (Brotli/Gzip)
- **Output cache** (istasyon arama/liste endpointleri)

## Proje Yapisi (Ozet)

```text
Controllers/
  AdminController.cs
  AuthController.cs
  BiletController.cs
  OdemeController.cs
  PagesController.cs
  SeferController.cs

Services/
  AdminFlowService.cs
  AdminService.cs
  AuthService.cs
  BiletFlowService.cs
  CouponService.cs
  LogService.cs
  OdemeFlowService.cs
  PaymentService.cs
  SearchService.cs
  SeferFlowService.cs
  TicketService.cs

Repositories/
  CouponRepository.cs
  DepartureRepository.cs
  LogRepository.cs
  SeatRepository.cs
  TicketRepository.cs
  UserRepository.cs

Middleware/
  RequestLoggingMiddleware.cs

Views/
  Admin/ Auth/ Bilet/ Odeme/ Pages/ Sefer/ Shared/
```

## Veritabani

Temel tablolar:

- `roles`, `users`
- `stations`, `routes`, `route_stations`
- `buses`, `departures`, `seats`
- `tickets`, `passengers`, `payments`
- `password_resets`, `logs`
- `coupons`, `coupon_usages`

ER diyagrami: `db_semasi_beyaz.svg`

## Calistirma

```bash
dotnet restore
dotnet ef database update
dotnet run
```

> Uygulama acilisinda migration + seed de otomatik tetiklenir (`Program.cs`).

## Konfigurasyon Notlari

- `ConnectionStrings:DefaultConnection` zorunlu.
- DB sifresi baglanti metninde yoksa:
  - `Database:Password` veya `MYSQL_PASSWORD` ile verilebilir.
- `Jwt:Key` en az 32 karakter olmali.
- `App:BaseUrl` sifre sifirlama linki icin kullanilir.
- `Smtp:*` ayarlari sifre sifirlama maili icin gereklidir.
- Production'da gizli bilgiler icin user-secrets / environment variable tercih edilmelidir.

## Teknolojiler

- ASP.NET Core 8 (MVC)
- Entity Framework Core 8
- Pomelo MySQL provider
- Cookie + JWT auth
- BCrypt.Net
- Repository + Service + FlowService mimarisi

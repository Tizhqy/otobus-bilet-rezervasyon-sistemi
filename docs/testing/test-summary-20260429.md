**Test Özeti (2026-04-29)**

**Ortam:** `ASPNETCORE_ENVIRONMENT=Testing` (InMemory DB)
**Amaç:** Otomatik SQL injection scripti ve performans baseline scripti çalıştırıldı; CSV çıktıları toplandı.

**Üretilen dosyalar:**
CSV: [tests/security/sql-injection-results.csv](tests/security/sql-injection-results.csv) — DOSYA MEVCUT, içeriği boş (kayıt yok).
CSV: [tests/performance/baseline-performance-20260429-210509.csv](tests/performance/baseline-performance-20260429-210509.csv)

**Performans (CSV'den hızlı özet):**
`https://localhost:5001/Auth/Giris` — Ortalama: 32.75 ms, Min: 19.93 ms, P95: 54.69 ms, Max: 62.91 ms
`https://localhost:5001/Sefer/Index` — Ortalama: 25.19 ms, Min: 16.71 ms, P95: 37.05 ms, Max: 47.68 ms
`https://localhost:5001/Sefer/IstasyonAra?query=is` — Ortalama: 26.88 ms, Min: 13.81 ms, P95: 40.51 ms, Max: 40.62 ms

**Notlar / Bulgular:**
Testler `Testing` ortamında InMemory DB ile çalıştırıldı; uygulama yerel olarak `https://localhost:5001` adresinde dinliyor.
`tests/security/sql-injection-results.csv` boş çıktı üretti. Muhtemel nedenler:
  - Script, istekler sırasında hata yakalayıp sonuçları `PASS`/`FAIL` olarak ekledi fakat CSV yazımı boş kaldı — (örn. scriptte `$results` scope/başlatma sorunu)
  - Endpoint'ler isteklere yanıt verdi ancak script hata durumunda bile objeleri doğru eklememiş olabilir.
Performans CSV'sindeki `SuccessCount`/`FailedCount` değerleri beklenenden farklı olabilir; detay için raw HTTP durum kodları ve script loglarının genişletilmesi önerilir.

**Önerilen aksiyonlar (kısa):**
1. `tests/security/sql-injection-tests.ps1`'e ayrıntılı logging (HTTP status, response length, exception message) ekleyip scripti tekrar çalıştırmak.
2. SQLi scriptine POST senaryoları ve ek endpointler (register/login, ticket create) eklemek.
3. Performans scriptinde HTTP durum kodlarını CSV'ye sütun olarak ekleyip başarılı/başarısız sayımlarını doğrulamak.
4. Bu rapor commitlenip `develop2`'ye pushlandı — eğer isterseniz CI pipeline'a entegrasyon yapabilirim.

**İşlem kayıtları:**
Uygulama: `dotnet run` (Testing)
SQLi script: `tests/security/sql-injection-tests.ps1 -BaseUrl "https://localhost:5001"`
Performans script: `tests/performance/baseline-performance-test.ps1 -BaseUrl "https://localhost:5001" -Iterations 30 -WarmupIterations 5`

**Güncel iterasyon notu:**
SQLi scripti daha fazla endpoint/POST senaryosu ile genişletildi ve `http://localhost:5000` üzerinden tekrar çalıştırıldı.
Son çalıştırmada kayıtlar şu şekilde oluştu:
  - `StationSearch`, `SeferIndex`, `AuthGiris`, `AuthLoginPost`, `AuthRegisterPost` için ağırlıklı olarak `ERROR`/`400` sonuçları.
  - `BiletSatinAlPost` ve `BiletKoltukKontrolPost` için `FAIL`/`500` sonuçları.
Bu, POST tarafında hata yüzeyinin daha görünür hale geldiğini gösteriyor; özellikle `BiletController` içindeki iş akışları için sunucu tarafı exception logları ayrıca gözden geçirilmeli.

---
Rapor otomatik oluşturuldu ve repoya eklendi.

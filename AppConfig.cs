namespace OtobusBiletRezervasyon
{
    /// <summary>
    /// Uygulama genelinde kullanilan yapilandirma degerleri.
    /// Bu degerler kolayca degistirilebilir olsun diye tek bir yerde toplandi.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// Odeme icin verilen sure (dakika).
        /// Bu sure icinde odeme yapilmazsa bilet otomatik iptal edilir.
        /// </summary>
        public const int PaymentTimeoutMinutes = 15;

        /// <summary>
        /// Bilet iptal edilebilmesi icin kalkisa kalan minimum sure (dakika).
        /// Ornegin 10 ise, kalkisa 10 dakikadan az kaldiysa iptal edilemez.
        /// </summary>
        public const int MinCancellationMinutesBeforeDeparture = 10;

        /// <summary>
        /// Sifre minimum uzunlugu.
        /// </summary>
        public const int MinPasswordLength = 8;

        /// <summary>
        /// Yaklasan seferler listesinde gosterilecek varsayilan sayi.
        /// </summary>
        public const int DefaultUpcomingDepartureCount = 10;

        /// <summary>
        /// Yaklasan seferler listesinde gosterilecek maksimum sayi.
        /// </summary>
        public const int MaxUpcomingDepartureCount = 50;

        /// <summary>
        /// Admin log sayfasinda sayfa basina gosterilecek kayit sayisi.
        /// </summary>
        public const int LogPageSize = 50;

        /// <summary>
        /// Log temizlemede saklanacak minimum gun sayisi.
        /// </summary>
        public const int MinLogRetentionDays = 7;

        /// <summary>
        /// Admin kullanici listesinde sayfa basina gosterilecek kayit sayisi.
        /// </summary>
        public const int AdminUserPageSize = 50;

        /// <summary>
        /// Bilet satisinin kapanacagi, kalkis oncesi minimum sure (dakika).
        /// </summary>
        public const int TicketSalesCutoffMinutesBeforeDeparture = 30;

        /// <summary>
        /// Istasyon arama sorgusu icin maksimum karakter sayisi.
        /// </summary>
        public const int MaxStationSearchQueryLength = 50;
    }
}

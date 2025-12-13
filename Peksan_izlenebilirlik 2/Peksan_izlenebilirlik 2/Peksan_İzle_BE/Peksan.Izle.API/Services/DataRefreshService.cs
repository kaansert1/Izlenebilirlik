using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;

namespace Peksan.Izle.API.Services
{
    public class DataRefreshService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataRefreshService> _logger;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10); // 10 dakika
        private int _lastCheckedMonth = DateTime.Now.Month; // Ay değişimi kontrolü için

        public DataRefreshService(IServiceProvider serviceProvider, ILogger<DataRefreshService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Refresh Service başlatıldı. Güncelleme aralığı: {Interval} dakika", _refreshInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ay değişimi kontrolü
                    CheckMonthChange();

                    await RefreshData();
                    _logger.LogInformation("Veriler başarıyla güncellendi: {Time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Veri güncelleme sırasında hata oluştu: {Time}", DateTime.Now);
                }

                // 10 dakika bekle
                await Task.Delay(_refreshInterval, stoppingToken);
            }
        }

        private void CheckMonthChange()
        {
            int currentMonth = DateTime.Now.Month;
            if (currentMonth != _lastCheckedMonth)
            {
                _logger.LogInformation("Ay değişimi tespit edildi! Önceki ay: {PreviousMonth}, Yeni ay: {CurrentMonth}",
                    _lastCheckedMonth, currentMonth);

                _lastCheckedMonth = currentMonth;

                // Burada cache temizleme veya özel işlemler yapılabilir
                _logger.LogInformation("Sunburst chart için yeni ay ({Month}) aktif hale getirildi", currentMonth);
            }
        }

        private async Task RefreshData()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Veritabanı bağlantısını test et
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Dashboard verilerini güncelle (cache'lenebilir)
                await RefreshDashboardData(connection);

                // Sunburst verilerini güncelle (cache'lenebilir)
                await RefreshSunburstData(connection);

                await connection.CloseAsync();

                _logger.LogInformation("Tüm veriler başarıyla yenilendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veri yenileme işlemi başarısız");
                throw;
            }
        }

        private async Task RefreshDashboardData(System.Data.Common.DbConnection connection)
        {
            try
            {
                // Günlük üretim verilerini kontrol et
                var dailyProductionCommand = connection.CreateCommand();
                dailyProductionCommand.CommandText = @"
                    SELECT COUNT(R.ID) AS URETILEN_KOLI,
                           CASE WHEN DESCRIPTION2='E' THEN 'Enjeksiyon'
                                WHEN DESCRIPTION2='M' THEN 'Montaj'
                                ELSE 'Serigrafi'
                           END AS MAK_TYPE
                    FROM _RP_URETIM_SERI R
                    LEFT OUTER JOIN ANC_TBLMACHINE E WITH (NOLOCK) ON E.MACHINE_CODE=R.MAK_ID
                    WHERE DAY(TARIH)=DAY(GETDATE())
                      AND MONTH(TARIH)=MONTH(GETDATE())
                      AND YEAR(TARIH)=YEAR(GETDATE())
                      AND URET_TIP=0
                    GROUP BY DESCRIPTION2";

                using var reader = await dailyProductionCommand.ExecuteReaderAsync();
                int totalProduction = 0;

                while (await reader.ReadAsync())
                {
                    var koliSayisi = Convert.ToInt32(reader["URETILEN_KOLI"]);
                    totalProduction += koliSayisi;
                }

                reader.Close();
                _logger.LogInformation("Dashboard verileri güncellendi. Günlük toplam üretim: {Total} koli", totalProduction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard verileri güncellenirken hata oluştu");
                throw;
            }
        }

        private async Task RefreshSunburstData(System.Data.Common.DbConnection connection)
        {
            try
            {
                // Aylık üretim verilerini kontrol et
                var monthlyCommand = connection.CreateCommand();
                monthlyCommand.CommandText = @"
                    SELECT MONTH(TARIH) as AY,
                           COUNT(R.ID) AS URETILEN_KOLI,
                           CASE WHEN DESCRIPTION2='E' THEN 'Enjeksiyon'
                                WHEN DESCRIPTION2='M' THEN 'Montaj'
                                ELSE 'Serigrafi'
                           END AS MAK_TYPE
                    FROM _RP_URETIM_SERI R
                    LEFT OUTER JOIN ANC_TBLMACHINE E WITH (NOLOCK) ON E.MACHINE_CODE=R.MAK_ID
                    WHERE YEAR(TARIH)=2025 AND URET_TIP=0
                    GROUP BY MONTH(TARIH), DESCRIPTION2
                    ORDER BY AY";

                using var reader = await monthlyCommand.ExecuteReaderAsync();
                int totalMonthlyProduction = 0;

                while (await reader.ReadAsync())
                {
                    var koliSayisi = Convert.ToInt32(reader["URETILEN_KOLI"]);
                    totalMonthlyProduction += koliSayisi;
                }

                reader.Close();
                _logger.LogInformation("Sunburst verileri güncellendi. Yıllık toplam üretim: {Total} koli", totalMonthlyProduction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sunburst verileri güncellenirken hata oluştu");
                throw;
            }
        }
    }
}

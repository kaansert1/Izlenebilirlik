using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private static object? _dashboardDataCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                // Cache kontrolü
                if (_dashboardDataCache != null && DateTime.Now - _lastCacheUpdate < _cacheExpiration)
                {
                    return Ok(_dashboardDataCache);
                }
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Günlük üretim adetleri sorgusu - optimize edilmiş
                var dailyProductionCommand = connection.CreateCommand();
                dailyProductionCommand.CommandText = @"
                    SELECT COUNT(R.ID) AS URETILEN_KOLI,
                           CASE WHEN E.DESCRIPTION2='E' THEN 'Enjeksiyon'
                                WHEN E.DESCRIPTION2='M' THEN 'Montaj'
                                ELSE 'Serigrafi'
                           END AS MAK_TYPE,
                           'Üretim' AS URET_TIP
                    FROM _RP_URETIM_SERI R WITH (NOLOCK)
                    LEFT OUTER JOIN ANC_TBLMACHINE E WITH (NOLOCK) ON E.MACHINE_CODE=R.MAK_ID
                    WHERE CAST(R.TARIH AS DATE) = CAST(GETDATE() AS DATE)
                      AND R.URET_TIP = '0'
                    GROUP BY E.DESCRIPTION2";

                var dailyProductionData = new List<object>();
                using var reader = await dailyProductionCommand.ExecuteReaderAsync();

                int totalProduction = 0;
                while (await reader.ReadAsync())
                {
                    var koliSayisi = Convert.ToInt32(reader["URETILEN_KOLI"]);
                    totalProduction += koliSayisi;

                    dailyProductionData.Add(new
                    {
                        uretilenKoli = koliSayisi,
                        makineType = reader["MAK_TYPE"]?.ToString() ?? "",
                        uretimTipi = reader["URET_TIP"]?.ToString() ?? ""
                    });
                }

                reader.Close();

                // Toplam hammadde stoku için optimize edilmiş hesaplama
                var stockCommand = connection.CreateCommand();
                stockCommand.CommandText = @"
                    SELECT ISNULL(SUM(CONVERT(NUMERIC(18,2), NET)), 0) AS TOPLAM_NET
                    FROM _RP_URETIM_SERI WITH (NOLOCK)
                    WHERE URET_TIP = '0'
                      AND CAST(TARIH AS DATE) >= DATEADD(MONTH, -1, CAST(GETDATE() AS DATE))";

                var stockResult = await stockCommand.ExecuteScalarAsync();
                var rawMaterialStock = Convert.ToDecimal(stockResult ?? 0);

                await connection.CloseAsync();

                var result = new
                {
                    totalProduction = totalProduction,
                    rawMaterialStock = rawMaterialStock,
                    dailyProduction = totalProduction, // Günlük üretim toplam koli sayısı
                    dailyProductionDetails = dailyProductionData, // Detaylı günlük üretim verileri
                    timestamp = DateTime.Now
                };

                // Cache'i güncelle
                _dashboardDataCache = result;
                _lastCacheUpdate = DateTime.Now;

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Dashboard verileri alınırken hata oluştu: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("sunburst-data")]
        public async Task<IActionResult> GetSunburstData()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Ay isimlerini Türkçe olarak belirle
                string[] monthNames = {
                    "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                    "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
                };

                // Kategoriler için ana yapı
                var enjeksiyonChildren = new List<object>();
                var montajChildren = new List<object>();

                // Her ay için veri çek (1-12)
                for (int month = 1; month <= 12; month++)
                {
                    var monthCommand = connection.CreateCommand();
                    monthCommand.CommandText = @"
                        SELECT COUNT(R.ID) AS URETILEN_KOLI,
                               CASE WHEN DESCRIPTION2='E' THEN 'Enjeksiyon'
                                    WHEN DESCRIPTION2='M' THEN 'Montaj'
                                    ELSE 'Serigrafi'
                               END AS MAK_TYPE,
                               CASE WHEN URET_TIP='0' THEN 'Üretim'
                                    WHEN URET_TIP='1' THEN 'Numune'
                                    WHEN URET_TIP='2' THEN 'Fire'
                                    WHEN URET_TIP='3' THEN 'Renk Geçişi'
                                    WHEN URET_TIP='5' THEN 'Yarım Koli'
                                    ELSE 'x'
                               END AS URET_TIP
                        FROM _RP_URETIM_SERI R
                        LEFT OUTER JOIN ANC_TBLMACHINE E WITH (NOLOCK) ON E.MACHINE_CODE=R.MAK_ID
                        WHERE MONTH(TARIH)=@Month AND YEAR(TARIH)=2025 AND URET_TIP=0
                        GROUP BY DESCRIPTION2,URET_TIP";

                    monthCommand.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Month", month));

                    using var reader = await monthCommand.ExecuteReaderAsync();

                    var monthData = new Dictionary<string, int>
                    {
                        ["Enjeksiyon"] = 0,
                        ["Montaj"] = 0
                    };

                    while (await reader.ReadAsync())
                    {
                        var makType = reader["MAK_TYPE"]?.ToString() ?? "";
                        var koliSayisi = Convert.ToInt32(reader["URETILEN_KOLI"]);

                        if (makType == "Enjeksiyon" || makType == "Montaj")
                        {
                            monthData[makType] = koliSayisi;
                        }
                    }

                    reader.Close();

                    // Gelecek aylar için otomatik kontrol (mevcut aydan sonraki aylar)
                    int currentMonth = DateTime.Now.Month;
                    bool isFutureMonth = month > currentMonth; // Mevcut aydan sonraki aylar gelecek ay
                    string statusMessage = isFutureMonth ? "Üretim Bekleniyor" : "";

                    // Enjeksiyon verisi ekle (gelecek aylar için minimum 1 değeri ver ki chart'ta görünsün)
                    enjeksiyonChildren.Add(new
                    {
                        name = monthNames[month],
                        size = isFutureMonth ? 1 : monthData["Enjeksiyon"], // Gelecek aylar için 1
                        month = month,
                        category = "Enjeksiyon",
                        isFuture = isFutureMonth,
                        details = new
                        {
                            monthName = monthNames[month],
                            categoryName = "Enjeksiyon",
                            productionCount = monthData["Enjeksiyon"], // Gerçek değer 0
                            monthNumber = month,
                            statusMessage = statusMessage,
                            isFutureMonth = isFutureMonth
                        }
                    });

                    // Montaj verisi ekle (gelecek aylar için minimum 1 değeri ver ki chart'ta görünsün)
                    montajChildren.Add(new
                    {
                        name = monthNames[month],
                        size = isFutureMonth ? 1 : monthData["Montaj"], // Gelecek aylar için 1
                        month = month,
                        category = "Montaj",
                        isFuture = isFutureMonth,
                        details = new
                        {
                            monthName = monthNames[month],
                            categoryName = "Montaj",
                            productionCount = monthData["Montaj"], // Gerçek değer 0
                            monthNumber = month,
                            statusMessage = statusMessage,
                            isFutureMonth = isFutureMonth
                        }
                    });
                }

                await connection.CloseAsync();

                return Ok(new
                {
                    name = "Peksan Üretim",
                    children = new[]
                    {
                        new
                        {
                            name = "Enjeksiyon",
                            children = enjeksiyonChildren
                        },
                        new
                        {
                            name = "Montaj",
                            children = montajChildren
                        }
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Sunburst verileri alınırken hata oluştu: {ex.Message}",
                    error = ex.ToString(),
                    timestamp = DateTime.Now
                });
            }
        }
    }
}

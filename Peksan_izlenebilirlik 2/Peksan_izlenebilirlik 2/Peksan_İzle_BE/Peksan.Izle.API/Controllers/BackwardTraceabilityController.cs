using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackwardTraceabilityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BackwardTraceabilityController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Arama değeri boş olamaz!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                string sql = "";
                SqlParameter parameter;

                // Arama tipine göre farklı sorgular
                switch (request.SearchType?.ToLower())
                {
                    case "seri_no":
                    case "seri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,CONVERT(NUMERIC(18,0),SUM(ADET)) AS ADET,CONVERT(NUMERIC(18,2),SUM(NET)) AS NET_KG,CONVERT(NUMERIC(18,2),SUM(BRUT)) AS BRUT_KG,COUNT(ID) AS KOLI_SAYISI,
                            CASE WHEN URET_TIP='0' THEN 'Üretim' WHEN URET_TIP='1' THEN 'Numune' WHEN URET_TIP='2' THEN 'Fire' WHEN URET_TIP='3' THEN 'Renk Geçişi' WHEN URET_TIP='5' THEN 'Yarım Koli' ELSE 'x' END AS URET_TIP
                            FROM _RP_URETIM_SERI
                            WHERE ISEMRI_NO IN (SELECT ISEMRI_NO FROM _RP_URETIM_SERI WHERE SERI_NO=@SearchValue)
                            GROUP BY ISEMRI_NO,STOK_KODU,URET_TIP
                            ORDER BY ADET DESC";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "isemri_no":
                    case "isemri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,CONVERT(NUMERIC(18,0),SUM(ADET)) AS ADET,CONVERT(NUMERIC(18,2),SUM(NET)) AS NET_KG,CONVERT(NUMERIC(18,2),SUM(BRUT)) AS BRUT_KG,COUNT(ID) AS KOLI_SAYISI,
                            CASE WHEN URET_TIP='0' THEN 'Üretim' WHEN URET_TIP='1' THEN 'Numune' WHEN URET_TIP='2' THEN 'Fire' WHEN URET_TIP='3' THEN 'Renk Geçişi' WHEN URET_TIP='5' THEN 'Yarım Koli' ELSE 'x' END AS URET_TIP
                            FROM _RP_URETIM_SERI
                            WHERE ISEMRI_NO = @SearchValue
                            GROUP BY ISEMRI_NO,STOK_KODU,URET_TIP
                            ORDER BY ADET DESC";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "lot_no":
                    case "lot":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,CONVERT(NUMERIC(18,0),SUM(ADET)) AS ADET,CONVERT(NUMERIC(18,2),SUM(NET)) AS NET_KG,CONVERT(NUMERIC(18,2),SUM(BRUT)) AS BRUT_KG,COUNT(ID) AS KOLI_SAYISI,
                            CASE WHEN URET_TIP='0' THEN 'Üretim' WHEN URET_TIP='1' THEN 'Numune' WHEN URET_TIP='2' THEN 'Fire' WHEN URET_TIP='3' THEN 'Renk Geçişi' WHEN URET_TIP='5' THEN 'Yarım Koli' ELSE 'x' END AS URET_TIP
                            FROM _RP_URETIM_SERI
                            WHERE ISEMRI_NO IN (SELECT ISEMRI_NO FROM _RP_URETIM_SERI WHERE LOT_NO=@SearchValue)
                            GROUP BY ISEMRI_NO,STOK_KODU,URET_TIP
                            ORDER BY ADET DESC";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Geçersiz arama tipi! 'seri_no', 'lot_no' veya 'isemri_no' kullanın."
                        });
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(parameter);

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        STOK_KODU = reader["STOK_KODU"]?.ToString() ?? "",
                        ADET = Convert.ToDecimal(reader["ADET"]),
                        NET = Convert.ToDecimal(reader["NET_KG"]),
                        BRUT = Convert.ToDecimal(reader["BRUT_KG"]),
                        KOLI = Convert.ToInt32(reader["KOLI_SAYISI"]),
                        URETIM_TIPI = reader["URET_TIP"]?.ToString() ?? ""
                    });
                }

                await connection.CloseAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Arama sırasında hata oluştu: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpPost("quick-search")]
        public async Task<IActionResult> QuickSearch([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Arama değeri boş olamaz!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                string sql = "";
                SqlParameter parameter;

                // Tıklanan değer tipine göre direkt sorgu
                switch (request.SearchType?.ToLower())
                {
                    case "seri_no":
                    case "seri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE SERI_NO = @SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "isemri_no":
                    case "isemri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE ISEMRI_NO = @SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "lot_no":
                    case "lot":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE LOT_NO = @SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Geçersiz arama tipi! 'seri_no', 'lot_no' veya 'isemri_no' kullanın."
                        });
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(parameter);

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        STOK_KODU = reader["STOK_KODU"]?.ToString() ?? "",
                        YAP_KOD = reader["YAP_KOD"]?.ToString() ?? "",
                        SERI_NO = reader["SERI_NO"]?.ToString() ?? "",
                        LOT_NO = reader["LOT_NO"]?.ToString() ?? "",
                        TARIH = reader["TARIH"]?.ToString() ?? "",
                        PERSONEL = reader["PERSONEL"]?.ToString() ?? "",
                        MAKINA = reader["MAKINA"]?.ToString() ?? "",
                        B_AGIRLIK = Convert.ToDecimal(reader["B_AGIRLIK"] ?? 0),
                        DARA = Convert.ToDecimal(reader["DARA"] ?? 0),
                        NET = Convert.ToDecimal(reader["NET"] ?? 0),
                        ADET = Convert.ToDecimal(reader["ADET"] ?? 0)
                    });
                }

                await connection.CloseAsync();

                return Ok(new
                {
                    success = true,
                    data = results,
                    searchType = request.SearchType,
                    searchValue = request.SearchValue,
                    count = results.Count,
                    message = $"{results.Count} adet üretim kaydı bulundu.",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hızlı arama sırasında hata oluştu: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpPost("production-details")]
        public async Task<IActionResult> GetProductionDetails([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Seri no değeri boş olamaz!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                string sql = "";
                SqlParameter parameter;

                // Arama tipine göre farklı sorgular
                switch (request.SearchType?.ToLower())
                {
                    case "seri_no":
                    case "seri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE SERI_NO=@SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "lot_no":
                    case "lot":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE LOT_NO=@SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    case "isemri_no":
                    case "isemri":
                        sql = @"
                            SELECT ISEMRI_NO,STOK_KODU,YAP_KOD,SERI_NO,LOT_NO,CONVERT(date,TARIH,143) AS TARIH,(S.FIRST_NAME+''+S.LAST_NAME) AS PERSONEL,MAK_ID AS MAKINA,B_AGIRLIK,DARA,NET,ADET
                            FROM _RP_URETIM_SERI R
                            LEFT OUTER JOIN ANC_TBLSTAFF S WITH (NOLOCK) ON S.STAFF_CODE=R.PERSONEL_ID
                            WHERE ISEMRI_NO=@SearchValue";
                        parameter = new SqlParameter("@SearchValue", request.SearchValue);
                        break;

                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Geçersiz arama tipi! 'seri_no', 'lot_no' veya 'isemri_no' kullanın."
                        });
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(parameter);

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        STOK_KODU = reader["STOK_KODU"]?.ToString() ?? "",
                        YAP_KOD = reader["YAP_KOD"]?.ToString() ?? "",
                        SERI_NO = reader["SERI_NO"]?.ToString() ?? "",
                        LOT_NO = reader["LOT_NO"]?.ToString() ?? "",
                        TARIH = reader["TARIH"]?.ToString() ?? "",
                        PERSONEL = reader["PERSONEL"]?.ToString() ?? "",
                        MAKINA = reader["MAKINA"]?.ToString() ?? "",
                        B_AGIRLIK = Convert.ToDecimal(reader["B_AGIRLIK"] ?? 0),
                        DARA = Convert.ToDecimal(reader["DARA"] ?? 0),
                        NET = Convert.ToDecimal(reader["NET"] ?? 0),
                        ADET = Convert.ToDecimal(reader["ADET"] ?? 0)
                    });
                }

                await connection.CloseAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Üretim detayları alınırken hata oluştu: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpPost("machine-type")]
        public async Task<IActionResult> GetMachineType([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Arama değeri boş olamaz!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Makine tipini belirlemek için sorgu
                var sql = @"
                    SELECT DESCRIPTION2
                    FROM ANC_TBLMACHINE
                    WHERE MACHINE_CODE IN (
                        SELECT MAK_ID
                        FROM _RP_URETIM_SERI S
                        WHERE S.SERI_NO = @SearchValue
                           OR S.ISEMRI_NO = @SearchValue
                           OR S.LOT_NO = @SearchValue
                    )";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@SearchValue", request.SearchValue));

                var result = await command.ExecuteScalarAsync();
                await connection.CloseAsync();

                if (result != null)
                {
                    var machineType = result.ToString();
                    var machineTypeName = machineType switch
                    {
                        "E" => "Enjeksiyon",
                        "M" => "Montaj",
                        _ => "Serigrafi"
                    };

                    return Ok(new
                    {
                        success = true,
                        machineType = machineType,
                        machineTypeName = machineTypeName,
                        searchValue = request.SearchValue,
                        message = $"Makine tipi belirlendi: {machineTypeName}"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Bu değer için makine tipi bulunamadı!",
                        searchValue = request.SearchValue
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Makine tipi sorgulanırken hata oluştu: {ex.Message}",
                    searchValue = request.SearchValue
                });
            }
        }

        [HttpPost("material-consumption")]
        public async Task<IActionResult> GetMaterialConsumption([FromBody] MaterialConsumptionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Arama değeri boş olamaz!"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.MachineType))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Makine tipi belirtilmeli!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                string sql = "";

                // Makine tipine göre farklı sorgular
                if (request.MachineType == "E") // Enjeksiyon
                {
                    sql = @"
                        SELECT
                            T.VS_SERI_NO,
                            T.VS_STOK_KODU,
                            DBO.TRK(ST.STOK_ADI) AS STOK_ADI,
                            ISNULL(SR.ACIK2,'-') AS HAMMADDE_LOT,
                            CONVERT(NUMERIC(18,2),SUM(HARCANAN)) AS HARCANAN
                        FROM _RP_URETIM_SERI_TAKIP T
                        LEFT OUTER JOIN _RP_URETIM_SERI S WITH (NOLOCK) ON S.ID=T.URET_ID
                        LEFT OUTER JOIN TBLSTSABIT ST WITH (NOLOCK) ON ST.STOK_KODU=T.VS_STOK_KODU
                        LEFT OUTER JOIN TBLSERITRA SR WITH (NOLOCK) ON SR.SERI_NO=T.VS_SERI_NO AND SR.DEPOKOD IN (200,300,302) AND SR.GCKOD='G'
                        WHERE S.SERI_NO = @SearchValue OR S.ISEMRI_NO = @SearchValue OR S.LOT_NO = @SearchValue
                        GROUP BY T.VS_SERI_NO,T.VS_STOK_KODU,ST.STOK_ADI,ACIK2
                        ORDER BY T.VS_STOK_KODU ASC";
                }
                else if (request.MachineType == "MONTAJ_PART") // Montaj parçası için
                {
                    // Montaj parçası için sarf malzeme sorgusu (İş Emri No ile)
                    sql = @"
                        SELECT
                            S.ISEMRI_NO,
                            T.VS_STOK_KODU,
                            DBO.TRK(ST.STOK_ADI) AS STOK_ADI,
                            ISNULL(SR.ACIK2,'-') AS HAMMADDE_LOT,
                            CONVERT(NUMERIC(18,2),SUM(HARCANAN)) AS HARCANAN
                        FROM _RP_URETIM_SERI_TAKIP T
                        LEFT OUTER JOIN _RP_URETIM_SERI S WITH (NOLOCK) ON S.ID=T.URET_ID
                        LEFT OUTER JOIN TBLSTSABIT ST WITH (NOLOCK) ON ST.STOK_KODU=T.VS_STOK_KODU
                        LEFT OUTER JOIN TBLSERITRA SR WITH (NOLOCK) ON SR.SERI_NO=T.VS_SERI_NO AND SR.DEPOKOD IN (200,300,302) AND SR.GCKOD='G'
                        WHERE S.ISEMRI_NO = @SearchValue
                        GROUP BY S.ISEMRI_NO,T.VS_STOK_KODU,ST.STOK_ADI,ACIK2
                        ORDER BY T.VS_STOK_KODU ASC";
                }
                else if (request.MachineType == "M") // Montaj (ana sorgu)
                {
                    // TODO: Montaj ana sorgu eklenecek
                    sql = @"
                        SELECT
                            'Montaj' AS VS_SERI_NO,
                            'MONTAJ001' AS VS_STOK_KODU,
                            'Montaj Malzemesi' AS STOK_ADI,
                            '-' AS HAMMADDE_LOT,
                            0.00 AS HARCANAN";
                }
                else // Serigrafi
                {
                    // TODO: Serigrafi için sorgu eklenecek
                    sql = @"
                        SELECT
                            'Serigrafi' AS VS_SERI_NO,
                            'SERI001' AS VS_STOK_KODU,
                            'Serigrafi Malzemesi' AS STOK_ADI,
                            '-' AS HAMMADDE_LOT,
                            0.00 AS HARCANAN";
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@SearchValue", request.SearchValue));

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    // MONTAJ_PART için farklı property isimleri
                    if (request.MachineType == "MONTAJ_PART")
                    {
                        results.Add(new
                        {
                            ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                            VS_STOK_KODU = reader["VS_STOK_KODU"]?.ToString() ?? "",
                            STOK_ADI = reader["STOK_ADI"]?.ToString() ?? "",
                            HAMMADDE_LOT = reader["HAMMADDE_LOT"]?.ToString() ?? "",
                            HARCANAN = Convert.ToDecimal(reader["HARCANAN"])
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            VS_SERI_NO = reader["VS_SERI_NO"]?.ToString() ?? "",
                            VS_STOK_KODU = reader["VS_STOK_KODU"]?.ToString() ?? "",
                            STOK_ADI = reader["STOK_ADI"]?.ToString() ?? "",
                            HAMMADDE_LOT = reader["HAMMADDE_LOT"]?.ToString() ?? "",
                            HARCANAN = Convert.ToDecimal(reader["HARCANAN"])
                        });
                    }
                }

                await connection.CloseAsync();

                return Ok(new
                {
                    success = true,
                    data = results,
                    count = results.Count,
                    machineType = request.MachineType,
                    searchValue = request.SearchValue,
                    message = $"{request.MachineType} makinesi için {results.Count} sarf malzeme kaydı bulundu."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Sarf malzeme verileri alınırken hata oluştu: {ex.Message}",
                    searchValue = request.SearchValue
                });
            }
        }

        [HttpPost("montaj-parts")]
        public async Task<IActionResult> GetMontajParts([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Arama değeri boş olamaz!"
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Montaj parçalarını belirlemek için sorgu
                var sql = @"
                    SELECT DISTINCT S.ISEMRI_NO, R.URUN_TIP
                    FROM _RP_URETIM_SERI_TAKIP_MONTAJ R
                    LEFT OUTER JOIN _RP_URETIM_SERI S WITH (NOLOCK) ON S.SERI_NO=R.SERI_NO
                    WHERE URET_ID IN (
                        SELECT ID FROM _RP_URETIM_SERI
                        WHERE SERI_NO = @SearchValue
                           OR LOT_NO = @SearchValue
                           OR ISEMRI_NO = @SearchValue
                    ) AND R.STOK_KODU NOT LIKE '150%'";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@SearchValue", request.SearchValue));

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        URUN_TIP = reader["URUN_TIP"]?.ToString() ?? ""
                    });
                }

                await connection.CloseAsync();

                // Gövde ve Kapak parçalarını ayır
                var govdeParts = results.Where(r => ((dynamic)r).URUN_TIP != "02").ToList();
                var kapakParts = results.Where(r => ((dynamic)r).URUN_TIP == "02").ToList();

                return Ok(new
                {
                    success = true,
                    govdeParts = govdeParts,
                    kapakParts = kapakParts,
                    totalParts = results.Count,
                    searchValue = request.SearchValue,
                    message = $"Montaj parçaları belirlendi: {govdeParts.Count} gövde, {kapakParts.Count} kapak"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Montaj parçaları sorgulanırken hata oluştu: {ex.Message}",
                    searchValue = request.SearchValue
                });
            }
        }

        [HttpGet("test-montaj-records")]
        public async Task<IActionResult> GetTestMontajRecords()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Montaj kayıtlarını test etmek için örnek veriler
                var sql = @"
                    SELECT TOP 10
                        S.SERI_NO, S.ISEMRI_NO, S.LOT_NO,
                        COUNT(DISTINCT CASE WHEN R.URUN_TIP != '02' THEN R.URUN_TIP END) as GOVDE_COUNT,
                        COUNT(DISTINCT CASE WHEN R.URUN_TIP = '02' THEN R.URUN_TIP END) as KAPAK_COUNT
                    FROM _RP_URETIM_SERI_TAKIP_MONTAJ R
                    LEFT OUTER JOIN _RP_URETIM_SERI S WITH (NOLOCK) ON S.SERI_NO=R.SERI_NO
                    WHERE R.STOK_KODU NOT LIKE '150%'
                      AND (S.SERI_NO IS NOT NULL AND S.SERI_NO != '')
                    GROUP BY S.SERI_NO, S.ISEMRI_NO, S.LOT_NO
                    HAVING COUNT(DISTINCT CASE WHEN R.URUN_TIP != '02' THEN R.URUN_TIP END) > 1
                    ORDER BY GOVDE_COUNT DESC";

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        SERI_NO = reader["SERI_NO"]?.ToString() ?? "",
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        LOT_NO = reader["LOT_NO"]?.ToString() ?? "",
                        GOVDE_COUNT = Convert.ToInt32(reader["GOVDE_COUNT"]),
                        KAPAK_COUNT = Convert.ToInt32(reader["KAPAK_COUNT"])
                    });
                }

                await connection.CloseAsync();

                return Ok(new
                {
                    success = true,
                    data = results,
                    count = results.Count,
                    message = $"{results.Count} çoklu gövdeli montaj kaydı bulundu."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Test montaj kayıtları sorgulanırken hata oluştu: {ex.Message}"
                });
            }
        }
    }

    public class SearchRequest
    {
        public string SearchType { get; set; } = "";
        public string SearchValue { get; set; } = "";
    }

    public class MaterialConsumptionRequest
    {
        public string SearchValue { get; set; } = "";
        public string MachineType { get; set; } = "";
    }
}

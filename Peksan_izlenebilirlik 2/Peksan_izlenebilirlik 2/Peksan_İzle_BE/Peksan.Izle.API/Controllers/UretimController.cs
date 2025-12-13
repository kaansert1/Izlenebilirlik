using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;
using Peksan.Izle.API.Models;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UretimController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UretimController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("seri-uretim/{seriNo}")]
        public async Task<IActionResult> GetUretimBySeriNo(string seriNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(seriNo))
                {
                    return BadRequest(new UretimSeriResponse
                    {
                        Success = false,
                        Message = "Seri numarası boş olamaz!",
                        SeriNo = seriNo
                    });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        ISEMRI_NO,
                        STOK_KODU,
                        CONVERT(NUMERIC(18,0),SUM(ADET)) AS ADET,
                        CONVERT(NUMERIC(18,2),SUM(NET)) AS NET_KG,
                        CONVERT(NUMERIC(18,2),SUM(BRUT)) AS BRUT_KG,
                        COUNT(ID) AS KOLI_SAYISI,
                        CASE 
                            WHEN URET_TIP='0' THEN 'Üretim' 
                            WHEN URET_TIP='1' THEN 'Numune' 
                            WHEN URET_TIP='2' THEN 'Fire' 
                            WHEN URET_TIP='3' THEN 'Renk Geçişi' 
                            WHEN URET_TIP='5' THEN 'Yarım Koli' 
                            ELSE 'x' 
                        END AS URET_TIP
                    FROM _RP_URETIM_SERI
                    WHERE ISEMRI_NO IN (
                        SELECT ISEMRI_NO 
                        FROM _RP_URETIM_SERI 
                        WHERE SERI_NO = @SeriNo
                    )
                    GROUP BY ISEMRI_NO, STOK_KODU, URET_TIP
                    ORDER BY ADET DESC";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@SeriNo", seriNo));

                var results = new List<UretimSeriModel>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new UretimSeriModel
                    {
                        ISEMRI_NO = reader["ISEMRI_NO"]?.ToString() ?? "",
                        STOK_KODU = reader["STOK_KODU"]?.ToString() ?? "",
                        ADET = Convert.ToDecimal(reader["ADET"]),
                        NET_KG = Convert.ToDecimal(reader["NET_KG"]),
                        BRUT_KG = Convert.ToDecimal(reader["BRUT_KG"]),
                        KOLI_SAYISI = Convert.ToInt32(reader["KOLI_SAYISI"]),
                        URET_TIP = reader["URET_TIP"]?.ToString() ?? ""
                    });
                }

                await connection.CloseAsync();

                return Ok(new UretimSeriResponse
                {
                    Success = true,
                    Message = results.Count > 0 ? "Üretim verileri başarıyla getirildi!" : "Bu seri numarası için veri bulunamadı.",
                    Data = results,
                    Count = results.Count,
                    SeriNo = seriNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new UretimSeriResponse
                {
                    Success = false,
                    Message = $"Üretim verileri getirilirken hata oluştu: {ex.Message}",
                    SeriNo = seriNo
                });
            }
        }

        [HttpPost("seri-uretim")]
        public async Task<IActionResult> GetUretimBySeriNoPost([FromBody] UretimSeriRequest request)
        {
            return await GetUretimBySeriNo(request.SeriNo);
        }
    }
}

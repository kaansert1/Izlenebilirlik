using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Data;
using Peksan.Izle.API.Models;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("connection-test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Daha agresif test - gerçek bir sorgu çalıştır
                await _context.Database.OpenConnectionAsync();
                var connection = _context.Database.GetDbConnection();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var result = await command.ExecuteScalarAsync();

                await _context.Database.CloseConnectionAsync();

                return Ok(new {
                    success = true,
                    message = "Veritabanı bağlantısı başarılı!",
                    result = result,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Veritabanı bağlantısı sırasında hata oluştu!",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("database-info")]
        public IActionResult GetDatabaseInfo()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var databaseName = _context.Database.GetDbConnection().Database;
                
                return Ok(new { 
                    success = true,
                    connectionString = connectionString?.Replace("Password=", "Password=***"), // Güvenlik için şifreyi gizle
                    databaseName = databaseName,
                    timestamp = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Veritabanı bilgileri alınırken hata oluştu!", 
                    error = ex.Message,
                    timestamp = DateTime.Now 
                });
            }
        }

        [HttpPost("create-test-table")]
        public async Task<IActionResult> CreateTestTable()
        {
            try
            {
                // Veritabanında tablo oluştur
                await _context.Database.EnsureCreatedAsync();
                
                return Ok(new { 
                    success = true, 
                    message = "Test tablosu başarıyla oluşturuldu!", 
                    timestamp = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Test tablosu oluşturulurken hata oluştu!", 
                    error = ex.Message,
                    timestamp = DateTime.Now 
                });
            }
        }

        [HttpPost("add-test-data")]
        public async Task<IActionResult> AddTestData([FromBody] TestConnection testData)
        {
            try
            {
                _context.TestConnections.Add(testData);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    success = true, 
                    message = "Test verisi başarıyla eklendi!", 
                    data = testData,
                    timestamp = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Test verisi eklenirken hata oluştu!", 
                    error = ex.Message,
                    timestamp = DateTime.Now 
                });
            }
        }

        [HttpGet("get-test-data")]
        public async Task<IActionResult> GetTestData()
        {
            try
            {
                var testData = await _context.TestConnections.ToListAsync();

                return Ok(new {
                    success = true,
                    data = testData,
                    count = testData.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Test verileri alınırken hata oluştu!",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("list-tables")]
        public async Task<IActionResult> ListTables()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        TABLE_SCHEMA,
                        TABLE_NAME,
                        TABLE_TYPE
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_SCHEMA, TABLE_NAME";

                var tables = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(new
                    {
                        Schema = reader["TABLE_SCHEMA"].ToString(),
                        Name = reader["TABLE_NAME"].ToString(),
                        Type = reader["TABLE_TYPE"].ToString()
                    });
                }

                await connection.CloseAsync();

                return Ok(new {
                    success = true,
                    message = "Tablolar başarıyla listelendi!",
                    tables = tables,
                    count = tables.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Tablolar listelenirken hata oluştu!",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("sample-seri")]
        public async Task<IActionResult> GetSampleSeriNo()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TOP 5 SERI_NO, ISEMRI_NO, STOK_KODU
                    FROM _RP_URETIM_SERI
                    WHERE SERI_NO IS NOT NULL AND SERI_NO != ''
                    ORDER BY ID DESC";

                var samples = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    samples.Add(new
                    {
                        seriNo = reader["SERI_NO"]?.ToString() ?? "",
                        isemriNo = reader["ISEMRI_NO"]?.ToString() ?? "",
                        stokKodu = reader["STOK_KODU"]?.ToString() ?? ""
                    });
                }

                await connection.CloseAsync();

                return Ok(new {
                    success = true,
                    message = "Örnek seri numaraları getirildi!",
                    samples = samples,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Örnek seri numaraları alınırken hata oluştu!",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}

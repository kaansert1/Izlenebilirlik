using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificationController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _pdfBasePath = @"\\192.168.2.250\ortak\KALİTE\BRC ÇALIŞMASI 2020\KALİTE KONTROL BÖLÜMÜ\FORMLAR\Kalite Sertifikaları\";

        public CertificationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] CertificationSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Sertifikasyon araması: {request.SearchType} = {request.SearchValue}");

                // Şimdilik mock veri döndürelim
                var mockResults = new List<CertificationResult>();

                if (request.SearchType == "lot_no")
                {
                    // Lot No'ları virgülle ayır
                    var lotNumbers = request.SearchValue.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    foreach (var lotNo in lotNumbers)
                    {
                        mockResults.AddRange(new[]
                        {
                            new CertificationResult
                            {
                                IsemriNo = "2024001234",
                                Kod3 = "CSP4",
                                GrupIsim = "Cam Şişe Plastik",
                                AnalizeNumber = "029.018CSP4-B-20250103-58070"
                            },
                            new CertificationResult
                            {
                                IsemriNo = "2024001235",
                                Kod3 = "CSP5",
                                GrupIsim = "Cam Şişe Plastik",
                                AnalizeNumber = "029.018CSP5-B-20250103-58071"
                            }
                        });
                    }
                }
                else if (request.SearchType == "seri_no")
                {
                    mockResults.Add(new CertificationResult
                    {
                        IsemriNo = "2024001236",
                        Kod3 = "CSP6",
                        GrupIsim = "Cam Şişe Plastik",
                        AnalizeNumber = "029.018CSP6-B-20250103-58072"
                    });
                }

                return Ok(new { success = true, results = mockResults });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sertifikasyon arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("preview-pdf")]
        public async Task<IActionResult> PreviewPdf([FromBody] PdfRequest request)
        {
            try
            {
                var pdfPath = Path.Combine(_pdfBasePath, $"{request.AnalizeNumber}.pdf");
                Console.WriteLine($"PDF önizleme: {pdfPath}");

                // Şimdilik mock PDF döndürelim
                // Gerçek implementasyonda dosya varlığını kontrol edip döndüreceğiz
                if (System.IO.File.Exists(pdfPath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                    return File(fileBytes, "application/pdf");
                }
                else
                {
                    // Mock PDF için boş bir PDF döndür
                    return NotFound(new { success = false, message = "PDF dosyası bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF önizleme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("download-pdf")]
        public async Task<IActionResult> DownloadPdf([FromBody] PdfRequest request)
        {
            try
            {
                var pdfPath = Path.Combine(_pdfBasePath, $"{request.AnalizeNumber}.pdf");
                Console.WriteLine($"PDF indirme: {pdfPath}");

                if (System.IO.File.Exists(pdfPath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                    return File(fileBytes, "application/pdf", $"{request.AnalizeNumber}.pdf");
                }
                else
                {
                    return NotFound(new { success = false, message = "PDF dosyası bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF indirme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Gerçek veritabanı sorguları için method (şimdilik kullanılmıyor)
        private async Task<List<CertificationResult>> GetCertificationDataFromDatabase(string searchType, string searchValue)
        {
            var results = new List<CertificationResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                if (searchType == "lot_no")
                {
                    // Lot No'ları virgülle ayır ve SQL IN clause için hazırla
                    var lotNumbers = searchValue.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => $"'{x}'")
                        .ToList();

                    var lotNoList = string.Join(",", lotNumbers);

                    // İlk sorgu: Lot No'dan İş Emri No'ları bul
                    var firstQuery = $@"
                        SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                        FROM _RP_URETIM_SERI A
                        LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                        LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                        WHERE SERI_NO IN 
                        (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                        (SELECT ID FROM _RP_URETIM_SERI WHERE LOT_NO IN ({lotNoList}) AND URET_TIP=0))";

                    using (var command = new SqlCommand(firstQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var isemriNumbers = new List<string>();
                            var tempResults = new List<CertificationResult>();

                            while (await reader.ReadAsync())
                            {
                                var isemriNo = reader["ISEMRI_NO"]?.ToString();
                                if (!string.IsNullOrEmpty(isemriNo))
                                {
                                    isemriNumbers.Add($"'{isemriNo}'");
                                    tempResults.Add(new CertificationResult
                                    {
                                        IsemriNo = isemriNo,
                                        Kod3 = reader["KOD_3"]?.ToString(),
                                        GrupIsim = reader["GRUP_ISIM"]?.ToString()
                                    });
                                }
                            }

                            reader.Close();

                            // İkinci sorgu: İş Emri No'larından ANALIZE_NUMBER'ları al
                            if (isemriNumbers.Any())
                            {
                                var isemriList = string.Join(",", isemriNumbers);
                                var secondQuery = $@"
                                    SELECT DISTINCT ISEMRINO, ANALIZE_NUMBER
                                    FROM ETIKET..ANC_VW_ANALIZE_DOCUMENT
                                    WHERE ISEMRINO IN ({isemriList})";

                                // Her benzersiz ANALIZE_NUMBER için ayrı kayıt oluştur
                                var addedAnalizeNumbers = new HashSet<string>();

                                using (var command2 = new SqlCommand(secondQuery, connection))
                                {
                                    using (var reader2 = await command2.ExecuteReaderAsync())
                                    {
                                        while (await reader2.ReadAsync())
                                        {
                                            var isemriNo = reader2["ISEMRINO"]?.ToString();
                                            var analizeNumber = reader2["ANALIZE_NUMBER"]?.ToString();

                                            // Her benzersiz ANALIZE_NUMBER için ayrı kayıt ekle
                                            if (!string.IsNullOrEmpty(isemriNo) && !string.IsNullOrEmpty(analizeNumber) &&
                                                !addedAnalizeNumbers.Contains(analizeNumber))
                                            {
                                                var existingResult = tempResults.FirstOrDefault(x => x.IsemriNo == isemriNo);
                                                if (existingResult != null)
                                                {
                                                    // Her analiz numarası için yeni bir kopya oluştur
                                                    var newResult = new CertificationResult
                                                    {
                                                        IsemriNo = existingResult.IsemriNo,
                                                        Kod3 = existingResult.Kod3,
                                                        GrupIsim = existingResult.GrupIsim,
                                                        AnalizeNumber = analizeNumber
                                                    };
                                                    results.Add(newResult);
                                                    addedAnalizeNumbers.Add(analizeNumber);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
    }

    public class CertificationSearchRequest
    {
        public string SearchType { get; set; }
        public string SearchValue { get; set; }
    }

    public class PdfRequest
    {
        public string AnalizeNumber { get; set; }
    }

    public class CertificationResult
    {
        public string IsemriNo { get; set; }
        public string Kod3 { get; set; }
        public string GrupIsim { get; set; }
        public string AnalizeNumber { get; set; }
    }
}

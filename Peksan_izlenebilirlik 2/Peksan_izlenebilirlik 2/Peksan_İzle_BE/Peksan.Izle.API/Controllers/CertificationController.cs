using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;

namespace Peksan.Izle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificationController : ControllerBase
    {
        private readonly string _connectionString;
        // Kalite Sertifikaları için path'ler
        private readonly string[] _pdfPaths = {
            "/Volumes/ortak/KALİTE/BRC ÇALIŞMASI 2020/KALİTE KONTROL BÖLÜMÜ/FORMLAR/Kalite Sertifikaları",
            "//192.168.2.250/ortak/KALİTE/BRC ÇALIŞMASI 2020/KALİTE KONTROL BÖLÜMÜ/FORMLAR/Kalite Sertifikaları"
        };

        // İlk Numune için path'ler
        private readonly string[] _ilkNumunePdfPaths = {
            "/Volumes/ortak/KALİTE/BRC ÇALIŞMASI 2020/KALİTE KONTROL BÖLÜMÜ/FORMLAR/İlk Üretim Kontrol Sertifikaları",
            "//192.168.2.250/ortak/KALİTE/BRC ÇALIŞMASI 2020/KALİTE KONTROL BÖLÜMÜ/FORMLAR/İlk Üretim Kontrol Sertifikaları"
        };

        // HTTP URL for PDF access
        private readonly string _pdfBaseUrl = "http://192.168.2.250:8080/";
        private readonly HttpClient _httpClient = new HttpClient();

        // Network authentication için Windows API
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        public CertificationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private bool AuthenticateNetworkPath()
        {
            try
            {
                IntPtr token = IntPtr.Zero;
                bool success = LogonUser("administrator", "PEKSAN", "Peks@n2021!+",
                    3, // LOGON32_LOGON_NETWORK
                    0, // LOGON32_PROVIDER_DEFAULT
                    out token);

                if (success && token != IntPtr.Zero)
                {
                    CloseHandle(token);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network authentication hatası: {ex.Message}");
                return false;
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Certification Controller çalışıyor!", timestamp = DateTime.Now });
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] CertificationSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Sertifikasyon araması: {request.SearchType} = {request.SearchValue}");

                List<CertificationResult> results;

                if (request.SearchType == "lot_no")
                {
                    results = await GetCertificationDataFromDatabase(request.SearchType, request.SearchValue);
                }
                else if (request.SearchType == "seri_no")
                {
                    results = await GetCertificationDataFromSeriNo(request.SearchValue);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Geçersiz arama tipi" });
                }

                Console.WriteLine($"Sertifikasyon sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sertifikasyon arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("search-ilk-numune")]
        public async Task<IActionResult> SearchIlkNumune([FromBody] SearchRequest request)
        {
            try
            {
                Console.WriteLine($"İlk Numune arama isteği: {request?.SearchType} - {request?.SearchValue}");

                if (request == null || string.IsNullOrEmpty(request.SearchValue))
                {
                    return BadRequest(new { success = false, message = "Arama değeri gerekli" });
                }

                var results = new List<IlkNumuneResult>();

                if (request.SearchType == "lot_no")
                {
                    results = await GetIlkNumuneDataFromLotNo(request.SearchValue);
                }
                else if (request.SearchType == "seri_no")
                {
                    results = await GetIlkNumuneDataFromSeriNo(request.SearchValue);
                }

                Console.WriteLine($"İlk Numune sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İlk Numune arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("search-is-emri")]
        public async Task<IActionResult> SearchIsEmri([FromBody] SearchRequest request)
        {
            try
            {
                Console.WriteLine($"İş Emri arama isteği: {request?.SearchType} - {request?.SearchValue}");

                if (request == null || string.IsNullOrEmpty(request.SearchValue))
                {
                    return BadRequest(new { success = false, message = "SearchValue gerekli" });
                }

                List<IsEmriResult> results = new List<IsEmriResult>();

                if (request.SearchType == "lot_no")
                {
                    results = await GetIsEmriDataFromLotNo(request.SearchValue);
                }
                else if (request.SearchType == "seri_no")
                {
                    results = await GetIsEmriDataFromSeriNo(request.SearchValue);
                }

                Console.WriteLine($"İş Emri sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İş Emri arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("search-kutle-denkligi")]
        public async Task<IActionResult> SearchKutleDenkligi([FromBody] SearchRequest request)
        {
            try
            {
                Console.WriteLine($"Kütle Denkliği arama isteği: {request?.SearchType} - {request?.SearchValue}");

                if (request == null || string.IsNullOrEmpty(request.SearchValue))
                {
                    return BadRequest(new { success = false, message = "SearchValue gerekli" });
                }

                List<KutleDenkligiResult> results = new List<KutleDenkligiResult>();

                if (request.SearchType == "lot_no")
                {
                    results = await GetKutleDenkligiDataFromLotNo(request.SearchValue);
                }
                else if (request.SearchType == "seri_no")
                {
                    results = await GetKutleDenkligiDataFromSeriNo(request.SearchValue);
                }

                Console.WriteLine($"Kütle Denkliği sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kütle Denkliği arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("preview-pdf")]
        public async Task<IActionResult> PreviewPdf([FromBody] PdfRequest request)
        {
            try
            {
                Console.WriteLine($"PDF önizleme isteği alındı: {request?.AnalizeNumber}");

                if (request == null || string.IsNullOrEmpty(request.AnalizeNumber))
                {
                    return BadRequest(new { success = false, message = "AnalizeNumber gerekli" });
                }

                string? foundPdfPath = null;

                // Tüm path'leri deneyelim
                foreach (var basePath in _pdfPaths)
                {
                    var pdfPath = Path.Combine(basePath, request.AnalizeNumber + ".pdf");
                    Console.WriteLine($"PDF aranıyor: {pdfPath}");
                    Console.WriteLine($"Base path var mı: {Directory.Exists(basePath)}");
                    Console.WriteLine($"PDF dosyası var mı: {System.IO.File.Exists(pdfPath)}");

                    if (System.IO.File.Exists(pdfPath))
                    {
                        foundPdfPath = pdfPath;
                        Console.WriteLine($"PDF bulundu: {foundPdfPath}");
                        break;
                    }
                }

                // Dosya var mı kontrol et
                if (!string.IsNullOrEmpty(foundPdfPath))
                {
                    Console.WriteLine("PDF dosyası bulundu, gönderiliyor...");
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(foundPdfPath);
                    return File(fileBytes, "application/pdf");
                }
                else
                {
                    Console.WriteLine("PDF dosyası bulunamadı");
                    return Ok(new { success = false, message = "PDF dosyası bulunamadı", paths = _pdfPaths });
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
                Console.WriteLine($"PDF indirme isteği alındı: {request?.AnalizeNumber}");

                if (request == null || string.IsNullOrEmpty(request.AnalizeNumber))
                {
                    return BadRequest(new { success = false, message = "AnalizeNumber gerekli" });
                }

                string? foundPdfPath = null;

                // Tüm path'leri deneyelim
                foreach (var basePath in _pdfPaths)
                {
                    var pdfPath = Path.Combine(basePath, request.AnalizeNumber + ".pdf");
                    Console.WriteLine($"PDF indiriliyor: {pdfPath}");
                    Console.WriteLine($"PDF dosyası var mı: {System.IO.File.Exists(pdfPath)}");

                    if (System.IO.File.Exists(pdfPath))
                    {
                        foundPdfPath = pdfPath;
                        Console.WriteLine($"PDF bulundu: {foundPdfPath}");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(foundPdfPath))
                {
                    Console.WriteLine("PDF dosyası bulundu, indiriliyor...");
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(foundPdfPath);
                    return File(fileBytes, "application/pdf", $"{request.AnalizeNumber}.pdf");
                }
                else
                {
                    Console.WriteLine("PDF dosyası bulunamadı");
                    return Ok(new { success = false, message = "PDF dosyası bulunamadı", paths = _pdfPaths });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF indirme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("preview-ilk-numune-pdf")]
        public async Task<IActionResult> PreviewIlkNumunePdf([FromBody] PdfRequest request)
        {
            try
            {
                Console.WriteLine($"İlk Numune PDF önizleme isteği alındı: {request?.AnalizeNumber}");

                if (request == null || string.IsNullOrEmpty(request.AnalizeNumber))
                {
                    return BadRequest(new { success = false, message = "AnalizeNumber gerekli" });
                }

                string? foundPdfPath = null;

                // İlk Numune path'lerini deneyelim
                foreach (var basePath in _ilkNumunePdfPaths)
                {
                    var pdfPath = Path.Combine(basePath, $"{request.AnalizeNumber}.pdf");
                    Console.WriteLine($"İlk Numune PDF path deneniyor: {pdfPath}");

                    if (System.IO.File.Exists(pdfPath))
                    {
                        foundPdfPath = pdfPath;
                        Console.WriteLine($"İlk Numune PDF bulundu: {foundPdfPath}");
                        break;
                    }
                }

                if (foundPdfPath != null)
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(foundPdfPath);
                    return File(fileBytes, "application/pdf");
                }
                else
                {
                    Console.WriteLine($"İlk Numune PDF bulunamadı: {request.AnalizeNumber}");
                    return NotFound(new { success = false, message = "PDF dosyası bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İlk Numune PDF önizleme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("download-ilk-numune-pdf")]
        public async Task<IActionResult> DownloadIlkNumunePdf([FromBody] PdfRequest request)
        {
            try
            {
                Console.WriteLine($"İlk Numune PDF indirme isteği alındı: {request?.AnalizeNumber}");

                if (request == null || string.IsNullOrEmpty(request.AnalizeNumber))
                {
                    return BadRequest(new { success = false, message = "AnalizeNumber gerekli" });
                }

                string? foundPdfPath = null;

                // İlk Numune path'lerini deneyelim
                foreach (var basePath in _ilkNumunePdfPaths)
                {
                    var pdfPath = Path.Combine(basePath, $"{request.AnalizeNumber}.pdf");
                    Console.WriteLine($"İlk Numune PDF path deneniyor: {pdfPath}");

                    if (System.IO.File.Exists(pdfPath))
                    {
                        foundPdfPath = pdfPath;
                        Console.WriteLine($"İlk Numune PDF bulundu: {foundPdfPath}");
                        break;
                    }
                }

                if (foundPdfPath != null)
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(foundPdfPath);
                    return File(fileBytes, "application/pdf", $"{request.AnalizeNumber}.pdf");
                }
                else
                {
                    Console.WriteLine($"İlk Numune PDF bulunamadı: {request.AnalizeNumber}");
                    return NotFound(new { success = false, message = "PDF dosyası bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İlk Numune PDF indirme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Lot No için veritabanı sorguları
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
                    Console.WriteLine($"Lot No listesi: {lotNoList}");

                    // İlk sorgu: Lot No'dan İş Emri No'ları bul
                    var firstQuery = $@"
                        SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                        FROM _RP_URETIM_SERI A
                        LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                        LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                        WHERE SERI_NO IN
                        (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                        (SELECT ID FROM _RP_URETIM_SERI WHERE LOT_NO IN ({lotNoList}) AND URET_TIP=0))";

                    Console.WriteLine($"İlk sorgu: {firstQuery}");

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
                            Console.WriteLine($"İlk sorgudan {tempResults.Count} kayıt bulundu");

                            // İkinci sorgu: İş Emri No'larından ANALIZE_NUMBER'ları al
                            if (isemriNumbers.Any())
                            {
                                var isemriList = string.Join(",", isemriNumbers);
                                var secondQuery = $@"
                                    SELECT DISTINCT ISEMRINO, ANALIZE_NUMBER
                                    FROM ETIKET..ANC_VW_ANALIZE_DOCUMENT
                                    WHERE ISEMRINO IN ({isemriList})";

                                Console.WriteLine($"İkinci sorgu: {secondQuery}");

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

            Console.WriteLine($"Toplam {results.Count} sertifikasyon kaydı bulundu");
            return results;
        }

        // Seri No için veritabanı sorguları
        private async Task<List<CertificationResult>> GetCertificationDataFromSeriNo(string seriNo)
        {
            var results = new List<CertificationResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Seri No'dan direkt İş Emri No'sunu bul
                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                    FROM _RP_URETIM_SERI A
                    LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                    LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                    WHERE SERI_NO = '{seriNo}'";

                Console.WriteLine($"Seri No sorgusu: {firstQuery}");

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

            return results;
        }

        // İlk Numune - Lot No için veritabanı sorguları
        private async Task<List<IlkNumuneResult>> GetIlkNumuneDataFromLotNo(string lotNoValue)
        {
            var results = new List<IlkNumuneResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Lot No'ları virgülle ayırarak liste haline getir
                var lotNumbers = lotNoValue.Split(',').Select(x => $"'{x.Trim()}'").ToArray();
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

                Console.WriteLine($"İlk Numune - İlk sorgu: {firstQuery}");

                var isemriNumbers = new List<string>();

                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(isemriNo) && !isemriNumbers.Contains(isemriNo))
                            {
                                isemriNumbers.Add(isemriNo);
                            }
                        }
                    }
                }

                if (isemriNumbers.Count > 0)
                {
                    // İkinci sorgu: İş Emri No'larından İlk Numune analiz numaralarını bul
                    var isemriList = string.Join("','", isemriNumbers);
                    var secondQuery = $@"
                        SELECT DISTINCT M.ISEMRI_NO, A.ANALIZE_NUMBER
                        FROM ETIKET..ANC_CAVITY_MEASURE_ENTRY_VALUE A
                        LEFT JOIN PEKSAN25.._RP_URETIM_SERI M ON A.PID = M.SERI_NO
                        WHERE M.ISEMRI_NO IN ('{isemriList}')";

                    Console.WriteLine($"İlk Numune - İkinci sorgu: {secondQuery}");

                    // Her benzersiz ANALIZE_NUMBER için ayrı kayıt oluştur
                    var addedAnalizeNumbers = new HashSet<string>();

                    using (var command2 = new SqlCommand(secondQuery, connection))
                    {
                        using (var reader2 = await command2.ExecuteReaderAsync())
                        {
                            while (await reader2.ReadAsync())
                            {
                                var isemriNo = reader2["ISEMRI_NO"]?.ToString() ?? "";
                                var analizeNumber = reader2["ANALIZE_NUMBER"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(analizeNumber) && !addedAnalizeNumbers.Contains(analizeNumber))
                                {
                                    var newResult = new IlkNumuneResult
                                    {
                                        IsemriNo = isemriNo,
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

            Console.WriteLine($"İlk Numune - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        // İlk Numune - Seri No için veritabanı sorguları
        private async Task<List<IlkNumuneResult>> GetIlkNumuneDataFromSeriNo(string seriNo)
        {
            var results = new List<IlkNumuneResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Seri No'dan direkt İş Emri No'sunu bul
                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO
                    FROM _RP_URETIM_SERI A
                    WHERE SERI_NO = '{seriNo}'";

                Console.WriteLine($"İlk Numune - Seri No sorgusu: {firstQuery}");

                var isemriNumbers = new List<string>();

                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(isemriNo) && !isemriNumbers.Contains(isemriNo))
                            {
                                isemriNumbers.Add(isemriNo);
                            }
                        }
                    }
                }

                if (isemriNumbers.Count > 0)
                {
                    // İkinci sorgu: İş Emri No'larından İlk Numune analiz numaralarını bul
                    var isemriList = string.Join("','", isemriNumbers);
                    var secondQuery = $@"
                        SELECT DISTINCT M.ISEMRI_NO, A.ANALIZE_NUMBER
                        FROM ETIKET..ANC_CAVITY_MEASURE_ENTRY_VALUE A
                        LEFT JOIN PEKSAN25.._RP_URETIM_SERI M ON A.PID = M.SERI_NO
                        WHERE M.ISEMRI_NO IN ('{isemriList}')";

                    Console.WriteLine($"İlk Numune - İkinci sorgu: {secondQuery}");

                    var addedAnalizeNumbers = new HashSet<string>();

                    using (var command2 = new SqlCommand(secondQuery, connection))
                    {
                        using (var reader2 = await command2.ExecuteReaderAsync())
                        {
                            while (await reader2.ReadAsync())
                            {
                                var isemriNo = reader2["ISEMRI_NO"]?.ToString() ?? "";
                                var analizeNumber = reader2["ANALIZE_NUMBER"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(analizeNumber) && !addedAnalizeNumbers.Contains(analizeNumber))
                                {
                                    var newResult = new IlkNumuneResult
                                    {
                                        IsemriNo = isemriNo,
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

            Console.WriteLine($"İlk Numune - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        // İş Emri - Lot No için veritabanı sorguları
        private async Task<List<IsEmriResult>> GetIsEmriDataFromLotNo(string lotNoValue)
        {
            var results = new List<IsEmriResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // İlk sorgu: Lot No'dan İş Emri numaralarını al
                var lotNumbers = lotNoValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var lotNoList = string.Join("','", lotNumbers);

                Console.WriteLine($"İş Emri - İlk sorgu: Lot No listesi: '{lotNoList}'");

                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                    FROM _RP_URETIM_SERI A
                    LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                    LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                    WHERE SERI_NO IN
                    (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                    (SELECT ID FROM _RP_URETIM_SERI WHERE LOT_NO IN ('{lotNoList}') AND URET_TIP=0))";

                Console.WriteLine($"İş Emri - İlk sorgu: {firstQuery}");

                var isemriNumbers = new List<string>();
                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString();
                            if (!string.IsNullOrEmpty(isemriNo) && !isemriNumbers.Contains(isemriNo))
                            {
                                isemriNumbers.Add(isemriNo);
                            }
                        }
                    }
                }

                Console.WriteLine($"İş Emri - İlk sorgudan {isemriNumbers.Count} iş emri bulundu");

                // İkinci sorgu: İş Emri numaralarından stok kodlarını al
                var stokKodlari = new Dictionary<string, string>();
                if (isemriNumbers.Count > 0)
                {
                    var isemriList = string.Join("','", isemriNumbers);
                    var stokKoduQuery = $"SELECT ISEMRINO,STOK_KODU FROM TBLISEMRI WHERE ISEMRINO IN ('{isemriList}')";
                    Console.WriteLine($"İş Emri - Stok kodu sorgusu: {stokKoduQuery}");

                    using (var stokCommand = new SqlCommand(stokKoduQuery, connection))
                    {
                        using (var stokReader = await stokCommand.ExecuteReaderAsync())
                        {
                            while (await stokReader.ReadAsync())
                            {
                                var isemriNo = stokReader["ISEMRINO"]?.ToString();
                                var stokKodu = stokReader["STOK_KODU"]?.ToString();
                                if (!string.IsNullOrEmpty(isemriNo) && !string.IsNullOrEmpty(stokKodu))
                                {
                                    stokKodlari[isemriNo] = stokKodu;
                                }
                            }
                        }
                    }
                    Console.WriteLine($"İş Emri - {stokKodlari.Count} stok kodu bulundu");
                }

                // Üçüncü sorgu: İş Emri numaralarından TIID'leri al
                if (isemriNumbers.Count > 0)
                {
                    foreach (var isemriNo in isemriNumbers)
                    {
                        var tiidQuery = $"SELECT TOP 1 TIID FROM eflow..INST_TASKS WHERE SUBJECT LIKE '%{isemriNo}%' ORDER BY TIID DESC";
                        Console.WriteLine($"İş Emri - TIID sorgusu: {tiidQuery}");

                        using (var command2 = new SqlCommand(tiidQuery, connection))
                        {
                            using (var reader2 = await command2.ExecuteReaderAsync())
                            {
                                while (await reader2.ReadAsync())
                                {
                                    var tiid = reader2["TIID"]?.ToString();
                                    if (!string.IsNullOrEmpty(tiid))
                                    {
                                        var url = $"http://192.168.2.251:82/Task/PrinterFriendlyDetail?TIID={tiid}";
                                        var stokKodu = stokKodlari.ContainsKey(isemriNo) ? stokKodlari[isemriNo] : "";

                                        results.Add(new IsEmriResult
                                        {
                                            IsemriNo = isemriNo,
                                            StokKodu = stokKodu,
                                            TIID = tiid,
                                            Url = url
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"İş Emri - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        // İş Emri - Seri No için veritabanı sorguları
        private async Task<List<IsEmriResult>> GetIsEmriDataFromSeriNo(string seriNo)
        {
            var results = new List<IsEmriResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // İlk sorgu: Seri No'dan İş Emri numarasını al
                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                    FROM _RP_URETIM_SERI A
                    LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                    LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                    WHERE SERI_NO IN
                    (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                    (SELECT ID FROM _RP_URETIM_SERI WHERE SERI_NO = '{seriNo}' AND URET_TIP=0))";

                Console.WriteLine($"İş Emri - İlk sorgu: {firstQuery}");

                var isemriNumbers = new List<string>();
                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString();
                            if (!string.IsNullOrEmpty(isemriNo) && !isemriNumbers.Contains(isemriNo))
                            {
                                isemriNumbers.Add(isemriNo);
                            }
                        }
                    }
                }

                Console.WriteLine($"İş Emri - İlk sorgudan {isemriNumbers.Count} iş emri bulundu");

                // İkinci sorgu: İş Emri numaralarından stok kodlarını al
                var stokKodlari = new Dictionary<string, string>();
                if (isemriNumbers.Count > 0)
                {
                    var isemriList = string.Join("','", isemriNumbers);
                    var stokKoduQuery = $"SELECT ISEMRINO,STOK_KODU FROM TBLISEMRI WHERE ISEMRINO IN ('{isemriList}')";
                    Console.WriteLine($"İş Emri - Stok kodu sorgusu: {stokKoduQuery}");

                    using (var stokCommand = new SqlCommand(stokKoduQuery, connection))
                    {
                        using (var stokReader = await stokCommand.ExecuteReaderAsync())
                        {
                            while (await stokReader.ReadAsync())
                            {
                                var isemriNo = stokReader["ISEMRINO"]?.ToString();
                                var stokKodu = stokReader["STOK_KODU"]?.ToString();
                                if (!string.IsNullOrEmpty(isemriNo) && !string.IsNullOrEmpty(stokKodu))
                                {
                                    stokKodlari[isemriNo] = stokKodu;
                                }
                            }
                        }
                    }
                    Console.WriteLine($"İş Emri - {stokKodlari.Count} stok kodu bulundu");
                }

                // Üçüncü sorgu: İş Emri numaralarından TIID'leri al
                if (isemriNumbers.Count > 0)
                {
                    foreach (var isemriNo in isemriNumbers)
                    {
                        var tiidQuery = $"SELECT TOP 1 TIID FROM eflow..INST_TASKS WHERE SUBJECT LIKE '%{isemriNo}%' ORDER BY TIID DESC";
                        Console.WriteLine($"İş Emri - TIID sorgusu: {tiidQuery}");

                        using (var command2 = new SqlCommand(tiidQuery, connection))
                        {
                            using (var reader2 = await command2.ExecuteReaderAsync())
                            {
                                while (await reader2.ReadAsync())
                                {
                                    var tiid = reader2["TIID"]?.ToString();
                                    if (!string.IsNullOrEmpty(tiid))
                                    {
                                        var url = $"http://192.168.2.251:82/Task/PrinterFriendlyDetail?TIID={tiid}";
                                        var stokKodu = stokKodlari.ContainsKey(isemriNo) ? stokKodlari[isemriNo] : "";

                                        results.Add(new IsEmriResult
                                        {
                                            IsemriNo = isemriNo,
                                            StokKodu = stokKodu,
                                            TIID = tiid,
                                            Url = url
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"İş Emri - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        // Kütle Denkliği - Lot No için veritabanı sorguları
        private async Task<List<KutleDenkligiResult>> GetKutleDenkligiDataFromLotNo(string lotNoValue)
        {
            var results = new List<KutleDenkligiResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // İlk sorgu: Lot No'dan İş Emri numaralarını al
                var lotNumbers = lotNoValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var lotNoList = string.Join("','", lotNumbers);

                Console.WriteLine($"Kütle Denkliği - İlk sorgu: Lot No listesi: '{lotNoList}'");

                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                    FROM _RP_URETIM_SERI A
                    LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                    LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                    WHERE SERI_NO IN
                    (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                    (SELECT ID FROM _RP_URETIM_SERI WHERE LOT_NO IN ('{lotNoList}') AND URET_TIP=0))";

                Console.WriteLine($"Kütle Denkliği - İlk sorgu: {firstQuery}");

                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var ustKapakSayac = 1;
                        var govdeSayac = 1;

                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString();
                            var kod3 = reader["KOD_3"]?.ToString();
                            var grupIsim = reader["GRUP_ISIM"]?.ToString();

                            if (!string.IsNullOrEmpty(isemriNo) && !string.IsNullOrEmpty(kod3))
                            {
                                string tabBaslik = "";

                                if (kod3 == "02")
                                {
                                    tabBaslik = $"Üst Kapak-{ustKapakSayac}";
                                    ustKapakSayac++;
                                }
                                else if (kod3 == "01")
                                {
                                    tabBaslik = $"Gövde-{govdeSayac}";
                                    govdeSayac++;
                                }

                                results.Add(new KutleDenkligiResult
                                {
                                    IsemriNo = isemriNo,
                                    Kod3 = kod3,
                                    GrupIsim = grupIsim ?? "",
                                    TabBaslik = tabBaslik
                                });
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Kütle Denkliği - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        // Kütle Denkliği - Seri No için veritabanı sorguları
        private async Task<List<KutleDenkligiResult>> GetKutleDenkligiDataFromSeriNo(string seriNo)
        {
            var results = new List<KutleDenkligiResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // İlk sorgu: Seri No'dan İş Emri numaralarını al
                var firstQuery = $@"
                    SELECT DISTINCT ISEMRI_NO, B.KOD_3, C.GRUP_ISIM
                    FROM _RP_URETIM_SERI A
                    LEFT OUTER JOIN TBLSTSABIT B ON A.STOK_KODU = B.STOK_KODU
                    LEFT OUTER JOIN TBLSTOKKOD3 C ON B.KOD_3 = C.GRUP_KOD
                    WHERE SERI_NO IN
                    (SELECT DISTINCT SERI_NO FROM _RP_URETIM_SERI_TAKIP_MONTAJ WHERE URET_ID IN
                    (SELECT ID FROM _RP_URETIM_SERI WHERE SERI_NO = '{seriNo}' AND URET_TIP=0))";

                Console.WriteLine($"Kütle Denkliği - İlk sorgu: {firstQuery}");

                using (var command = new SqlCommand(firstQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var ustKapakSayac = 1;
                        var govdeSayac = 1;

                        while (await reader.ReadAsync())
                        {
                            var isemriNo = reader["ISEMRI_NO"]?.ToString();
                            var kod3 = reader["KOD_3"]?.ToString();
                            var grupIsim = reader["GRUP_ISIM"]?.ToString();

                            if (!string.IsNullOrEmpty(isemriNo) && !string.IsNullOrEmpty(kod3))
                            {
                                string tabBaslik = "";

                                if (kod3 == "02")
                                {
                                    tabBaslik = $"Üst Kapak-{ustKapakSayac}";
                                    ustKapakSayac++;
                                }
                                else if (kod3 == "01")
                                {
                                    tabBaslik = $"Gövde-{govdeSayac}";
                                    govdeSayac++;
                                }

                                results.Add(new KutleDenkligiResult
                                {
                                    IsemriNo = isemriNo,
                                    Kod3 = kod3,
                                    GrupIsim = grupIsim ?? "",
                                    TabBaslik = tabBaslik
                                });
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Kütle Denkliği - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        [HttpPost("kutle-denkligi-uretim")]
        public async Task<IActionResult> GetKutleDenkligiUretim([FromBody] IsemriRequest request)
        {
            try
            {
                Console.WriteLine($"Kütle Denkliği üretim verileri isteği: {request?.IsemriNo}");

                if (request == null || string.IsNullOrEmpty(request.IsemriNo))
                {
                    return BadRequest(new { success = false, message = "IsemriNo gerekli" });
                }

                var results = await GetKutleDenkligiUretimData(request.IsemriNo);
                Console.WriteLine($"Kütle Denkliği üretim sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kütle Denkliği üretim verileri hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("kutle-denkligi-hammadde")]
        public async Task<IActionResult> GetKutleDenkligiHammadde([FromBody] IsemriRequest request)
        {
            try
            {
                Console.WriteLine($"Kütle Denkliği hammadde verileri isteği: {request?.IsemriNo}");

                if (request == null || string.IsNullOrEmpty(request.IsemriNo))
                {
                    return BadRequest(new { success = false, message = "IsemriNo gerekli" });
                }

                var results = await GetKutleDenkligiHammaddeData(request.IsemriNo);
                Console.WriteLine($"Kütle Denkliği hammadde sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kütle Denkliği hammadde verileri hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("search-montaj")]
        public async Task<IActionResult> SearchMontaj([FromBody] CertificationSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Montaj arama isteği: lot_no - {request?.SearchValue}");

                if (request == null || string.IsNullOrEmpty(request.SearchValue))
                {
                    return BadRequest(new { success = false, message = "SearchValue gerekli" });
                }

                var results = await GetMontajResults(request.SearchValue);
                Console.WriteLine($"Montaj sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Montaj arama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("montaj-uretim")]
        public async Task<IActionResult> GetMontajUretimData([FromBody] IsemriRequest request)
        {
            try
            {
                Console.WriteLine($"Montaj üretim verileri isteği: {request?.IsemriNo}");

                if (request == null || string.IsNullOrEmpty(request.IsemriNo))
                {
                    return BadRequest(new { success = false, message = "IsemriNo gerekli" });
                }

                var results = await GetMontajUretimData(request.IsemriNo);
                Console.WriteLine($"Montaj üretim sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Montaj üretim verileri hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("montaj-sarf")]
        public async Task<IActionResult> GetMontajSarfData([FromBody] IsemriRequest request)
        {
            try
            {
                Console.WriteLine($"Montaj sarf verileri isteği: {request?.IsemriNo}");

                if (request == null || string.IsNullOrEmpty(request.IsemriNo))
                {
                    return BadRequest(new { success = false, message = "IsemriNo gerekli" });
                }

                var results = await GetMontajSarfData(request.IsemriNo);
                Console.WriteLine($"Montaj sarf sonucu: {results.Count} kayıt bulundu");
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Montaj sarf verileri hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<List<KutleDenkligiUretimResult>> GetKutleDenkligiUretimData(string isemriNo)
        {
            var results = new List<KutleDenkligiUretimResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT ISEMRI_NO,STOK_KODU,CONVERT(NUMERIC(18,0),SUM(ADET)) AS ADET,CONVERT(NUMERIC(18,2),SUM(NET)) AS NET_KG,CONVERT(NUMERIC(18,2),SUM(BRUT)) AS BRUT_KG,COUNT(ID) AS KOLI_SAYISI,
                    CASE WHEN URET_TIP='0' THEN 'Üretim' WHEN URET_TIP='1' THEN 'Numune' WHEN URET_TIP='2' THEN 'Fire' WHEN URET_TIP='3' THEN 'Renk Geçişi' WHEN URET_TIP='5' THEN 'Yarım Koli' ELSE 'x' END AS URET_TIP
                    FROM _RP_URETIM_SERI
                    WHERE ISEMRI_NO = @IsemriNo
                    GROUP BY ISEMRI_NO,STOK_KODU,URET_TIP
                    ORDER BY ADET DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IsemriNo", isemriNo);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new KutleDenkligiUretimResult
                            {
                                IsemriNo = reader["ISEMRI_NO"]?.ToString() ?? "",
                                StokKodu = reader["STOK_KODU"]?.ToString() ?? "",
                                Adet = Convert.ToDecimal(reader["ADET"] ?? 0),
                                NetKg = Convert.ToDecimal(reader["NET_KG"] ?? 0),
                                BrutKg = Convert.ToDecimal(reader["BRUT_KG"] ?? 0),
                                KoliSayisi = Convert.ToInt32(reader["KOLI_SAYISI"] ?? 0),
                                UretTip = reader["URET_TIP"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            Console.WriteLine($"Kütle Denkliği Üretim - {isemriNo} için {results.Count} kayıt bulundu");
            return results;
        }

        private async Task<List<KutleDenkligiHammaddeResult>> GetKutleDenkligiHammaddeData(string isemriNo)
        {
            var results = new List<KutleDenkligiHammaddeResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        ISEMRI_NO,
                        T.VS_STOK_KODU,
                        DBO.TRK(ST.STOK_ADI) AS STOK_ADI,
                        ISNULL(SR.ACIK2,'-') AS HAMMADDE_LOT,
                        CONVERT(NUMERIC(18,2),SUM(HARCANAN)) AS HARCANAN
                    FROM _RP_URETIM_SERI_TAKIP T
                    LEFT OUTER JOIN _RP_URETIM_SERI S WITH (NOLOCK) ON S.ID=T.URET_ID
                    LEFT OUTER JOIN TBLSTSABIT ST WITH (NOLOCK) ON ST.STOK_KODU=T.VS_STOK_KODU
                    LEFT OUTER JOIN TBLSERITRA SR WITH (NOLOCK) ON SR.SERI_NO=T.VS_SERI_NO AND SR.DEPOKOD IN (200,300,302) AND SR.GCKOD='G'
                    WHERE S.ISEMRI_NO = @IsemriNo
                    GROUP BY ISEMRI_NO,T.VS_STOK_KODU,ST.STOK_ADI,ACIK2
                    ORDER BY T.VS_STOK_KODU ASC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IsemriNo", isemriNo);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new KutleDenkligiHammaddeResult
                            {
                                IsemriNo = reader["ISEMRI_NO"]?.ToString() ?? "",
                                VsStokKodu = reader["VS_STOK_KODU"]?.ToString() ?? "",
                                StokAdi = reader["STOK_ADI"]?.ToString() ?? "",
                                HammaddeLot = reader["HAMMADDE_LOT"]?.ToString() ?? "",
                                Harcanan = Convert.ToDecimal(reader["HARCANAN"] ?? 0)
                            });
                        }
                    }
                }
            }

            Console.WriteLine($"Kütle Denkliği Hammadde - {isemriNo} için {results.Count} kayıt bulundu");
            return results;
        }

        private async Task<List<MontajResult>> GetMontajResults(string lotNumbers)
        {
            var results = new List<MontajResult>();
            var lotNoList = lotNumbers.Split(',').Select(x => $"'{x.Trim()}'").ToList();
            var lotNoString = string.Join(",", lotNoList);

            Console.WriteLine($"Montaj - Lot No listesi: {lotNoString}");

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT ISEMRI_NO FROM _RP_URETIM_SERI
                    WHERE LOT_NO IN (" + lotNoString + @")
                    GROUP BY ISEMRI_NO";

                Console.WriteLine($"Montaj sorgusu: {query}");

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        int counter = 1;
                        while (await reader.ReadAsync())
                        {
                            results.Add(new MontajResult
                            {
                                IsemriNo = reader["ISEMRI_NO"]?.ToString() ?? "",
                                TabBaslik = $"Montaj-{counter}"
                            });
                            counter++;
                        }
                    }
                }
            }

            Console.WriteLine($"Montaj - Toplam {results.Count} kayıt bulundu");
            return results;
        }

        private async Task<List<MontajUretimResult>> GetMontajUretimData(string isemriNo)
        {
            var results = new List<MontajUretimResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT ISEMRI_NO,STOK_KODU,CONVERT(NUMERIC(18,0),(SUM(ADET))) AS ADET,
                    SUM(NET) AS NET,SUM(BRUT) AS BRUT,
                    CASE WHEN URET_TIP = 1 THEN 0 ELSE COUNT(ID) END AS KOLI,
                    CASE WHEN URET_TIP = 0 THEN N'Üretim'
                    WHEN URET_TIP = 1 THEN N'Numune'
                    WHEN URET_TIP = 2 THEN N'Fire'
                    WHEN URET_TIP = 3 THEN N'-'
                    WHEN URET_TIP = 4 THEN N'Renk Geçişi'
                    WHEN URET_TIP = 5 THEN N'Yarım Koli' END AS URETIM_TIPI
                    FROM _RP_URETIM_SERI
                    WHERE  ISEMRI_NO = @IsemriNo
                    GROUP BY ISEMRI_NO,STOK_KODU,URET_TIP
                    ORDER BY ISEMRI_NO,URET_TIP";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IsemriNo", isemriNo);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new MontajUretimResult
                            {
                                IsemriNo = reader["ISEMRI_NO"]?.ToString() ?? "",
                                StokKodu = reader["STOK_KODU"]?.ToString() ?? "",
                                Adet = Convert.ToInt32(reader["ADET"] ?? 0),
                                Net = Convert.ToDecimal(reader["NET"] ?? 0),
                                Brut = Convert.ToDecimal(reader["BRUT"] ?? 0),
                                Koli = Convert.ToInt32(reader["KOLI"] ?? 0),
                                UretimTipi = reader["URETIM_TIPI"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            Console.WriteLine($"Montaj Üretim - {isemriNo} için {results.Count} kayıt bulundu");
            return results;
        }

        private async Task<List<MontajSarfResult>> GetMontajSarfData(string isemriNo)
        {
            var results = new List<MontajSarfResult>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT S.STOK_KODU,A.STOK_ADI,
                    CONVERT(NUMERIC(18,0),SUM(HARCANAN)) AS HARCANAN
                    FROM _RP_URETIM_SERI_TAKIP_MONTAJ S
                    LEFT OUTER JOIN TBLSTSABIT A ON S.STOK_KODU = A.STOK_KODU
                    WHERE URET_ID IN
                    (SELECT ID FROM _RP_URETIM_SERI WHERE ISEMRI_NO = @IsemriNo) AND S.STOK_KODU LIKE '150%'
                    GROUP BY S.STOK_KODU,A.STOK_ADI,URUN_TIP
                    ORDER BY S.STOK_KODU,URUN_TIP";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IsemriNo", isemriNo);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new MontajSarfResult
                            {
                                StokKodu = reader["STOK_KODU"]?.ToString() ?? "",
                                StokAdi = reader["STOK_ADI"]?.ToString() ?? "",
                                Harcanan = Convert.ToInt32(reader["HARCANAN"] ?? 0)
                            });
                        }
                    }
                }
            }

            Console.WriteLine($"Montaj Sarf - {isemriNo} için {results.Count} kayıt bulundu");
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

    public class IsemriRequest
    {
        public string IsemriNo { get; set; }
    }

    public class CertificationResult
    {
        public string IsemriNo { get; set; }
        public string Kod3 { get; set; }
        public string GrupIsim { get; set; }
        public string AnalizeNumber { get; set; }
    }

    public class IlkNumuneResult
    {
        public string IsemriNo { get; set; }
        public string AnalizeNumber { get; set; }
    }

    public class IsEmriResult
    {
        public string IsemriNo { get; set; }
        public string StokKodu { get; set; }
        public string TIID { get; set; }
        public string Url { get; set; }
    }

    public class KutleDenkligiResult
    {
        public string IsemriNo { get; set; }
        public string Kod3 { get; set; }
        public string GrupIsim { get; set; }
        public string TabBaslik { get; set; }
    }

    public class KutleDenkligiUretimResult
    {
        public string IsemriNo { get; set; }
        public string StokKodu { get; set; }
        public decimal Adet { get; set; }
        public decimal NetKg { get; set; }
        public decimal BrutKg { get; set; }
        public int KoliSayisi { get; set; }
        public string UretTip { get; set; }
    }

    public class KutleDenkligiHammaddeResult
    {
        public string IsemriNo { get; set; }
        public string VsStokKodu { get; set; }
        public string StokAdi { get; set; }
        public string HammaddeLot { get; set; }
        public decimal Harcanan { get; set; }
    }

    public class MontajResult
    {
        public string IsemriNo { get; set; }
        public string TabBaslik { get; set; }
    }

    public class MontajUretimResult
    {
        public string IsemriNo { get; set; }
        public string StokKodu { get; set; }
        public int Adet { get; set; }
        public decimal Net { get; set; }
        public decimal Brut { get; set; }
        public int Koli { get; set; }
        public string UretimTipi { get; set; }
    }

    public class MontajSarfResult
    {
        public string StokKodu { get; set; }
        public string StokAdi { get; set; }
        public int Harcanan { get; set; }
    }
}

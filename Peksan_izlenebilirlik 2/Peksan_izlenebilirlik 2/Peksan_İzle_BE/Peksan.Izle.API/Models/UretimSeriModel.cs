namespace Peksan.Izle.API.Models
{
    public class UretimSeriModel
    {
        public string ISEMRI_NO { get; set; } = string.Empty;
        public string STOK_KODU { get; set; } = string.Empty;
        public decimal ADET { get; set; }
        public decimal NET_KG { get; set; }
        public decimal BRUT_KG { get; set; }
        public int KOLI_SAYISI { get; set; }
        public string URET_TIP { get; set; } = string.Empty;
    }

    public class UretimSeriRequest
    {
        public string SeriNo { get; set; } = string.Empty;
    }

    public class UretimSeriResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UretimSeriModel> Data { get; set; } = new List<UretimSeriModel>();
        public int Count { get; set; }
        public string SeriNo { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

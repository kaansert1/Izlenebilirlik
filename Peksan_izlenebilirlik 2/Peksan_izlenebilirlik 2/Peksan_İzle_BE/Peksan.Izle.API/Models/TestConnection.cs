using System.ComponentModel.DataAnnotations;

namespace Peksan.Izle.API.Models
{
    public class TestConnection
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

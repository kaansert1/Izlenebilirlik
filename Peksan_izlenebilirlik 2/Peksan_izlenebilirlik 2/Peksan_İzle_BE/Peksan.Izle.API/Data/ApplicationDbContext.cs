using Microsoft.EntityFrameworkCore;
using Peksan.Izle.API.Models;

namespace Peksan.Izle.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Test için basit bir tablo
        public DbSet<TestConnection> TestConnections { get; set; }

        // Burada diğer veritabanı tablolarınızı temsil eden DbSet'leri ekleyeceksiniz
        // Örnek:
        // public DbSet<Product> Products { get; set; }
        // public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Burada model konfigürasyonlarınızı yapabilirsiniz
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Services;

namespace ORMPostgreSQL
{
    public class SQLiteContext<T> : DbContext where T : class, IProduct<T>
    {
        public DbSet<T> products { get; set; } = null!;
        public string DBFileName { get; private set; } = "products";

        public SQLiteContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source = {DBFileName}.db");
        }
    }
}

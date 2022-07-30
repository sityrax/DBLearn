using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Services;
using System;

namespace ORMPostgreSQL
{
    public class PostgreContext<T> : DbContext where T : class, IProduct<T>
    {
        public DbSet<T> products { get; set; } = null!;

        #region Connection settings
        string Host { get; }
        string Port { get; }
        string DataBase { get; }
        string Username { get; }
        string Password { get; }
        #endregion

        ///<exception cref="Exception"></exception>
        public PostgreContext(string host = "localhost", 
                              string port = "5432", 
                              string dataBase = "master", 
                              string username = "postgres", 
                              string password = "postgres")
        {
            Host = host;
            Port = port;
            DataBase = dataBase;
            Username = username;
            Password = password;

            Database.EnsureCreated();   // не может создать таблицу в существующей базе данных, уже имеющей другие таблицы.
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql($"Host={Host};" +
                                     $"Port={Port};" +
                                     $"Database={DataBase};" +
                                     $"Username={Username};" +
                                     $"Password={Password}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}

#define LOGTEST

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Diagnostics;

namespace ORMPostgreSQL
{
    public class PostgreContext<T> : DbContext where T : class
    {
        public DbSet<T> products { get; set; } = null!;

        #region Logging
#if RELEASE
        static int _logStreamsCount;
        static StreamWriter _logStream;
#endif
        #endregion

        #region Connection settings
        string Host { get; }
        string Port { get; }
        string DataBase { get; }
        string Username { get; }
        string Password { get; }
        #endregion

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
#region Logging
#if RELEASE
            if (_logStreamsCount == 0)
                _logStream = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Errorlog.txt"), append: true);
            _logStreamsCount++;
                optionsBuilder.LogTo(_logStream.WriteLine)
                              .EnableSensitiveDataLogging();
#elif LOGTEST
            optionsBuilder.LogTo(x => Debug.WriteLine(x))
                          .EnableSensitiveDataLogging();
#endif
#endregion
        }

        public override void Dispose()
        {
            base.Dispose();
#region Logging
#if RELEASE
            _logStream?.WriteLine("Logstream is closed.\n\n");
            if (_logStreamsCount == 1)
            {
                _logStream?.Dispose();
                _logStreamsCount--;
            }
            else
                _logStreamsCount--;
#endif
#endregion
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
#region Logging
#if RELEASE
            if (_logStreamsCount == 1)
            {
                await _logStream.DisposeAsync();
                _logStreamsCount--;
            }
            else
                _logStreamsCount--;
#endif
#endregion
        }
    }
}

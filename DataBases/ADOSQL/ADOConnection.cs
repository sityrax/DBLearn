using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using System;

namespace ADO
{
    public class ADOConnection<T> : IDisposable, IAsyncDisposable where T : class, new()
    {
        #region Connection settings
        public string Host { get; }
        public string Database { get; }
        public bool Trusted_Connection { get; }
        public bool TrustServer_Certificate { get; }
        string connectionString { get => string.Concat(@$"Server={Host};
                                                          Database={Database};
                                                          Trusted_Connection={Trusted_Connection};
                                                          TrustServerCertificate={TrustServer_Certificate}"); }
        #endregion

        #region PoolInfo
        // В ADO.NET используется механизм пула подключений: После закрытия подключения с помощью метода Close() закрытое подключение возвращается в пул подключений, где оно оно готово к повторному использованию при следующем вызове метода Open(). В пул помещаются подключения только с одинаковой конфигурацией. Максимальный размер пула 100 подключений.
        #endregion

        public string CurrentTable { get; set; }
        public SqlConnection Connection { get; private set; }
        public SqlCommand Command { get; private set; }

        ReflectionDB<T> reflection = new();

        /// <param name="host">Name of connection.</param>
        /// <param name="database">Name of database.</param>
        /// <param name="trusted_Connection">Create connection with current windows accaunt.</param>
        /// <param name="minPoolSize">More than 0 to make a connection longer 4-8 min.</param>
        /// <param name="maxPoolSize">Maximum number of connections in pool.</param>
        /// <param name="pooling">To disable a pool of connections.</param>
        public ADOConnection(string host, string database, bool trusted_Connection, bool trustServer_Certificate = true)
        {
            Host                    = host;
            Database                = database;
            Trusted_Connection      = trusted_Connection;
            TrustServer_Certificate = trustServer_Certificate;
        }

        ///<summary>Connect to database.</summary>
        /// <exception cref="ArgumentException"></exception>
        public void Connect()
        {
            Connection = new SqlConnection(connectionString);   // создание подключения.
            Command = Connection.CreateCommand();   // команда сразу привязана к подключению.
            Connection.Open();  // открываем подключение.
        }

        public async void ConnectAsync()
        {
            // то же только в профиль.
            Connection = new SqlConnection(connectionString);
            Command = Connection.CreateCommand();
            await Connection.OpenAsync();
        }

        /// <exception cref="ArgumentNullException">Throws if connection instance is missing.</exception>
        public void CreateTable(string tableName, bool recreate = false)
        {
            if (Connection is null)
                throw new ArgumentNullException("Connection does not exist.");
            CurrentTable ??= tableName.TrimStart().Split(' ')[0];

            Command.Transaction = Connection.BeginTransaction();
            try
            {
                if (recreate)
                {
                    Command.CommandText = 
                     $@"IF OBJECT_ID(N'dbo.{CurrentTable}', N'U') IS NOT NULL
                                DROP TABLE {CurrentTable};";
                    Command.ExecuteNonQuery();
                    Command.CommandText = GetCreateTableRequire();
                }
                else
                {
                    string createTableRequire = $"IF OBJECT_ID(N'dbo.{CurrentTable}', N'U') IS NULL " + GetCreateTableRequire();
                    Command.CommandText = createTableRequire;
                }
            Command.ExecuteNonQuery();
            Command.Transaction.Commit();
            }
            catch (Exception)
            {
                Command.Transaction.Rollback();
                throw;
            }
        }

        public void CreateProcedure()
        {
            try
            {
                Command.Transaction = Connection.BeginTransaction();
                Command.CommandText =
                $@"IF OBJECT_ID('sp_Insert{CurrentTable}', 'P') IS NOT NULL
                  DROP PROCEDURE sp_Insert{CurrentTable}";
                Command.ExecuteNonQuery();

                Command.CommandText = GetCreateProcedureRequire();

                Command.ExecuteNonQuery();
                Command.Transaction.Commit();

                Command.Transaction = Connection.BeginTransaction();
                Command.CommandText =
                    $@"IF OBJECT_ID('sp_Get{CurrentTable}', 'P') IS NOT NULL
                   DROP PROCEDURE sp_Get{CurrentTable}";
                Command.ExecuteNonQuery();

                Command.CommandText =
                $@"CREATE PROCEDURE [dbo].[sp_Get{CurrentTable}]
                   AS
                     SELECT * FROM { CurrentTable } 
                   GO";
                Command.ExecuteNonQuery();
                Command.Transaction.Commit();
            }
            catch(Exception)
            {
                Command.Transaction.Rollback();
                throw;
            }
        }

        private string GetCreateProcedureRequire()
        {
            StringBuilder stringBuilder = new StringBuilder($"CREATE PROCEDURE [dbo].[sp_Insert{CurrentTable}]");

            PropertyInfoDB[] properties = reflection.PropertiesInfoDB;

            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(", ");
                string columnName = properties[i].dbName;
                string columnType = properties[i].dbType;
                stringBuilder.Append($"@{columnName} {columnType} ");
            }
            stringBuilder.Append(@$" AS BEGIN SET IDENTITY_INSERT { CurrentTable } ON ");
            stringBuilder.Append(@$"INSERT INTO {CurrentTable} (");
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(", ");
                string columnName = properties[i].dbName;
                stringBuilder.Append($"{columnName}");
            }
            stringBuilder.Append($") VALUES (");
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(", ");
                string columnName = properties[i].dbName;
                stringBuilder.Append($"@{columnName}");
            }
            stringBuilder.Append($") ");
            stringBuilder.Append($"SELECT SCOPE_IDENTITY() ");
            stringBuilder.Append($"SET IDENTITY_INSERT {CurrentTable} OFF ");
            stringBuilder.Append($"END; ");

            return stringBuilder.ToString();
        }

        private string GetCreateTableRequire()
        {
            PropertyInfoDB[] properties = reflection.PropertiesInfoDB;

            StringBuilder stringBuilder = new StringBuilder($"CREATE TABLE {CurrentTable} (");
            string columnIdentity = null;

            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(", ");
                string columnName     = properties[i].dbName;
                string columnType     = properties[i].dbType;
                string columnRequired = properties[i].Required   ? "NOT NULL" : "NULL";
                       columnIdentity = properties[i].PrimaryKey ? "IDENTITY" : string.Empty;
                stringBuilder.Append($"{columnName} {columnType} {columnRequired} {columnIdentity}");
            }
            if (reflection.PrimaryKey is not null)
            {
                string columnName = reflection.PrimaryKey.dbName;
                stringBuilder.Append($"CONSTRAINT \"PK_{CurrentTable}_{columnName}\" PRIMARY KEY({columnName})");
            }
            stringBuilder.Append($");");
            return stringBuilder.ToString();
        }

        public async ValueTask DisposeAsync()
        {
            await Command.DisposeAsync();
            await Connection.DisposeAsync();
        }

        public void Dispose()
        {
            Command.Dispose();
            Connection.Dispose();
        }
    }
}

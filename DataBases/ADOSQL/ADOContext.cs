﻿using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Data;
using Services;
using System;

namespace ADOSQL
{
    public class ADOContext<T> :IDisposable, IAsyncDisposable where T : class, IProduct<T>, new()
    {
        string sqlCommandToLoad;
        public DataSet DataSet { get; private set; }    // особенность класса в использовании DataSet

        private SqlDataAdapter Adapter;
        public SqlCommand Command { get; private set; }
        public SqlConnection Сonnection { get; private set; }

        #region Connection settings
        public string Host { get; }
        public string Database { get; }
        public bool Trusted_Connection { get; }
        public bool TrustServer_Certificate { get => true; }

        public string CurrentTable { get; set; }
        string connectionString { get => @$"Server={Host};
                                            Database={Database};
                                            Trusted_Connection={Trusted_Connection};
                                            TrustServerCertificate={TrustServer_Certificate}"; }
        #endregion

        /// <param name="host">Name of connection.</param>
        /// <param name="database">Name of database.</param>
        /// <param name="trusted_Connection">Create connection with current windows accaunt.</param>
        /// <param name="minPoolSize">More than 0 to make a connection longer 4-8 min.</param>
        /// <param name="maxPoolSize">Maximum number of connections in pool.</param>
        /// <param name="pooling">To disable a pool of connections.</param>
        public ADOContext(string host, string database, bool trusted_Connection/*, int minPoolSize = 0, int maxPoolSize = 0, bool pooling = true*/)
        {
            Host = host;
            Database = database;
            Trusted_Connection = trusted_Connection;
        }

        /// <param name="host">Name of connection.</param>
        /// <param name="database">Name of database.</param>
        /// <param name="trusted_Connection">Create connection with current windows accaunt.</param>
        /// <param name="minPoolSize">More than 0 to make a connection longer 4-8 min.</param>
        /// <param name="maxPoolSize">Maximum number of connections in pool.</param>
        /// <param name="pooling">To disable a pool of connections.</param>
        public ADOContext(string sqlCommandToLoad, string host, string database, bool trusted_Connection)
        {
            Host = host;
            Database = database;
            Trusted_Connection = trusted_Connection;

            this.sqlCommandToLoad = sqlCommandToLoad;
        }

        ///<summary>Connect to database.</summary>
        /// <exception cref="ArgumentException"></exception>
        public void Connect(string connectionTable = "products")
        {
            CurrentTable ??= connectionTable.TrimStart().Split(' ')[0]; // отрезаем все лишнее.
            ReflectionDB<T> reflection = new();

            #region Creation instances
            Сonnection = new SqlConnection(connectionString);
            Сonnection.Open();
            Command = Сonnection.CreateCommand();
            DataSet = new DataSet();
            CreateTable();
            #endregion

            sqlCommandToLoad ??= $"SELECT * FROM {CurrentTable}";   // создаем запрос для получения всех записей из таблицы.
            Adapter = new SqlDataAdapter(sqlCommandToLoad, Сonnection);
            Adapter.Fill(DataSet, CurrentTable);    // заполняем коллекцию записями, полученными в результате запроса.
            
            DataSet.Tables[CurrentTable].PrimaryKey = new DataColumn[] // Помечаем первичный ключ.
            { 
                DataSet.Tables[CurrentTable].Columns[reflection.PropertуNames[nameof(reflection.instance.Id)]] 
            };
        }

        private void CreateTable(bool recreate = false)
        {
            if (Сonnection is null)
                throw new ArgumentNullException("Connection does not exist.");
            Command.Transaction = Сonnection.BeginTransaction();    // открываем блок транзакции.
            try
            {
                if (recreate)
                {
                    Command.CommandText = 
                     $@"IF OBJECT_ID(N'dbo.{CurrentTable}', N'U') IS NOT NULL   
                DROP TABLE {CurrentTable};";  // наши прихоти.
                    Command.ExecuteNonQuery();  // добавляем запрос в транзакцию.

                    Command.CommandText = GetCreateTableRequire();
                }
                else
                {
                    Command.CommandText = $@"IF OBJECT_ID(N'dbo.{CurrentTable}', N'U') IS NULL " + GetCreateTableRequire();
                }
                Command.ExecuteNonQuery();  // добавляем запрос в транзакцию.
                Command.Transaction.Commit();   // коммитим транзакцию на сервер.
            }
            catch (Exception)
            {
                Command.Transaction.Rollback(); // откатываем транзакцию, если все пошло не по плану.
                throw;
            }
        }

        /// <summary>Can return many different exceptions or update table in the database.</summary>
        /// <exception cref="SystemException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DBConcurrencyException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Update()
        {
            SqlCommandBuilder builder = new(Adapter);
            Adapter.Update(DataSet, CurrentTable);
        }

        private string GetCreateTableRequire()
        {
            ReflectionDB<T> reflection = new();
            StringBuilder stringBuilder = new StringBuilder($"CREATE TABLE {CurrentTable} (");;

            PropertyInfo[] properties = reflection.Properties;
            string columnIdentity = null;

            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(", ");
                string columnName     = reflection.PropertуNames[properties[i].Name];
                string columnType     = reflection.PropertуTypes[properties[i]];
                string columnRequired = reflection.PropertуRequired[properties[i]];
                       columnIdentity = reflection.PrimaryKey.Name == columnName ? "IDENTITY" : string.Empty;
                stringBuilder.Append($"{columnName} {columnType} {columnRequired} {columnIdentity}");
            }
            if (columnIdentity is not null)
            {
                string columnName = reflection.PropertуNames[reflection.PrimaryKey.Name];
                stringBuilder.Append($"CONSTRAINT \"PK_{CurrentTable}_{columnName}\" PRIMARY KEY({columnName})");
            }
            stringBuilder.Append($");");
            return stringBuilder.ToString();
        }

        public async ValueTask DisposeAsync()
        {
            await Command.DisposeAsync();
            await Сonnection.DisposeAsync();
        }

        public void Dispose()
        {
            Command.Dispose();
            Сonnection.Dispose();
        }
    }
}

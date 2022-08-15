//#define VARIANT
#define PROCEDURE_TEST

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Linq;
using System.Text;
using Services;
using System;

namespace ADO
{
    // Мы тут тестируем рефлексию и API для подключения к MSSQL с помощью ADO.NET, всех впечатлительных просьба закрыть глаза.
    public class ADOProductRepository<T> : IProductRepository<T> where T : class, IProduct<T>, new()
    {
        #region Connection settings
        public string Host { get; }
        public string Database { get; }
        public bool Trusted_Connection { get; }
        #endregion

        object key = new();

        ReflectionDB<T> reflection = new();

        public ADOProductRepository(string host, string database, bool trusted_Connection)
        {
            Host               = host;
            Database           = database;
            Trusted_Connection = trusted_Connection;
        }

        public int CheckAvailableCount()
        {
            using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
            {
                connection.Connect();
                connection.CreateTable(reflection.TableName);
                connection.Command.CommandText = $@"SELECT COUNT(*) FROM {connection.CurrentTable}";
                int toReturn = (int)connection.Command.ExecuteScalar();
                return toReturn;
            }
        }

        /// <exception cref="SqlException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public List<T> Get(string name)
        {
            lock (key)
            {
                using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
                {
                    List<T> toReturn = null;

                    connection.Connect();
                    connection.CreateTable(reflection.TableName);

                    // создаем параметр для имени
                    SqlParameter teamParam = new()
                    {
                        ParameterName = "@" + reflection.PropertуNames[nameof(reflection.instance.ProductName)],
                        Value = name,
                        Direction  =  System.Data.ParameterDirection.Input,
                        SqlDbType  =  reflection.PropertiesRelations[nameof(reflection.instance.ProductName)].sqlDbType,
                        Size       =  reflection.PropertiesRelations[nameof(reflection.instance.ProductName)].Size,
                        IsNullable = !reflection.PropertiesRelations[nameof(reflection.instance.ProductName)].Required
                    };

                    // добавляем параметр к команде
                    connection.Command.Parameters.Add(teamParam);
                    connection.Command.CommandText = $@"SELECT *
                                                        FROM {connection.CurrentTable} 
                                                        WHERE [{reflection.PropertуNames[nameof(reflection.instance.ProductName)]}] = {teamParam.ParameterName}";
                    using (SqlDataReader reader = connection.Command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            toReturn ??= new();
                            while (reader.Read())
                            {
                                T instance = reflection.ReflectionRead(reader);
                                toReturn.Add(instance);
                            }
                        }
                    }
                    return toReturn;
                }
            }
        }

        /// <returns>Products corresponding to the id or null if not found.</returns>
        /// <exception cref="SqlException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public T Get(int id)
        {
            using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
            {
                connection.Connect();
                connection.CreateTable(reflection.TableName);

                connection.Command.CommandText = @$"SELECT * 
                                                    FROM {connection.CurrentTable}
                                                    WHERE [{reflection.PropertуNames[nameof(reflection.instance.Id)]}] = {id}";
                SqlDataReader reader = connection.Command.ExecuteReader();

                T toReturn = null;
                if (reader.HasRows)
                {
                    reader.Read();
                    toReturn = reflection.ReflectionRead(reader);
                }
                reader.Close();
                return toReturn;
            }
        }

        /// <exception cref="SqlException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public List<T> GetAll(Func<T, bool> predicate = null)
        {
            using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
            {
                List<T> toReturn = new();

                connection.Connect();
                connection.CreateTable(reflection.TableName);

                connection.Command.CommandText = $"SELECT * FROM {connection.CurrentTable}";

                using (SqlDataReader reader = connection.Command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            T item = reflection.ReflectionRead(reader);
                            if (predicate is not null)
                            {
                                if(predicate(item))
                                toReturn.Add(item);
                            }
                            else
                                toReturn.Add(item);
                        }
                    }
                }
                return toReturn;
            }
        }

        /// <exception cref="SqlException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public List<T> Save(bool persistentEntry = false, params T[] products)
        {
            lock (key)
            {
                using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
                {
                    List<T> toReturn = new();
                    List<T> toWrite  = new();

                    connection.Connect();
                    connection.CreateTable(reflection.TableName);
                    connection.CreateProcedure();

                    foreach (var product in products)
                    {
                        if(product.Id is not null)
                            connection.Command.CommandText = $@"SELECT COUNT(*) 
                                                                FROM {connection.CurrentTable} 
                                                                WHERE [{reflection.PropertуNames[nameof(reflection.instance.Id)]}] = {product.Id}";
                        else
                        {
                            string date = $"{product.ManufactureDate.Year}-{product.ManufactureDate.Month:00}-{product.ManufactureDate.Day:00}" +
                                          $"T{product.ManufactureDate.Hour:00}:{product.ManufactureDate.Minute:00}:{product.ManufactureDate.Second:00}";
                            // запрещаем компиляцию при некорректном изменении свойств интерфейса:
                            string manufactureDate = reflection.PropertуNames[nameof(reflection.instance.ManufactureDate)];
                            string productName     = reflection.PropertуNames[nameof(reflection.instance.ProductName)];
                            connection.Command.CommandText = $@"SELECT COUNT(*) 
                                                                FROM {connection.CurrentTable} 
                                                                WHERE [{manufactureDate}] = '{date}' and 
                                                                      [{productName}] = '{product.ProductName}'";
                        }
                        int resultCount = int.Parse(connection.Command.ExecuteScalar().ToString());
                        if (resultCount > 0)
                        {
                            if (persistentEntry is false)
                                throw new ArgumentException($"One or more entities with the same key value for {reflection.PropertуNames[nameof(reflection.instance.Id)]} are already being tracked or are in the databases");
                            else
                                toReturn.Add(product);
                        }
                        else
                            toWrite.Add(product);
                    }

                    if (toWrite.Count > 0)
                    {
                        connection.Command.CommandText = @$"SET IDENTITY_INSERT {connection.CurrentTable} ON";
                        connection.Command.ExecuteNonQuery();

                        for (int i = 0; i < toWrite.Count; i++)
                        {
                            // Этот блок тестирует параметры.
#if PROCEDURE_TEST
                            //Параметры с процедурой.
                            // название процедуры
                            connection.Command.CommandText = $"sp_Insert{connection.CurrentTable}";
                            // указываем, что команда представляет хранимую процедуру
                            connection.Command.CommandType = System.Data.CommandType.StoredProcedure;
#else
                            // Параметры без процедуры.
                            StringBuilder stringBuilder = new StringBuilder($"INSERT INTO {connection.CurrentTable} (");

                            #region Column Headers
                            for (int j = 0; j < reflection.PropertiesInfoDB.Length; j++)
                            {
                                if (j > 0)
                                    stringBuilder.Append(", ");
                                stringBuilder.Append($"{reflection.PropertiesInfoDB[j].dbName}");
                            }
                            #endregion

                            stringBuilder.Append(") VALUES ");
                            stringBuilder.Append("(");

                            #region Tuple
                            for (int j = 0; j < reflection.PropertiesInfoDB.Length; j++)
                            {
                                if (j > 0)
                                    stringBuilder.Append(", ");
                                stringBuilder.Append($"@{reflection.PropertiesInfoDB[j].dbName}");
                            }
                            #endregion

                            stringBuilder.Append(")");
                            connection.Command.CommandText = stringBuilder.ToString();
#endif
                            #region Parameters addition
                            PropertyInfoDB[] propertyValues = reflection.LoadValues(toWrite[i]);
                            if(reflection.PrimaryKey.value == DBNull.Value)
                               reflection.PrimaryKey.value = GetLastId(connection) + 1 + i;
                            foreach (var property in propertyValues)
                            {
                                SqlParameter param = new("@" + property.dbName, property.value)
                                {
                                    Direction = System.Data.ParameterDirection.Input,
                                    SqlDbType = property.sqlDbType
                                };
                                connection.Command.Parameters.Add(param);
                            }
                            #endregion

                            try
                            {
                                connection.Command.ExecuteNonQuery();
                                connection.Command.Parameters.Clear();
                            }
                            catch (InvalidOperationException)
                            {
                                if (persistentEntry is false)
                                    throw new ArgumentException($"One or more entities with the same key value for {reflection.PropertуNames[nameof(reflection.instance.Id)]} are already being tracked or are in the databases");
                            }
                        }
                        connection.Command.CommandText = @$"SET IDENTITY_INSERT {connection.CurrentTable} OFF";
                        connection.Command.CommandType = System.Data.CommandType.Text;
                        connection.Command.ExecuteNonQuery();
                    }
                    return toReturn;
                }
            }
        }

        /// <returns>Not remote id.</returns>
        /// <exception cref="SqlException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public List<int> Delete(bool persistentEntry = false, params int[] idCollection) //TODO: сделать async
        {
            using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
            {
                List<int> NullId = new();

                connection.Connect();
                connection.CreateTable(reflection.TableName);

                foreach (var item in idCollection)
                {
                    T toCheckOnNull;
                    toCheckOnNull = Get(item);
                    if (toCheckOnNull is null)
                    {
                        if (persistentEntry)
                            NullId.Add(item);
                        else
                            throw new ArgumentException("Id is absent in database");
                    }
                }
#if VARIANT
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($@"DELETE FROM {connection.CurrentTable} 
                                               WHERE [{reflection.PropertуNames[nameof(reflection.instance.Id)]}] = {idCollection[0]}");
                foreach (var item in idCollection)
                {
                    stringBuilder.Append($" or [{reflection.PropertуNames[nameof(reflection.instance.Id)]}] = {item}");
                }

                connection.Command.CommandText = stringBuilder.ToString();
                connection.Command.ExecuteNonQuery();
#else
                try
                {
                    connection.Command.Transaction = connection.Connection.BeginTransaction();
                    // выполняем две отдельные команды
                    foreach (var item in idCollection)
                    {
                    connection.Command.CommandText = $@"DELETE FROM {connection.CurrentTable} 
                                                               WHERE [{reflection.PropertуNames[nameof(reflection.instance.Id)]}] = {item}";
                    connection.Command.ExecuteNonQuery();
                    }
                    // подтверждаем транзакцию
                    connection.Command.Transaction.Commit();
                }
                catch (Exception)
                {
                    // если ошибка, откатываем назад все изменения
                    connection.Command.Transaction.Rollback();  //TODO: async?
                }
#endif
                return NullId;
            }
        }

        public int DeleteAll()
        {
            using (ADOConnection<T> connection = new(Host, Database, Trusted_Connection))
            {
                connection.Connect();
                connection.CreateTable(reflection.TableName);

                // название таблицы нельзя передать в качестве параметра, что делает код уязвимым для SQL - инъекции. Обезопасим себя внутри CreateTable.
                connection.Command.CommandText = $@"DELETE FROM {connection.CurrentTable}";
                return connection.Command.ExecuteNonQuery();
            }
        }

        /// <exception cref="OverflowException"></exception>
        public int GetLastId(ADOConnection<T> connection = null)
        {
            int lastId;
            if (connection is null)
            {
                ADOConnection<T> localConnection = new(Host, Database, Trusted_Connection);
                localConnection.Connect();
                localConnection.CreateTable(reflection.TableName);
                localConnection.Command.CommandText = $@"SELECT MAX({reflection.PropertуNames[nameof(reflection.instance.Id)]}) 
                                                         FROM {localConnection.CurrentTable}";
                lastId = int.Parse(localConnection.Command.ExecuteScalar().ToString());
                localConnection.Dispose();
            }
            else
            {
                SqlCommand sqlCommand = connection.Connection.CreateCommand();
                sqlCommand.CommandText = $@"SELECT MAX({reflection.PropertуNames[nameof(reflection.instance.Id)]}) 
                                            FROM {connection.CurrentTable}";
                lastId = int.Parse(sqlCommand.ExecuteScalar().ToString());
            }
            return lastId;
        }
    }
}

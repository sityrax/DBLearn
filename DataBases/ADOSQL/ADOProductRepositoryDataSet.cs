using System.Collections.Generic;
using System.Data;
using Services;
using System;

namespace ADOSQL
{
    public class ADOProductRepositoryDataSet<T> : IProductRepository<T> where T : class, IProduct<T>, new()
    {
        #region Connection settings
        public string Host { get; }
        public string Database { get; }
        public bool   Trusted_Connection { get; }
        #endregion

        ReflectionDB<T> reflection = new();

        object key = new();

        public ADOProductRepositoryDataSet(string host, string database, bool trusted_Connection)
        {
            Host               = host;
            Database           = database;
            Trusted_Connection = trusted_Connection;
        }

        public int CheckAvailableCount()
        {
            using(ADOContext<T> db = new(Host, Database, Trusted_Connection))
            {
                db.Connect(reflection.TableName);
                return db.DataSet.Tables[db.CurrentTable].Rows.Count;
            }
        }

        /// <returns>Products collection corresponding to the team or null if not found.</returns>
        /// <exception cref="ArgumentException"></exception>
        public List<T> Get(string productName)
        {
            lock (key)
                using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
                {
                    List<T> toReturn = null;

                    db.Connect(reflection.TableName);

                    DataTable dataTable = db.DataSet.Tables[db.CurrentTable];
                    string propertyName = reflection.PropertуNames[nameof(reflection.instance.ProductName)];

                    // перебор всех строк таблицы
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string nameRow = (string)row[propertyName];
                        if (nameRow == productName)
                        {
                            toReturn ??= new();
                            toReturn.Add(reflection.ReflectionRead(row));
                        }
                    }
                    return toReturn;
                }
        }

        /// <returns>Products corresponding to the id or null if not found.</returns>
        /// <exception cref="ArgumentException"></exception>
        public T Get(int id)
        {
            lock (key)
                using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
                {
                    db.Connect(reflection.TableName);

                    DataTable dataTable = db.DataSet.Tables[db.CurrentTable];
                    string propertyName = reflection.PropertуNames[nameof(reflection.instance.Id)];

                    // перебор всех строк таблицы
                    foreach (DataRow row in dataTable.Rows)
                    {
                        int currentId = (int)row[propertyName];
                        if (currentId == id)
                            return reflection.ReflectionRead(row);
                    }
                    return null;
                }
        }

        public List<T> GetAll(Func<T, bool> predicate = null)
        {
            lock (key)
                using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
                {
                    List<T> toReturn = null;

                    db.Connect(reflection.TableName);

                    DataTable dataTable = db.DataSet.Tables[db.CurrentTable];

                    // перебор всех строк таблицы
                    foreach (DataRow row in dataTable.Rows)
                    {
                        T product = reflection.ReflectionRead(row);

                        if (predicate is not null)
                        {
                            if (predicate(product))
                            {
                                toReturn ??= new();
                                toReturn.Add(product);
                            }
                        }
                        else
                        {
                            toReturn ??= new();
                            toReturn.Add(product);
                        }
                    }
                    return toReturn;
                }
        }

        public List<T> Save(bool persistentEntry = false, params T[] entities)
        {
            lock (key)
                using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
                {
                    List<T> toReturn = null;
                    List<T> toWrite  = null;

                    db.Connect(reflection.TableName);
                    db.Refresh();

                    var table = db.DataSet.Tables[db.CurrentTable];

                    foreach (var entity in entities)
                    {
                        if (entity.Id is not null)
                        {
                            DataRow rowWithId = table.Rows.Find(entity.Id);
                            if (rowWithId is not null)
                            {
                                if (!persistentEntry)
                                    throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                                toReturn ??= new();
                                toReturn.Add(entity); 
                                continue;
                            }
                        }
                        foreach (DataRow row in table.Rows)
                        {
                            if (reflection.Equals(entity, row))
                            {
                                if (!persistentEntry)
                                    throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                                    toReturn ??= new();
                                    toReturn.Add(entity);
                                    goto NextLap;   // TODO: покаяться за пастафарианство.
                            }
                        }
                        toWrite ??= new();
                        toWrite.Add(entity);
                        NextLap:;
                    }
                    if (toWrite != null)
                    {
                        int newId = 1;
                        foreach (DataRow item in db.DataSet.Tables[db.CurrentTable].Rows) // TODO: поискать альтернативу через прямой запрос к базе.
                        {
                            int currentId = (int)item[reflection.PrimaryKey.dbName];
                            if (currentId > newId)
                                newId = currentId + 1;
                        }
                        for (int i = 0; i < toWrite.Count; i++)
                        {
                            DataRow newRow = reflection.ReflectionWrite(source: toWrite[i],
                                                                        destination: table.NewRow());
                            if (newRow[reflection.PrimaryKey.dbName] == DBNull.Value)
                                newRow[reflection.PrimaryKey.dbName] = newId + i;
                            table.Rows.Add(newRow);
                        }
                        db.Update();
                        db.Refresh();
                    }
                    return toReturn;
                }
        }

        /// <param name="persistentEntry">Ignore the absence of deleted entities in the database.</param>
        /// <param name="id">Id of entities to delete.</param>
        /// <returns>Id collection of not deleted entities.</returns>
        /// <exception cref="ArgumentException"></exception>
        public List<int> Delete(bool persistentEntry = false, params int[] id)
        {
            lock(key)
            using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
            {
                List<int> toReturn = null;
                List<int> toDelete = null;

                db.Connect(reflection.TableName);

                DataTable dataTable = db.DataSet.Tables[db.CurrentTable];

                foreach (var entity in id)
                {
                    DataRow rowWithId = dataTable.Rows.Find(entity);
                    if (rowWithId is null)
                    {
                        if (!persistentEntry)
                            throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked as deleted or are absent in the databases");
                        toReturn ??= new();
                        toReturn.Add(entity);
                    }
                    else
                    {
                        toDelete ??= new();
                        toDelete.Add(entity);
                    }
                }
                if (toDelete is not null)
                {
                    foreach (var removableId in toDelete)
                    {
                        dataTable.Rows.Find(removableId).Delete(); // отмечаем строку как удаленную.
                    }
                    db.Update();    // применяем изменения.
                }
                return toReturn;
            }
        }

        /// <summary>
        /// Delete all entities in the database.
        /// </summary>
        /// <returns></returns>
        public int DeleteAll()
        {
            using (ADOContext<T> db = new(Host, Database, Trusted_Connection))
            {
                db.Connect(reflection.TableName);
                var rows = db.DataSet.Tables[db.CurrentTable].Rows;
                int rowsCount = rows.Count;
                foreach (DataRow row in rows)
                {
                    row.Delete();   // помечаем каждую запись для удаления.
                }
                db.Update();
                return rowsCount;
            }
        }
    }
}

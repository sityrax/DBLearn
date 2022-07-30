#define LAZY_LOADING
#define NOT_SO_LAZY

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Services;
using System;

namespace ORMPostgreSQL
{
    public class PostgreProductRepository<T> : IProductRepository<T> where T : class, IProduct<T>
    {
        #region Connection settings
        string Host { get; }
        string Port { get; }
        string DataBase { get; }
        string Username { get; }
        string Password { get; }
        #endregion

        readonly object key = new();

        public PostgreProductRepository(string host, string port, string dataBase, string username, string password)
        {
            Host = host;
            Port = port;
            DataBase = dataBase;
            Username = username;
            Password = password;
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>
        public List<T> Save(bool persistentEntry = false, params T[] products)
        {
            lock (key)
            {
                using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
                {
                    List<T> forecastsNotNull = new List<T>();
                    List<T> toSave = new List<T>();
                    T element;

                    foreach (var product in products)
                    {
                        element = db.products.Find(product.Id);
                        if (element is null)
                            toSave.Add(product);
                        else
                        {
                            if (persistentEntry is false)
                                throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                            forecastsNotNull.Add(product);
                        }
                    }
                    try
                    {
                        db.products.AddRange(toSave);
                        db.SaveChanges();
                    }
                    catch (InvalidOperationException)
                    {
                        if (persistentEntry is false)
                            throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                    }
                    return forecastsNotNull;
                }
            }
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public List<T> Get(string name)
        {
            using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
            {
#if LAZY_LOADING
                // Ленивая загрузка: отправка запроса SQL с фильтром where из метода на сервер.
                return db.products.AsQueryable().Where(x => x.ProductName.ToLower().Contains(name.ToLower()))
                                                .ToList();
#elif NOT_SO_LAZY
                // Чуть менее ленивая загрузка: отправка запроса на полную выборку и последующая фильтрация методом where, запрашивая каждую запись из выборки отдельно.
                return db.products.AsEnumerable().Where(x => x.ProductName.ToLower().Contains(name.ToLower()))
                                                 .ToList();
#else
                // Загрузка трудоголика? запрашивает все записи и загружает все разом, а уже потом фильтрует.
                return db.products.ToList().Where(x => x.ProductName.ToLower().Contains(name.ToLower())).ToList();
#endif
            }
        }

        /// <returns>The entity or null.</returns>
        public T Get(int id)
        {
            using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
            {
                return db.products.Find(id);
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        public List<T> GetAll(Func<T, bool> predicate = null)
        {
            using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
            {
                if(predicate is not null)
                {
#if LAZY_LOADING
                    return db.products.AsQueryable().Where(predicate).ToList();
#elif NOT_SO_LAZY
                    return db.products.AsEnumerable().Where(predicate).ToList();
#else
                    return db.products.ToList().Where(predicate).ToList();
#endif
                }
                return db.products.ToList();
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="OverflowException"></exception>
        public int CheckAvailableCount()
        {
            using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
                return db.products.Count();
        }

        /// <returns>Not remote id.</returns>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>        
        /// <exception cref="ArgumentException"></exception>
        public List<int> Delete(bool persistentEntry = false, params int[] id)
        {
            lock (key)
            {
                using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
                {
                    List<int> idNotNull = new List<int>();
                    List<T> toRemove = new List<T>();
                    T element;
                    foreach (var item in id)
                    {
                        element = db.products.Find(item);
                        if (element is not null)
                            toRemove.Add(element);
                        else
                        {
                            if (persistentEntry is false)
                                throw new ArgumentException("ID is absent in database");
                            idNotNull.Add(item);
                        }
                    }
                    foreach (var item in toRemove)
                    {
                        db.products.Remove(item);
                    }
                    db.SaveChanges();
                    return idNotNull;
                }
            }
        }

        /// <returns> Number of items removed.</returns>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>      
        public int DeleteAll()
        {
            lock (key)
            {
                using (PostgreContext<T> db = new(Host, Port, DataBase, Username, Password))
                {
                    IEnumerable<T> toRemove = GetAll();
                    db.products.RemoveRange(toRemove);
                    return db.SaveChanges();
                }
            }
        }
    }
}

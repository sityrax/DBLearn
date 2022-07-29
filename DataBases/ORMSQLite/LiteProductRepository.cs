using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Services;
using System;

namespace ORMPostgreSQL
{
    public class LiteProductRepository<T> : IProductRepository<T> where T : class, IProduct<T>
    {
        object obj = new();

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>
        public List<T> Save(bool persistentEntry = false, params T[] users)
        {
            lock (obj)
            {
                using (SQLiteContext<T> db = new())
                {
                    List<T> productsNotNull = new();
                    List<T> toSave = new();
                    T element;

                    foreach (var user in users)
                    {
                        element = db.users.Find(user.Id);
                        if (element is null)
                            toSave.Add(user);
                        else
                        {
                            if (persistentEntry is false)
                                throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                            productsNotNull.Add(user);
                        }
                    }
                    try
                    {
                        db.users.AddRange(toSave);
                        db.SaveChanges();
                    }
                    catch (InvalidOperationException)
                    {
                        if (persistentEntry is false)
                            throw new ArgumentException("One or more entities with the same key value for {'Id'} are already being tracked or are in the databases");
                    }
                    return productsNotNull;
                }
            }
        }

        /// <returns>The entity or null.</returns>
        public T Get(int id)
        {
            using (SQLiteContext<T> db = new())
            {
                return db.users.Find(id);
            }
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The entity or null.</returns>
        public List<T> Get(string name)
        {
            using (SQLiteContext<T> db = new())
            {
                return db.users.AsQueryable().Where(x => x.ProductName.ToLower().Contains(name.ToLower()))
                                             .ToList();
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        public List<T> GetAll(Func<T, bool> predicate = null)
        {
            using (SQLiteContext<T> db = new())
            {
                if (predicate is not null)
                    return db.users.AsEnumerable().Where(predicate).ToList();
                return db.users.AsNoTracking().ToList();
            }
        }

        /// <exception cref="OverflowException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public int CheckAvailableCount()
        {
            using (SQLiteContext<T> db = new())
                return db.users.Count();
        }

        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>        
        public List<int> Delete(bool persistentEntry = false, params int[] id)
        {
            using (SQLiteContext<T> db = new())
            {
                lock (obj)
                {
                List<int> idNotNull = new();
                List<T> toRemove = new();
                T element;

                foreach (var item in id)
                {
                    element = db.users.Find(item);
                    if (element is not null)
                        toRemove.Add(element);
                    else
                    {
                        if (persistentEntry is false)
                            throw new ArgumentException("Wrong ID is absent in database");
                        idNotNull.Add(item);
                    }
                }
                foreach (var item in toRemove)
                {
                    db.users.Remove(item);
                }
                db.SaveChanges();
                return idNotNull;
                }
            }
        }
    }
}

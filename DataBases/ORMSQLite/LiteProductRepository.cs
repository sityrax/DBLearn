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
        public List<T> Save(bool persistentEntry = false, params T[] products)
        {
            lock (obj)
            {
                using (SQLiteContext<T> db = new())
                {
                    List<T> productsNotNull = new();
                    List<T> toSave = new();
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
                            productsNotNull.Add(product);
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
                    return productsNotNull;
                }
            }
        }

        /// <returns>The entity or null.</returns>
        public T Get(int id)
        {
            using (SQLiteContext<T> db = new())
            {
                return db.products.Find(id);
            }
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The entity or null.</returns>
        public List<T> Get(string name)
        {
            using (SQLiteContext<T> db = new())
            {
                return db.products.AsQueryable().Where(x => x.ProductName.ToLower().Contains(name.ToLower()))
                                                .ToList();
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        public List<T> GetAll(Func<T, bool> predicate = null)
        {
            using (SQLiteContext<T> db = new())
            {
                if (predicate is not null)
                    return db.products.AsEnumerable().Where(predicate).ToList();
                return db.products.AsNoTracking().ToList();
            }
        }

        /// <exception cref="OverflowException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public int CheckAvailableCount()
        {
            using (SQLiteContext<T> db = new())
                return db.products.Count();
        }

        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>        
        public List<int> Delete(bool persistentEntry = false, params int[] id)
        {
            lock (obj)
            {
                using (SQLiteContext<T> db = new())
                {
                    List<int> idNotNull = new();
                    List<T> toRemove = new();
                    T element;

                    foreach (var item in id)
                    {
                        element = db.products.Find(item);
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
                        db.products.Remove(item);
                    }
                    db.SaveChanges();
                    return idNotNull;
                }
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;

namespace Services
{
    public interface IProductRepository<T>
    {
        T Get(int id);

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        List<T> Get(string nameOrSurname);

        /// <exception cref="ArgumentNullException"></exception>
        List<T> GetAll(Func<T, bool> predicate = null);

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="OverflowException"></exception>
        int CheckAvailableCount();

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>
        public List<T> Save(bool persistentEntry = false, params T[] users);

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DbUpdateException"></exception>
        /// <exception cref="DbUpdateConcurrencyException"></exception>
        public List<int> Delete(bool persistentEntry = false, params int[] id);
    }
}

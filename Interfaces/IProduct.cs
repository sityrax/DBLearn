using System;

namespace Services
{
    public interface IProduct<T>
    {
        public int? Id { get; set; }
        public decimal Price { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public DateTime ManufactureDate { get; set; }
    }
}
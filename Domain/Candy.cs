using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Services;
using System;

namespace Domain
{
    public class Candy : IProduct<Candy>
    {
        [Key]
        [Column("id")]
        public int? Id { get; set; }

        [Required]
        [Column("type")]
        public string Type { get; set; }

        [Required]
        [Column("composition")]
        public string Composition { get; set; }

        [Required]
        [Column("productname")]
        public string ProductName { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Required]
        [Column("weight")]
        public float Weight { get; set; }

        [Required]
        [Column("energy_value")]
        public float EnergyValue { get; set; }

        [Required]
        [Column("brand")]
        public string Brand { get; set; }

        [Required]
        [Column("manufacture_date")]
        public DateTime ManufactureDate { get; set; }

        public Candy(string productName, string composition, string type, float weight, float energyValue, string brand, DateTime manufactureDate, decimal price, int? id = null)
        {
            this.Id = id;
            this.Type = type;
            this.Weight = weight;
            this.EnergyValue = energyValue;
            this.Brand = brand;
            this.ProductName = productName;
            this.Composition = composition;
            this.ManufactureDate = manufactureDate;
            this.Price = price;
        }

        public Candy() { }

        public override bool Equals(object obj)
        {
            var candy = obj as Candy;
            if (candy.ProductName == this.ProductName &&
                candy.Type == this.Type &&
                candy.Price == this.Price &&
                candy.Brand == this.Brand &&
                candy.EnergyValue == this.EnergyValue &&
                candy.Weight == this.Weight &&
                candy.Composition == this.Composition &&
                (candy.ManufactureDate.Date == this.ManufactureDate.Date &&
                 candy.ManufactureDate.Hour == this.ManufactureDate.Hour &&
                 candy.ManufactureDate.Minute == this.ManufactureDate.Minute &&
                 candy.ManufactureDate.Second == this.ManufactureDate.Second)
            )
                return true;
            else
                return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}

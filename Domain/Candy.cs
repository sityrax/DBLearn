using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Services;
using System;

namespace Domain
{
    [Table("products")]
    public class Candy : IProduct<Candy>
    {
        [Key]
        [Column("id", TypeName = "INTEGER")]
        public int? Id { get; set; }

        [Required]
        [Column("type", TypeName = "VARCHAR(64)")]
        public string Type { get; set; }

        [Required]
        [Column("composition", TypeName = "VARCHAR(64)")]
        public string Composition { get; set; }

        [Required]
        [Column("product_name", TypeName = "VARCHAR(64)")]
        public string ProductName { get; set; }

        [Required]
        [Column("price", TypeName = "NUMERIC(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("weight", TypeName = "REAL")]
        public float Weight { get; set; }

        [Required]
        [Column("energy_value", TypeName = "REAL")]
        public float EnergyValue { get; set; }

        [Required]
        [Column("brand", TypeName = "VARCHAR(64)")]
        public string Brand { get; set; }

        [Required]
        [Column("manufacture_date", TypeName = "DATETIME")]
        public DateTime ManufactureDate { get; set; }

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

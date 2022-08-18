#define VARIANT
#define VARIANT2
#define OWNED
#define USELAZYLOADING // предполагает неявную автоматическую загрузку связанных данных при обращении к навигационному свойству (такое свойство не представляют непосредственно конкретное поле таблицы, а используются для доступа к данным, связанным с выбранной записью таблице).

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;

namespace ORMPostgreSQL
{
    public class SQLiteContextComplex : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public string DBFileName { get; private set; } = "users";

        public SQLiteContextComplex() 
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
#if USELAZYLOADING
                .UseLazyLoadingProxies()
#endif
                .UseSqlite($"Data Source = {DBFileName}.db");
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        }

        // тестируем FluentApi.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>().HasKey(u => u.AltId); // можно не указывать т.к. по умолчанию Id или [Имя_класса]Id, например, UserId.
            modelBuilder.Entity<User>().HasOne(u => u.Company)   // устанавливаем тип связи (один ко многим в данном случае).
                                       .WithMany(c => c.Users)
                                       .HasForeignKey(ui => ui.CompanyId);  // назначаем внешний ключ.
            modelBuilder.Entity<User>().Property(p => p.Age)
                                       .IsRequired();   // добавляем требование NOT NULL к свойству (атрибуту) Age.
            modelBuilder.Entity<User>().Ignore(i => i.toIgnore);    // игнорируем свойство toIgnore (не добавляем в таблицу).
#if VARIANT2
            modelBuilder.Entity<User>().HasOne(u => u.FullName)
                                       .WithOne(ui => ui.User)
                                       .HasForeignKey<UserInfo>(ui => ui.Id);   // определяем связь User с UserInfo по внешнему ключу для последующего добавления данных навигационого свойства в единую таблицу users.
            modelBuilder.Entity<User>().ToTable("users");   // добавялем свойства типа User в таблицу users.
            modelBuilder.Entity<UserInfo>().ToTable("users");   // добавляем свойства типа UserInfo в таблицу users.
#endif
#if (VARIANT || VARIANT2) && !OWNED
            modelBuilder.Entity<UserInfo>().OwnsOne(u => u.Patronymic);
#endif
#if (!VARIANT && !VARIANT2) && !OWNED
            // Главной сущностью здесь является класс User, который содержит объект класса UserInfo. Класс UserInfo, в свою очередь, также содержит объекты еще одного класса - Patronymic. OwnsOne включает объекты типов UserInfo и Patronymic в результирующую таблицу User, для этого этим типам не нужен свое поле id.
            modelBuilder.Entity<User>().OwnsOne(us => us.FullName, u => 
            {
                u.OwnsOne(c => c.Patronymic);
            });
#endif
        }
    }

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
#if USELAZYLOADING
        public virtual List<User> Users { get; set; } = new();
#else
        public List<User> Users { get; set; } = new();
#endif
    }

    public class User
    {
        public int Id { get; set; }
#if USELAZYLOADING
        public virtual UserInfo FullName { get; set; }
#else
        public UserInfo FullName { get; set; }
#endif
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public bool toIgnore { get; set; }
#if USELAZYLOADING
        public virtual Company Company { get; set; }
#else
        public Company Company { get; set; }
#endif
    }

    public class UserInfo
    {
#if VARIANT || (!VARIANT && !VARIANT2 && OWNED) || (!VARIANT && VARIANT2)
        public int Id { get; set; }
#endif
#if VARIANT2
#if USELAZYLOADING
        public virtual User User { get; set; }
#else
        public User User { get; set; }
#endif
#endif
#if !VARIANT && VARIANT2
        public int UserId { get; set; }
#endif
        public string Name { get; set; }
        public string Surname { get; set; }
#if USELAZYLOADING
        public virtual Patronymic Patronymic { get; set; }
#else
        public Patronymic Patronymic { get; set; }
#endif
    }

#if OWNED
    [Owned]
#endif
    public class Patronymic
    {
        public int Key { get; set; }
        public string Name { get; set; }
    }
}

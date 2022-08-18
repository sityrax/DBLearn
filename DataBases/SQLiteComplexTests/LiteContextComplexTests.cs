using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace ORMPostgreSQL.IntegrationTests
{
    [TestClass()]
    public class LiteContextComplexTests
    {
        static Company[] companies;
        static User[] users;

        [ClassInitialize]
        public static void TestInit(TestContext testContext)
        {
            //Arrange
            Patronymic i       = new Patronymic { Key = 1, Name = "I." };
            Patronymic a       = new Patronymic { Key = 2, Name = "A." };  // нельзя добавлять один и тот же объект разным владельцам.
            Patronymic e       = new Patronymic { Key = 1, Name = "E." };
            Patronymic unknown = new Patronymic { Key = 1, Name = "unknown" };

            Company allsafe    = new Company { Name = "Allsafe Cybersecurity" };
            Company metacortex = new Company { Name = "Metacortex" };
            Company merlaut    = new Company { Name = "The Merlaut" };
            companies = new Company[] { allsafe, metacortex, merlaut };

            #region Users add
            UserInfo genaInfo   = new UserInfo { Name = "Gena",    Surname = "Hacker",   Patronymic = i };
            UserInfo tomasInfo  = new UserInfo { Name = "Tomas",   Surname = "Anderson", Patronymic = a };
            UserInfo elliotInfo = new UserInfo { Name = "Elliot",  Surname = "Alderson", Patronymic = e };
            UserInfo aidenInfo  = new UserInfo { Name = "Aiden",   Surname = "Pearce",   Patronymic = unknown };

            User gena   = new User { FullName = genaInfo,   Company = merlaut };
            User tomas  = new User { FullName = tomasInfo,  Company = metacortex };
            User elliot = new User { FullName = elliotInfo, Company = allsafe };
            User aiden  = new User { FullName = aidenInfo,  Company = merlaut };
            users = new User[] { gena, tomas, elliot, aiden };
            #endregion

            using (SQLiteContextComplex db = new SQLiteContextComplex())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Companies.AddRange(companies);
                db.Users.AddRange(users);
                db.SaveChanges();
            }
        }

        [TestMethod()]
        public void EagerLoadingTest()
        {
            using(SQLiteContextComplex db = new SQLiteContextComplex())
            { 
                //Act
                var result = db.Companies.Include(c => c.Users).ThenInclude(u => u.FullName).ToList();

                //Assert
                for (int i = 0; i < result.Count; i++)
                {
                    for (int j = 0; j < result[i].Users.Count; j++)
                    {
                        for (int k = 0; k < users.Length; k++)
                        {
                        if (users[k].Id                       != result[i].Users[j].Id ||
                            users[k].FullName.Name            != result[i].Users[j].FullName.Name ||
                            users[k].FullName.Surname         != result[i].Users[j].FullName.Surname ||
                            users[k].FullName.Patronymic.Name != result[i].Users[j].FullName.Patronymic.Name ||
                            users[k].Company.Name             != result[i].Users[j].Company.Name)
                        {
                            if (users.Length - k == 1)
                                throw new Exception();
                        }
                        else
                            break;
                        }
                    }
                }
                Assert.IsTrue(true);
            }
        }

        [TestMethod()]
        public void ExplicitLoadingTest()
        {
            using(SQLiteContextComplex db = new SQLiteContextComplex())
            {
                var usersCollection = db.Users.AsEnumerable();
                db.Users.Load();  // метод Include не используется
                foreach (var item in usersCollection)
                {
                    db.Entry(item).Reference(c => c.Company).Load();
                    db.Entry(item).Reference(c => c.FullName).Load();
                }

                //Assert
                for (int i = 0; i < usersCollection.Count(); i++)
                {
                    for (int j = 0; j < users.Length; j++)
                    {
                        if (usersCollection.ElementAt(i).Id                       != users[j].Id ||
                            usersCollection.ElementAt(i).FullName.Name            != users[j].FullName.Name ||
                            usersCollection.ElementAt(i).FullName.Surname         != users[j].FullName.Surname ||
                            usersCollection.ElementAt(i).FullName.Patronymic.Name != users[j].FullName.Patronymic.Name ||
                            usersCollection.ElementAt(i).Company.Name             != users[j].Company.Name ||
                            usersCollection.ElementAt(i).Company.Id               != users[j].Company.Id)
                        {
                            if (usersCollection.Count() - j == 1)
                                throw new Exception();
                        }
                        else
                            break;
                    }
                }
                Assert.IsTrue(true);
            }
        }

        [TestMethod()]
        public void LazyLoadingCompaniesTest()
        {
            using (SQLiteContextComplex db = new SQLiteContextComplex())
            {
                var companies = db.Companies.ToList();

                //Assert
                for (int i = 0; i < companies.Count(); i++)
                {
                    if(companies[i].Users.Count > 0)
                    for (int j = 0; j < companies[i].Users.Count; j++)
                    {
                        if (companies[i].Users[j].Id                       != users[i].Id ||
                            companies[i].Users[j].Age                      != users[i].Age ||
                            companies[i].Users[j].FullName.Name            != users[i].FullName.Name ||
                            companies[i].Users[j].FullName.Surname         != users[i].FullName.Surname ||
                            companies[i].Users[j].FullName.Patronymic.Name != users[i].FullName.Patronymic.Name ||
                            companies[i].Users[j].Company.Name             != users[i].Company.Name ||
                            companies[i].Users[j].Company.Id               != users[i].Company.Id)
                        {
                            if (users.Length - j == 1)
                                throw new Exception($"Some properties of User {j} in Company {i} is wrong.");
                        }
                        else
                            break;
                    }
                    else
                        throw new Exception($"Users collection in company {i} is empty.");
                }
                Assert.IsTrue(true);
            }
        }

        [TestMethod()]
        public void LazyLoadingUsersTest()
        {
            using (SQLiteContextComplex db = new SQLiteContextComplex())
            {
                var usersCollection = db.Users.ToList();

                //Assert
                for (int i = 0; i < companies.Count(); i++)
                {
                    for (int j = 0; j < users.Length; j++)
                    {
                        if (usersCollection[i].Id                       != users[j].Id ||
                            usersCollection[i].Age                      != users[j].Age ||
                            usersCollection[i].FullName.Name            != users[j].FullName.Name ||
                            usersCollection[i].FullName.Surname         != users[j].FullName.Surname ||
                            usersCollection[i].FullName.Patronymic.Name != users[j].FullName.Patronymic.Name ||
                            usersCollection[i].Company.Name             != users[j].Company.Name ||
                            usersCollection[i].Company.Id               != users[j].Company.Id)
                        {
                            if (users.Length - j == 1)
                                throw new Exception();
                        }
                        else
                            break;
                    }
                }
                Assert.IsTrue(true);
            }
        }

        [ClassCleanup]
        public static void Delete()
        {
            using (SQLiteContextComplex db = new SQLiteContextComplex())
                db.Database.EnsureDeleted();
        }
    }
}
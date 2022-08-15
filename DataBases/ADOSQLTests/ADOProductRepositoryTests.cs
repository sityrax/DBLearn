using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System;
using Domain;

namespace ADO.IntegrationTests
{
    [TestClass()]
    public class ADOProductRepositoyTests
    {
        static ADOProductRepository<Candy> repository;
        static List<Candy> expectedCollection;
        static int additionalCount = 100;
        static int[] id = new int[2];
        static int startId = 100;
        static int secondId { get => startId + 500; }

        [ClassInitialize()]
        public static void SaveTestInit(TestContext testContext)
        {
            repository = new ADOProductRepository<Candy>(host: "localhost", 
                                                         database: "master", 
                                                         trusted_Connection: true);
            expectedCollection = new List<Candy>();
            for (int i = 0; i < additionalCount; i++)
            {
                expectedCollection.Add( new Candy()
                {
                    Id              = startId + i,
                    ProductName     = "ProductName" + i,
                    Brand           = "Brand" + i,
                    Type            = "Type" + i,
                    Composition     = "Composition" + i,
                    ManufactureDate = DateTime.Now,
                    EnergyValue     = 1.1f + i,
                    Weight          = 1.1f + i,
                    Price           = 1.01m
                });
            }

            expectedCollection.Add( new Candy()
            {
                Id              = startId + additionalCount,
                ProductName     = "ProductName" + additionalCount,
                Brand           = "Brand" + additionalCount,
                Type            = "Type" + additionalCount,
                Composition     = "Composition" + additionalCount,
                ManufactureDate = DateTime.Now,
                EnergyValue     = 1.1f + additionalCount,
                Weight          = 1.1f + additionalCount,
                Price           = 1.01m
            });
            additionalCount++;

            //Act
            repository.DeleteAll();
            repository.Save(true, expectedCollection.ToArray());
        }

        [TestMethod]
        public void SaveWithoutIdTest()
        {
            //Arrange
            repository = new ADOProductRepository<Candy>(host: "localhost",
                                                         database: "master",
                                                         trusted_Connection: true);
            Candy expectedValue = new Candy()
            {
                ProductName     = "SaveWithoutIdTest",
                Brand           = "SaveWithoutIdTest",
                Type            = "SaveWithoutIdTest",
                Composition     = "SaveWithoutIdTest",
                ManufactureDate = DateTime.Now,
                EnergyValue     = 1.1f,
                Weight          = 1.1f,
                Price           = 1.01m
            };

            //Act
            repository.Save(true, expectedValue);

            Candy actual = repository.GetAll(x => x.Equals(expectedValue)).First();
            repository.Delete(false, (int)actual.Id);

            //Assert
            Assert.AreEqual(expectedValue, actual);
        }

        [TestMethod]
        public void CheckAvailableTest()
        {
            //Act
            int actual = repository.CheckAvailableCount();

            //Assert
            Assert.IsTrue(actual >= 3);
        }

        [TestMethod]
        public void GetTest()
        {
            //Act
            Candy actual = repository.Get(expectedCollection[0].ProductName).First();

            //Assert
            Assert.AreEqual(expectedCollection[0], actual);
        }

        [TestMethod]
        public void GetAllTest()
        {
            //Arrange
            List<Candy> actual = new();

            //Act
            actual = repository.GetAll().ToList();
            actual = actual.OrderBy(x => x.Id).ToList();
            for (int i = 0; i < expectedCollection.Count; i++)
            {
                for (int j = 0; j < actual.Count; j++)
                {
                    if (expectedCollection[i].Equals(actual[j]))
                    {
                        break;
                    }
                    else
                        if(actual.Count - j == 1)
                             throw new AssertFailedException("Collection doesn't contain certain elements.");
                }
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetAllPredicateTest()
        {
            IEnumerable<Candy> specialForecast = expectedCollection.Skip(additionalCount - 3).ToList();

            //Act
            IEnumerable<Candy> actual = repository.GetAll(x => x.Id == expectedCollection[additionalCount - 3].Id ||
                                                               x.Id == expectedCollection[additionalCount - 2].Id ||
                                                               x.Id == expectedCollection[additionalCount - 1].Id);

            //Assert
            CollectionAssert.AreEqual((ICollection)specialForecast, (ICollection)actual);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod()]
        public void SaveSubsequenceTest()
        {
            //Arrange
            int[] idValues = new int[2];
            Candy[][] expected = new Candy[idValues.Length][];

            for (int i = 0; i < idValues.Length; i++)
            {
                idValues[i] = secondId + i;
            }
            for (int j = 0; j < expected.Length; j++)
            {
                expected[j] = new Candy[idValues.Length];
                for (int i = 0; i < expected[j].Length; i++)
                {
                    expected[j][i] = new Candy()
                    {
                        Id              = secondId + i,
                        ProductName     = "SaveSubsequence" + i,
                        Brand           = "SaveSubsequence" + i,
                        Type            = "SaveSubsequence" + i,
                        Composition     = "SaveSubsequence" + i,
                        ManufactureDate = DateTime.Now,
                        EnergyValue     = 1.1f + i,
                        Weight          = 1.1f + i,
                        Price           = 1.01m + i * 1.01m
                    };
                }
            }

            //Act
            try
            {
                repository.Save(false, expected[0]);
                repository.Save(false, expected[1]);
            }
            finally
            {
                repository.Delete(true, idValues);
            }
        }

        [TestMethod()]
        public void SaveAsyncTest()
        {
            //Arrange
            int[] idValues = new int[12];
            Task<List<Candy>>[] tasks = new Task<List<Candy>>[idValues.Length];
            Candy[][] expected = new Candy[tasks.Length][];

            for (int i = 0; i < idValues.Length; i++)
            {
                idValues[i] = secondId + i;
            }
            for (int j = 0; j < expected.Length; j++)
            {
                expected[j] = new Candy[tasks.Length];
                for (int i = 0; i < expected[j].Length; i++)
                {
                    expected[j][i] = new Candy()
                    {
                        Id              = secondId + i,
                        ProductName     = "SaveAsync" + i + "/" + j,
                        Brand           = "SaveAsync" + i + "/" + j,
                        Type            = "SaveAsync" + i + "/" + j,
                        Composition     = "SaveAsync" + i + "/" + j,
                        ManufactureDate = DateTime.Now,
                        EnergyValue     = 1.1f + i,
                        Weight          = 1.1f + i,
                        Price           = 1.01m + i * 1.01m
                    };
                }
                var iterator = j;
                tasks[iterator] = Task.Run(() => repository.Save(true, expected[iterator]));
            }
            Task.WaitAll(tasks);

            //Act
            IEnumerable<Candy> actual = repository.GetAll(x => x.ProductName.Contains("SaveAsync") &&
                                                               x.Brand.Contains("SaveAsync"));
            repository.Delete(true, idValues);

            bool actualBool = false;
            int counter = 0;
            for (int j = 0; j < expected.Length; j++)
            {
                counter = 0;
                for (int i = 0; i < actual.Count(); i++)
                {
                    if (expected[j][i].Equals(actual.ElementAt(i)))
                        counter++;
                }
                if (counter == expected.Length)
                {
                    actualBool = true;
                    break;
                }
            }

            //Assert
            Assert.IsTrue(actualBool);
        }

        [ClassCleanup]
        public static void DeleteTest()
        {
            int[] toDelete = new int[expectedCollection.Count];
            for (int i = 0; i < expectedCollection.Count; i++)
            {
                toDelete[i] = (int)expectedCollection[i].Id;
            }
            repository.Delete(true, toDelete);

            IEnumerable<Candy> enumerable = repository.GetAll(x => x.Id >= startId && 
                                                                   x.Id <= startId + additionalCount);

            if (enumerable.Count() == 0)
                enumerable = null;

            Assert.IsNull(enumerable);
        }

        [TestMethod()]
        public void GetLastIdTest()
        {
            //Arrange
            Random random = new();
            int id = int.MaxValue-random.Next(1, 125);

            //Act
            id = repository.GetLastId() + 1;
            Candy expected = new Candy()
            {
                Id              = id,
                ProductName     = "GetLastIdTest",
                Brand           = "GetLastIdTest",
                Type            = "GetLastIdTest",
                Composition     = "GetLastIdTest",
                ManufactureDate = DateTime.Now,
                EnergyValue     = 1.1f,
                Weight          = 1.1f,
                Price           = 1.01m
            };
            repository.Save(persistentEntry: true, expected);
            id = repository.GetLastId();
            Candy actual = repository.Get(id);
            repository.Delete(true, id);

            //Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
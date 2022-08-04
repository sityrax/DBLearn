using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using Domain;
using System;

namespace ADOSQL.IntegrationTests
{
    [TestClass()]
    public class ADOProductRepositoryDataSetTests
    {
        static ADOProductRepositoryDataSet<Candy> repository;
        static List<Candy> expectedCollection;
        static int additionalCount = 100;
        static int[] id = new int[2];
        static int startId = 100;
        static int secondId { get => startId + 500; }


        [ClassInitialize()]
        public static void SaveTestWithoutIdInit(TestContext testContext)
        {
            repository = new ADOProductRepositoryDataSet<Candy>(host: "localhost",
                                                                database: "master",
                                                                trusted_Connection: true);
            expectedCollection = new List<Candy>();
            for (int i = 0; i < additionalCount; i++)
            {
                expectedCollection.Add( new Candy()
                {
                    Id              = startId       + i,
                    ProductName     = "ProductName" + i,
                    Brand           = "Brand"       + i,
                    Type            = "Type"        + i,
                    Composition     = "Composition" + i,
                    EnergyValue     = 1.1f          + i,
                    Weight          = 1.1f          + i,
                    Price           = 1.01m,
                    ManufactureDate = DateTime.Now
                });
            }
            expectedCollection.Add(new Candy()
            {
                Id              = startId       + additionalCount,
                ProductName     = "ProductName" + additionalCount,
                Brand           = "Brand"       + additionalCount,
                Type            = "Type"        + additionalCount,
                Composition     = "Composition" + additionalCount,
                EnergyValue     = 1.1f          + additionalCount,
                Weight          = 1.1f          + additionalCount,
                Price           = 1.01m,
                ManufactureDate = DateTime.Now
            });  
            additionalCount++;

            //Act
            repository.DeleteAll();
            repository.Save(true, expectedCollection.ToArray());
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
                        if (actual.Count - j == 1)
                        throw new AssertFailedException("Collection doesn't contain certain elements.");
                }
            }

            //Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetAllPredicateTest()
        {
            IEnumerable<Candy> specialForecast = expectedCollection.Skip(additionalCount - 3).ToList();

            //Act
            IEnumerable<Candy> actual = repository.GetAll(x => x.ProductName == expectedCollection[additionalCount - 3].ProductName ||
                                                               x.ProductName == expectedCollection[additionalCount - 2].ProductName ||
                                                               x.ProductName == expectedCollection[additionalCount - 1].ProductName);

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
                List<Candy> returned = repository.GetAll(x => x.ProductName.Contains("SaveSubsequence") &&
                                                              x.Brand.Contains("SaveSubsequence"));
                for (int i = 0; i < idValues.Length; i++)
                {
                    idValues[i] = (int)returned[i].Id;
                }
                repository.Delete(true, idValues);
            }
        }

        [TestMethod()]
        public void SaveAsyncTest()
        {
            //Arrange
            Task<List<Candy>>[] tasks = new Task<List<Candy>>[12];
            List<int> idValues = new(tasks.Length);
            Candy[][] expected = new Candy[tasks.Length][];

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
            List<Candy> actual = repository.GetAll(x => x.ProductName.Contains("SaveAsync") && 
                                                        x.Brand.Contains("SaveAsync"));
            for (int i = 0; i < actual.Count; i++)
            {
                idValues.Add((int)actual[i].Id);
            }
            repository.Delete(true, idValues.ToArray());

            bool actualBool = false;
            int counter = 0;
            for (int j = 0; j < expected.Length; j++)
            {
                counter = 0;
                for (int i = 0; i < expected[j].Length; i++)
                {
                    if (expected[j][i].Equals(actual[i]))
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

        [TestMethod]
        public void DeleteAsyncTest()
        {
            //Arrange
            const int quantity = 10;
            Task<List<int>>[] deleteTasks = new Task<List<int>>[quantity];
            Task<List<Candy>>[] saveTasks = new Task<List<Candy>>[quantity];

            int[] idValues = new int[quantity];
            Candy[] expected = new Candy[quantity];

            //Act
            try
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    expected[i] = new Candy()
                    {
                        Id              = idValues[i]   = (secondId + i),
                        ProductName     = "DeleteAsync" + i,
                        Brand           = "DeleteAsync" + i,
                        Type            = "DeleteAsync" + i,
                        Composition     = "DeleteAsync" + i,
                        ManufactureDate = DateTime.Now,
                        EnergyValue     = 1.1f + i,
                        Weight          = 1.1f + i,
                        Price           = 1.01m + i * 1.01m
                    };
                }
                for (int j = 0; j < expected.Length; j++)
                {
                    var iterator = j;
                    saveTasks[iterator]   = Task.Run(() => repository.Save(true, expected[iterator]));
                    deleteTasks[iterator] = Task.Run(() => repository.Delete(false, idValues[iterator]));
                    Task.WaitAll(saveTasks[iterator], deleteTasks[iterator]);
                }
            }
            finally
            {
                repository.Delete(true, idValues);
            }
        }

        [ClassCleanup]
        public static void DeleteTest()
        {
            //Arrange
            List<Candy> toDeleteEntities = repository.GetAll(x => x.ProductName.Contains("ProductName") && 
                                                                  x.Composition.Contains("Composition"));
            IEnumerable<int> enumerable = null;
            int[] toDelete = null;
            if (toDeleteEntities is not null)
            {
                toDelete = new int[toDeleteEntities.Count];
                for (int i = 0; i < toDeleteEntities.Count; i++)
                {
                    toDelete[i] = (int)toDeleteEntities[i].Id;
                }

            //Act
            enumerable = repository.Delete(true, toDelete);
            }

            //Assert
            Assert.IsNull(enumerable);
        }
    }
}
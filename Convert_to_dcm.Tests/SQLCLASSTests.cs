using Microsoft.VisualStudio.TestTools.UnitTesting;
using Convert_to_dcm.Sql;
using Convert_to_dcm.Model; // For SettingsModel & DataSets
using System.Data;
using System; // For ArgumentNullException, InvalidOperationException

namespace Convert_to_dcm.Tests
{
    [TestClass]
    public class SQLCLASSTests
    {
        private SettingsModel CreateValidSettings()
        {
            return new SettingsModel
            {
                ServerAddress = "VALID_SERVER",
                Instance = "SQLEXPRESS", // Instance can be empty if not used, but provide for completeness
                Catalog = "VALID_CATALOG",
                username = "user",
                password = "password",
                ServerModality = DataSets.Modality.CT
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullSettingsModel_ThrowsArgumentNullException()
        {
            new SQLCLASS(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_NullServerAddress_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.ServerAddress = null;
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_EmptyServerAddress_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.ServerAddress = "";
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_NullCatalog_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.Catalog = null;
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_EmptyCatalog_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.Catalog = "";
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_NullUsername_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.username = null;
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_EmptyUsername_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.username = "";
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_NullPassword_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.password = null;
            new SQLCLASS(settings);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_EmptyPassword_ThrowsInvalidOperationException()
        {
            SettingsModel settings = CreateValidSettings();
            settings.password = "";
            new SQLCLASS(settings);
        }

        [TestMethod]
        public void ExecuteSelectQuery_InvalidServerAddress_ThrowsSqlCustomException()
        {
            SettingsModel settings = new SettingsModel
            {
                ServerAddress = "INVALID_SERVER_ADDRESS_THAT_DOES_NOT_EXIST",
                Instance = "SQLEXPRESS_NON_EXISTENT", // Instance name, can be anything if server is invalid
                Catalog = "AnyDB",
                username = "anyuser",
                password = "anypassword",
                ServerModality = DataSets.Modality.CT
            };
            ISqlService sqlService = new SQLCLASS(settings);

            // Assert that SqlCustomException is thrown when the query is executed
            Assert.ThrowsException<SqlCustomException>(() =>
            {
                // Parameters for pid and modality don't matter much here as connection should fail first
                sqlService.ExecuteSelectQuery("test_pid", "CT");
            });
        }

        // [TestMethod]
        // [Ignore("Requires test database or further refactoring for DB mocking")]
        // public void ExecuteSelectQuery_ValidQuery_ReturnsData()
        // {
        //     // TODO: Setup test database and valid settings
        //     // SettingsModel settings = new SettingsModel { /* Valid connection to test DB */ };
        //     // ISqlService sqlService = new SQLCLASS(settings);
        //     // DataTable result = sqlService.ExecuteSelectQuery("EXISTING_PID", "EXISTING_MODALITY");
        //     // Assert.IsNotNull(result);
        //     // Assert.IsTrue(result.Rows.Count > 0);
        // }
    }
}

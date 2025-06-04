using Convert_to_dcm.Model; // Changed from Convert_to_dcom.Class
using Microsoft.Data.SqlClient;
using System.Data;
// using static System.ComponentModel.Design.ObjectSelectorEditor; // This seems unused, removing
// using System.Security.Cryptography; // This seems unused, removing
using System;

namespace Convert_to_dcm.Sql
{
    // Define a custom exception class
    public class SqlCustomException : Exception
    {
        public SqlCustomException() { }
        public SqlCustomException(string message) : base(message) { }
        public SqlCustomException(string message, Exception inner) : base(message, inner) { }
    }

    public class SQLCLASS : ISqlService
    {
        private readonly SettingsModel _setting;
        private readonly string constring;

        public SQLCLASS(SettingsModel setting)
        {
            _setting = setting ?? throw new ArgumentNullException(nameof(setting), "SettingsModel cannot be null.");

            if (String.IsNullOrEmpty(_setting.ServerAddress) ||
                String.IsNullOrEmpty(_setting.Catalog) ||
                String.IsNullOrEmpty(_setting.username) ||
                String.IsNullOrEmpty(_setting.password))
            {
                throw new InvalidOperationException("Required database settings are missing. Please check ServerAddress, Catalog, Username, and Password.");
            }

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = String.IsNullOrEmpty(_setting.Instance) ? _setting.ServerAddress : $"{_setting.ServerAddress}\\{_setting.Instance}",
                    InitialCatalog = _setting.Catalog,
                    IntegratedSecurity = false,
                    UserID = _setting.username,
                    Password = _setting.password,
                    TrustServerCertificate = true
                };
                constring = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to build connection string: " + ex.Message, ex);
            }
        }

        public DataTable ExecuteSelectQuery(string pid, string modality)
        {
            if (String.IsNullOrEmpty(constring))
            {
                throw new InvalidOperationException("Connection string is not initialized. SQLCLASS may not have been constructed properly.");
            }

            string query = @"
                            SELECT a.StudyInsUID, b.SOPClassUID,a.PName
                            FROM [PPWDB].[dbo].[StudyTab] AS a
                            INNER JOIN [PPWDB].[dbo].[ImageTab] AS b ON a.StudyKey = b.StudyKey
                            WHERE a.PID = @PID and Modality = @MODALITY";

            using (SqlConnection connection = new SqlConnection(constring))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PID", pid);
                    command.Parameters.AddWithValue("@MODALITY", modality);

                    try
                    {
                        connection.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable resultTable = new DataTable();
                            adapter.Fill(resultTable);
                            return resultTable;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new SqlCustomException("A database error occurred while executing the select query: " + ex.Message, ex);
                    }
                    catch (Exception ex)
                    {
                        throw new SqlCustomException("An unexpected error occurred while executing the select query: " + ex.Message, ex);
                    }
                }
            }
        }
    }
}

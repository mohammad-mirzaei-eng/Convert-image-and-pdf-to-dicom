using Convert_to_dcom.Class;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Convert_to_dcm.Sql
{
    internal class SQLCLASS
    {
        private readonly SettingsModel _setting;
        private string constring;

        public SQLCLASS(SettingsModel setting)
        {
            _setting = setting;

        }

        public DataTable ExecuteSelectQuery(string pid)
        {
            string query = @"
                            SELECT a.StudyInsUID, b.SOPClassUID
                            FROM [PPWDB].[dbo].[StudyTab] AS a
                            INNER JOIN [PPWDB].[dbo].[ImageTab] AS b ON a.StudyKey = b.StudyKey
                            WHERE a.PID = @PID";
            if (!String.IsNullOrEmpty(_setting.Instance) && !String.IsNullOrEmpty(_setting.username) && !String.IsNullOrEmpty(_setting.Catalog) && !String.IsNullOrEmpty(_setting.password) && !String.IsNullOrEmpty(_setting.ServerAddress))
            {
                constring = String.Format("Data Source={0}\\{1};Initial Catalog={2};Integrated Security=False;User ID={3};Password={4};TrustServerCertificate=True", _setting.ServerAddress, _setting.Instance, _setting.Catalog, _setting.username, _setting.password);
                using (SqlConnection connection = new SqlConnection(constring))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PID", pid);

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
                        catch (Exception ex)
                        {
                            // Handle exception (e.g., log it)
                            throw new ApplicationException("An error occurred while executing the SQL query.", ex);
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("One or more of the connection settings are missing.");
            }
        }
    }
}

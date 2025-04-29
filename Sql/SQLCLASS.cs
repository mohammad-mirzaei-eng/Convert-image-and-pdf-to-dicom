using Convert_to_dcom.Class;
using Microsoft.Data.SqlClient;
using System.Data;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using System.Security.Cryptography;
using Convert_to_dcm.Helper;

namespace Convert_to_dcm.Sql
{
    internal class SQLCLASS
    {
        private readonly SettingsModel _setting;
        private string constring = string.Empty; // Initialize with a default value

        public SQLCLASS(SettingsModel setting)
        {
            if (setting == null)
            {
                ErrHelper.Instance.LogError(constring, new ArgumentNullException(nameof(setting))).Wait();
                return;
            }
            _setting = setting;
        }

        public DataTable ExecuteSelectQuery(string pid, string modality)
        {
            string query = @"
                                SELECT a.StudyInsUID, b.SOPClassUID,a.PName
                                FROM [PPWDB].[dbo].[StudyTab] AS a
                                INNER JOIN [PPWDB].[dbo].[ImageTab] AS b ON a.StudyKey = b.StudyKey
                                WHERE a.PID = @PID and Modality = @MODALITY";
            if (!String.IsNullOrEmpty(_setting.Instance) && !String.IsNullOrEmpty(_setting.username) && !String.IsNullOrEmpty(_setting.Catalog) && !String.IsNullOrEmpty(_setting.password) && !String.IsNullOrEmpty(_setting.ServerAddress))
            {
                constring = String.Format("Data Source={0}\\{1};Initial Catalog={2};Integrated Security=False;User ID={3};Password={4};TrustServerCertificate=True", _setting.ServerAddress, _setting.Instance, _setting.Catalog, _setting.username, _setting.password);
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
                        catch (Exception)
                        {
                            ErrHelper.Instance.LogError(query, new Exception("Error executing SQL query")).Wait();
                            return new DataTable();
                        }
                    }
                }
            }
            else
            {
                return new DataTable();
            }
        }
    }
}

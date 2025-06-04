using static Convert_to_dcm.Model.DataSets; // This using should still be correct

namespace Convert_to_dcm.Model // Changed namespace
{
    [Serializable]
    public class SettingsModel
    {
        public SettingsModel()
        {
            ServerAddress = string.Empty;
            ServerTitle = string.Empty;
            ServerAET = string.Empty;
            Instance = string.Empty;
            username = string.Empty;
            password = string.Empty;
            Catalog = string.Empty;
            // ServerModality will use its default (CT) if not set
        }

        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ServerTitle { get; set; }
        public string ServerAET { get; set; }
        public bool ServerUseTls { get; set; }
        public string Instance { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string Catalog { get; set; }
        public Modality ServerModality { get; set; }
    }
}

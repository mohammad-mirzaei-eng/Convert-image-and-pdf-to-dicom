using Convert_to_dcm.Helper;
using Convert_to_dcom.Class;
using Convert_to_dcom.Helper;
using System.Text.Json;

namespace FileCopyer.Classes.Design_Patterns.Helper
{
    internal class SerializationHelper 
    {
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ErrHelper _errHelper = ErrHelper.Instance;
        public SerializationHelper(IEncryptionHelper encryptionHelper)
        {
            _encryptionHelper = encryptionHelper;
        }
        
        // JSON serializer options for serialization and deserialization
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Get the file path for file models or settings
        private static string GetFilePath(bool isFileModel)
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConvertToDcm");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, isFileModel ? "fileModels.bin" : "settings.bin");
        }

        // Save the settings to a binary file
        public static async Task SaveSettings(SettingsModel settings)
        {
            try
            {
                if (settings == null)
                {
                    return;
                }

                settings.ServerAddress = Encrypt(settings.ServerAddress);
                settings.username = Encrypt(settings.username);
                settings.password = Encrypt(settings.password);
                settings.Instance = Encrypt(settings.Instance);
                settings.Catalog = Encrypt(settings.Catalog);
                settings.ServerTitle = Encrypt(settings.ServerTitle);
                settings.ServerAET = Encrypt(settings.ServerAET);

                string filePath = GetFilePath(false);
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await JsonSerializer.SerializeAsync(fs, settings,_jsonOptions);
                }
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error saving settings", ex);
            }
        }

        // Load the settings from a binary file
        public static async Task<SettingsModel> LoadSettings()
        {
            try
            {
                string filePath = GetFilePath(false);
                if (!File.Exists(filePath))
                {
                    return new SettingsModel();
                }

                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                if (fs.Length == 0)
                {
                    return new SettingsModel();
                }

                SettingsModel settings = await JsonSerializer.DeserializeAsync<SettingsModel>(fs,_jsonOptions) ?? new SettingsModel();

                // Decrypt sensitive data
                settings.username = Decrypt(settings.username);
                settings.password = Decrypt(settings.password);
                settings.Instance = Decrypt(settings.Instance);
                settings.ServerAddress = Decrypt(settings.ServerAddress);
                settings.Catalog = Decrypt(settings.Catalog);
                settings.ServerTitle = Decrypt(settings.ServerTitle);
                settings.ServerAET = Decrypt(settings.ServerAET);

                return settings;

            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error saving settings", ex);
                return new SettingsModel();
            }
        }

        public static string Encrypt(string value)
        {
            return string.IsNullOrEmpty(value) ? value : EncryptionHelper.Encrypt(value);
        }

        public static string Decrypt(string value)
        {
            return string.IsNullOrEmpty(value) ? value : EncryptionHelper.Decrypt(value);
        }
    }

}

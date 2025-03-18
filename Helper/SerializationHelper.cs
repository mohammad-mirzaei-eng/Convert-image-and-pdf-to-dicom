using Convert_to_dcom.Class;
using Convert_to_dcom.Helper;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace FileCopyer.Classes.Design_Patterns.Helper
{
    internal static class SerializationHelper
    {
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
        public static void SaveSettings(SettingsModel settings)
        {
            if (settings == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(settings.ServerAddress))
            {
                settings.ServerAddress = EncryptionHelper.Encrypt(settings.ServerAddress);
            }
            if (!string.IsNullOrEmpty(settings.username))
            {
                settings.username = EncryptionHelper.Encrypt(settings.username);
            }
            if (!string.IsNullOrEmpty(settings.password))
            {
                settings.password = EncryptionHelper.Encrypt(settings.password);
            }
            if (!string.IsNullOrEmpty(settings.Instance))
            {
                settings.Instance = EncryptionHelper.Encrypt(settings.Instance);
            }
            if (!string.IsNullOrEmpty(settings.Catalog))
            {
                settings.Catalog = EncryptionHelper.Encrypt(settings.Catalog);
            }
            if (!string.IsNullOrEmpty(settings.ServerTitle))
            {
                settings.ServerTitle = EncryptionHelper.Encrypt(settings.ServerTitle);
            }
            if (!string.IsNullOrEmpty(settings.ServerAET))
            {
                settings.ServerAET = EncryptionHelper.Encrypt(settings.ServerAET);
            }

            string filePath = GetFilePath(false);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                JsonSerializer.Serialize(fs, settings);
            }
        }

        // Load the settings from a binary file
        public static SettingsModel LoadSettings()
        {
            string filePath = GetFilePath(false);
            if (!File.Exists(filePath))
            {
                return new SettingsModel();
            }

            using FileStream fs = new FileStream(filePath, FileMode.Open);
            if (fs.Length == 0)
            {
                return new SettingsModel();
            }

            SettingsModel settings = JsonSerializer.Deserialize<SettingsModel>(fs) ?? new SettingsModel();

            // Decrypt sensitive data
            if (!string.IsNullOrEmpty(settings.username))
            {
                settings.username = EncryptionHelper.Decrypt(settings.username);
            }
            if (!string.IsNullOrEmpty(settings.password))
            {
                settings.password = EncryptionHelper.Decrypt(settings.password);
            }
            if (!string.IsNullOrEmpty(settings.Instance))
            {
                settings.Instance = EncryptionHelper.Decrypt(settings.Instance);
            }
            if (!string.IsNullOrEmpty(settings.ServerAddress))
            {
                settings.ServerAddress = EncryptionHelper.Decrypt(settings.ServerAddress);
            }
            if (!string.IsNullOrEmpty(settings.Catalog))
            {
                settings.Catalog = EncryptionHelper.Decrypt(settings.Catalog);
            }
            if (!string.IsNullOrEmpty(settings.ServerTitle))
            {
                settings.ServerTitle = EncryptionHelper.Decrypt(settings.ServerTitle);
            }
            if (!string.IsNullOrEmpty(settings.ServerAET))
            {
                settings.ServerAET = EncryptionHelper.Decrypt(settings.ServerAET);
            }

            return settings;
        }
    }

}

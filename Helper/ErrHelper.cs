using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convert_to_dcm.Helper
{
    internal class ErrHelper
    {
        // فیلد استاتیک برای نگهداری نمونه Singleton
        private static readonly Lazy<ErrHelper> _instance = new Lazy<ErrHelper>(() => new ErrHelper());

        // سازنده خصوصی برای جلوگیری از ایجاد نمونه‌های جدید
        private ErrHelper() { }

        // پراپرتی استاتیک برای دسترسی به نمونه Singleton
        public static ErrHelper Instance => _instance.Value;

        public async Task LogError(string message, Exception ex)
        {
            try
            {
                string errorDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Errors");
                if (!Directory.Exists(errorDirectory))
                {
                    Directory.CreateDirectory(errorDirectory);
                }
                string logFilePath = Path.Combine(errorDirectory, "error_Main_log" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    await writer.WriteLineAsync($"[{DateTime.Now}] {message}");
                    await writer.WriteLineAsync(ex.ToString());
                    await writer.WriteLineAsync();
                }
            }
            catch (Exception logEx)
            {
                MessageBox.Show($"Error logging exception: {logEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task LogError(string message)
        {
            try
            {
                string errorDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Errors");
                if (!Directory.Exists(errorDirectory))
                {
                    Directory.CreateDirectory(errorDirectory);
                }
                string logFilePath = Path.Combine(errorDirectory, "error_Main_log" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    await writer.WriteLineAsync($"[{DateTime.Now}] {message}");
                    await writer.WriteLineAsync();
                }
            }
            catch (Exception logEx)
            {
                MessageBox.Show($"Error logging exception: {logEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using Convert_to_dcm.Sql;
using Convert_to_dcom.Class; // For SettingsModel used as property
using Convert_to_dcom.Class.Helper; // For SerializationHelper, Serverhelper
using Convert_to_dcm.Helper; // For DicomConversionHelper
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FileCopyer.Classes.Design_Patterns.Helper;
using PdfiumViewer;
using System.Data;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace Convert_to_dcm
{
    public partial class Main : System.Windows.Forms.Form
    {
        private SettingsModel SettingsModel { get; set; } = new SettingsModel();
        private PatientModel PatientModel { get; set; }
        private List<string> ImagePath = new List<string>();
        private (string StudyInsUID, string SOPClassUID, string PName)? cachedTags = null;
        private string? cachedPatientID = null;
        private float ZoomFactor { get; set; } = 1.0f;
        private Bitmap? Img { get; set; }

        public Main()
        {
            try
            {
                InitializeComponent();
                PatientModel = new PatientModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LogError(string message, Exception ex)
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

        private async void LogError(string message)
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
                // Clipboard operations removed
                MessageBox.Show($"Error logging exception: {logEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pic1_MouseWheel(object? sender, MouseEventArgs e)
        {
            try
            {
                if (sender is PictureBox pictureBox)
                {
                    if (pictureBox.Image == null)
                    {
                        return;
                    }

                    AdjustZoomFactor(e.Delta);
                    UpdatePictureBoxSize(pictureBox);
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling mouse wheel event ", ex);
                MessageBox.Show($"Error handling mouse wheel event: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AdjustZoomFactor(int delta)
        {
            try
            {
                ZoomFactor += delta > 0 ? 0.1f : -0.1f;
                ZoomFactor = Math.Max(ZoomFactor, 0.1f);
            }
            catch (Exception ex)
            {
                LogError("Error handling Adjust Zoom Factor ", ex);
                MessageBox.Show($"Error adjusting zoom factor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePictureBoxSize(PictureBox pictureBox)
        {
            try
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Width = (int)(pictureBox.Image.Width * ZoomFactor);
                    pictureBox.Height = (int)(pictureBox.Image.Height * ZoomFactor);
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    MessageBox.Show("تصویر بارگذاری نشده است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling Update PictureBox Size ", ex);
                MessageBox.Show($"Error updating picture box size: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                LoadFileModels();
            }
            catch (Exception ex)
            {
                LogError("Error handling Main Load ", ex);
                MessageBox.Show($"Error loading main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFileModels()
        {
            try
            {
                SettingsModel = SerializationHelper.LoadSettings();
            }
            catch (Exception ex)
            {
                LogError("Error handling Load File Models ", ex);
                MessageBox.Show($"Error loading file models: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnselect_Click(object sender, EventArgs e)
        {
            try
            {
                ResetImageSetting();
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PDF And Image|*.pdf;*.jpg;*.jpeg;*.png;*.bmp";
                    openFileDialog.Title = "Select a PDF or Image File";
                    openFileDialog.Multiselect = true;
                    openFileDialog.CheckFileExists = true;
                    openFileDialog.CheckPathExists = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        DisplaySelectedFiles(openFileDialog.FileNames);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling btn select ", ex);
                MessageBox.Show($"Error selecting files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplaySelectedFiles(string[] fileNames)
        {
            try
            {
                FlowLayoutPanel? flowLayoutPanel = CreateFlowLayoutPanel();
                if (flowLayoutPanel == null)
                {
                    LogError("Error handling Display Selected Files : flowLayoutPanel is null ");
                    return;
                }

                ImagePath.AddRange(fileNames);
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

                Parallel.ForEach(fileNames, parallelOptions, item =>
                {
                    string extension = Path.GetExtension(item).ToLower();
                    if (extension == ".pdf")
                    {
                        DisplayPdf(item, flowLayoutPanel);
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                    {
                        DisplayImage(item, flowLayoutPanel);
                    }
                    else
                    {
                        MessageBox.Show("فایل انتخابی پشتیبانی نمیشود دوباره تلاش کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });

                flowLayoutPanel.Visible = true;
                panel1.Controls.Add(flowLayoutPanel);
            }
            catch (Exception ex)
            {
                LogError("Error handling Display Selected Files ", ex);
                MessageBox.Show($"Error displaying selected files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private FlowLayoutPanel? CreateFlowLayoutPanel()
        {
            try
            {
                return new FlowLayoutPanel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(0, 0),
                    Size = new Size(574, 420),
                    AutoScroll = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
            }
            catch (Exception ex)
            {
                LogError("Error handling Create FlowLayout Panel ", ex);
                MessageBox.Show($"Error creating flow layout panel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private async Task<bool> ConvertToDicomAndSendAsync(string filePath, PatientModel patientModel)
        {
            try
            {
                (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null;

                if (IsServerSettingsValid())
                {
                    additionalTags = GetCachedOrExecuteSelectQuery(SettingsModel, patientModel.PatientID);
                }

                if (filePath != null && File.Exists(filePath))
                {
                    using (Bitmap bitmapimg = new Bitmap(filePath))
                    {
                        // Updated call to use DicomConversionHelper
                        var dicomFile = DicomConversionHelper.ConvertImageToDicom(
                            bitmapimg,
                            patientModel,
                            SettingsModel.ServerModality.ToString(), // Pass modality
                            additionalTags);

                        if (dicomFile != null)
                        {
                            await dicomFile.SaveAsync(Path.GetFileNameWithoutExtension(filePath) + ".dcm");
                            return await SendDicomFileToServerAsync(dicomFile);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("فایل پیدا نشد", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError("Error handling Convert To Dicom And Send Async ", ex);
                MessageBox.Show($"Error converting to DICOM and sending: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool IsServerSettingsValid()
        {
            try
            {
                return !string.IsNullOrEmpty(SettingsModel.ServerAddress) &&
                       !string.IsNullOrEmpty(SettingsModel.Instance) &&
                       !string.IsNullOrEmpty(SettingsModel.username) &&
                       !string.IsNullOrEmpty(SettingsModel.password);
            }
            catch (Exception ex)
            {
                LogError("Error handling Is Server Settings Valid ", ex);
                MessageBox.Show($"Error validating server settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> SendDicomFileToServerAsync(DicomFile dicomFile)
        {
            try
            {
                if (SettingsModel != null && SettingsModel.ServerPort > 0 && Serverhelper.IsValidIP(SettingsModel.ServerAddress))
                {
                    if (Serverhelper.IsServerReachable(SettingsModel))
                    {
                        var client = DicomClientFactory.Create(SettingsModel.ServerAddress, SettingsModel.ServerPort, SettingsModel.ServerUseTls, SettingsModel.ServerTitle, SettingsModel.ServerAET);
                        await client.AddRequestAsync(new DicomCStoreRequest(dicomFile));
                        await client.SendAsync();
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("سرور PACS در دسترس نیست", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("شما اول باید تنظیمات پکس را ست کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError("Error handling Send Dicom File To Server Async ", ex);
                MessageBox.Show($"Error sending DICOM file to server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private (string StudyInsUID, string SOPClassUID, string name) ExecuteSelectQuery(SettingsModel settings, string pid)
        {
            ISqlService sqlService; // Changed from SQLCLASS to ISqlService
            try
            {
                sqlService = new SQLCLASS(settings); // Instantiation remains the same, but assigned to interface type
            }
            catch (ArgumentNullException ex) // Catching null settings
            {
                LogError("Error initializing SQLCLASS: Settings cannot be null.", ex);
                MessageBox.Show($"Error initializing database connection: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
            catch (InvalidOperationException ex) // Catching missing/invalid settings
            {
                LogError("Error initializing SQLCLASS: Invalid settings provided.", ex);
                MessageBox.Show($"Error initializing database connection: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex) // Catch any other unexpected errors during SQLCLASS instantiation
            {
                LogError("Unexpected error initializing SQLCLASS.", ex);
                MessageBox.Show($"An unexpected error occurred while setting up the database connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }

            try
            {
                DataTable resultTable = sqlService.ExecuteSelectQuery(pid, settings.ServerModality.ToString()); // Changed to sqlService

                if (resultTable.Rows.Count > 0)
                {
                    DataRow row = resultTable.Rows[0];
                    string sopClassUID = row["SOPClassUID"]?.ToString() ?? string.Empty;
                    string studyInsUID = row["StudyInsUID"]?.ToString() ?? string.Empty;
                    string PName = row["PName"]?.ToString() ?? string.Empty;
                    return (studyInsUID, sopClassUID, PName);
                }
                else
                {
                    return (string.Empty, string.Empty, string.Empty);
                }
            }
            catch (SqlCustomException ex) // Specific exception from SQLCLASS
            {
                LogError("Error executing SQL select query (SqlCustomException).", ex);
                MessageBox.Show($"Database query error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex) // General exceptions during query execution
            {
                LogError("Unexpected error executing select query.", ex);
                MessageBox.Show($"An unexpected error occurred during data retrieval: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
        }

        // AddDicomTags method removed - now in DicomConversionHelper
        // ConvertImageToDicom method removed - now in DicomConversionHelper
        // GetBitmapPixels method (later in file) will also be removed.

        private void DisplayImage(string filePath, FlowLayoutPanel panel)
        {
            try
            {
                // Load a new Bitmap for each image. Do not use class-level Img field here.
                using (Image sourceImage = Image.FromFile(filePath)) // Load image from file
                {
                    // CreatePictureBox will make its own copy of the image for the PictureBox
                    PictureBox? pictureBox = CreatePictureBox(sourceImage);
                    if (pictureBox != null)
                    {
                        panel.Controls.Add(pictureBox);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling Display Image ", ex);
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PictureBox? CreatePictureBox(Image image)
        {
            try
            {
                var pictureBox = new PictureBox
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Size = new Size(574, 454), // Default size, will be adjusted by zoom
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Visible = true,
                    // Create a new Bitmap for the PictureBox to ensure it has its own copy,
                    // especially if the source 'image' might be disposed by the caller.
                    Image = new Bitmap(image)
                };
                pictureBox.MouseWheel += pic1_MouseWheel;
                return pictureBox;
            }
            catch (Exception ex)
            {
                LogError("Error handling Create PictureBox ", ex);
                MessageBox.Show($"Error creating picture box: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void DisplayPdf(string filePath, FlowLayoutPanel panel)
        {
            try
            {
                using (var document = PdfDocument.Load(filePath))
                {
                    var page = document.Pages[0]; // Access the first page
                    SizeF pageSize = page.Size;   // Get original page size in points

                    int targetBoxWidth = 574;  // Target width of the PictureBox from CreatePictureBox
                    int targetBoxHeight = 454; // Target height of the PictureBox from CreatePictureBox

                    // Calculate new dimensions maintaining aspect ratio
                    double ratioX = (double)targetBoxWidth / pageSize.Width;
                    double ratioY = (double)targetBoxHeight / pageSize.Height;
                    double ratio = Math.Min(ratioX, ratioY); // Use the smaller ratio to fit entirely

                    int newWidth = (int)(pageSize.Width * ratio);
                    int newHeight = (int)(pageSize.Height * ratio);

                    // Ensure minimum dimensions if calculation results in zero or very small numbers
                    newWidth = Math.Max(newWidth, 10);
                    newHeight = Math.Max(newHeight, 10);

                    // Render the page with the new dimensions.
                    // PdfiumViewer's Render method takes pixel dimensions.
                    // The boolean flag (last parameter) is for 'forPrinting'. False for screen.
                    using (var renderedImage = document.Render(0, newWidth, newHeight, false))
                    {
                        // CreatePictureBox will make its own copy
                        PictureBox? pictureBox = CreatePictureBox(renderedImage);
                        if (pictureBox != null)
                        {
                             panel.Controls.Add(pictureBox);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling Display Pdf ", ex);
                MessageBox.Show($"Error displaying PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtpatientId.Text.Trim()))
                {
                    MessageBox.Show("شماره مراجعه بیمار نمیتواند خالی باشد", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (ImagePath == null || ImagePath.Count == 0)
                {
                    MessageBox.Show("شما باید فایل را انتخاب کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btn.Enabled = false;

                PatientModel = new PatientModel
                {
                    PatientName = txtpatientfamily.Text.Trim(),
                    PatientID = txtpatientId.Text.Trim()
                };
                // پردازش فایل‌ها به صورت موازی
                var tasks = ImagePath
                    .Where(item => !string.IsNullOrEmpty(item) && File.Exists(item)) // فیلتر فایل‌های معتبر
                    .Select(async item =>
                    {
                        try
                        {
                            // تبدیل و ارسال فایل به DICOM
                            bool success = await ConvertToDicomAndSendAsync(item, PatientModel);
                            if (success)
                            {
                                MessageBox.Show($"فایل {Path.GetFileNameWithoutExtension(item)} تبدیل و ارسال شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show($"خطا در تبدیل یا ارسال فایل {Path.GetFileNameWithoutExtension(item)} به سرور PACS", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error processing file {item}", ex);
                        }
                    });

                // منتظر ماندن برای اتمام تمام وظایف
                await Task.WhenAll(tasks);

                //foreach (var item in ImagePath)
                //{
                //    if (!string.IsNullOrEmpty(item) && File.Exists(item))
                //    {
                //        if (await ConvertToDicomAndSendAsync(item, PatientModel))
                //        {
                //            MessageBox.Show($"فایل {Path.GetFileNameWithoutExtension(item)} تبدیل و ارسال شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //        }
                //        else
                //        {
                //            MessageBox.Show("خطا در تبدیل یا ارسال فایل به سرور PACS", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //        }
                //    }
                //    else
                //    {
                //        MessageBox.Show("شما باید فایل را انتخاب کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    }
                //}
                ResetImageSetting();
                // Img field is no longer primarily populated by DisplayImage/DisplayPdf
                // It might still be used by pic1_MouseWheel if pic1 is the main PictureBox,
                // but dynamically created PictureBoxes now correctly use their own images.
                // The Img field should be reviewed for its role if pic1 (the main form PictureBox) is also used.
                // For now, setting it to null in ResetImageSetting is consistent.
                Img = null;
            }
            catch (Exception ex)
            {
                LogError("Error handling btn Click ", ex);
                MessageBox.Show($"Error handling button click: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        private void ResetImageSetting()
        {
            try
            {
                // Disposing images held by PictureBoxes in panel1 before clearing
                foreach (Control ctrl in panel1.Controls)
                {
                    if (ctrl is FlowLayoutPanel flowPanel)
                    {
                        foreach(Control pbCtrl in flowPanel.Controls)
                        {
                            if (pbCtrl is PictureBox pb)
                            {
                                pb.Image?.Dispose(); // Dispose the image
                            }
                        }
                        flowPanel.Controls.Clear(); // Clear controls from FlowLayoutPanel
                    }
                }
                panel1.Controls.Clear(); // Clear the FlowLayoutPanel itself from panel1
                
                cachedPatientID = null;
                // Img = null; // Already handled by the btn_Click finally block logic if needed.
                // Or, if Img is specifically for a main PictureBox (not in FlowLayoutPanel), manage its lifecycle separately.
                // For now, let's ensure it's nulled if it was tied to the cleared content.
                Img?.Dispose(); // Dispose if it holds an image
                Img = null;

                cachedTags = null;

                if (ImagePath != null && ImagePath.Count > 0)
                {
                    foreach (var item in ImagePath)
                    {
                        string dicomFilePath = Path.GetFileNameWithoutExtension(item) + ".dcm";
                        if (File.Exists(dicomFilePath))
                        {
                            File.Delete(dicomFilePath);
                        }
                    }
                    ImagePath.Clear();
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling Reset Image Setting ", ex);
                MessageBox.Show($"Error resetting image settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (Setting setting = new Setting())
                {
                    setting.ShowDialog();
                    LoadFileModels();
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling Settings ToolStripMenuItem ", ex);
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                LogError("Error handling exit ToolStripMenuItem ", ex);
                MessageBox.Show($"Error exiting application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // GetBitmapPixels method removed - now in DicomConversionHelper

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (About about = new About())
                {
                    about.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogError("Error handling about ToolStripMenuItem ", ex);
                MessageBox.Show($"Error opening about dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private (string StudyInsUID, string SOPClassUID, string PName) GetCachedOrExecuteSelectQuery(SettingsModel settings, string patientID)
        {
            // اگر PatientID تغییر نکرده باشد، مقدار کش شده را بازگردان
            if (cachedPatientID == patientID && cachedTags.HasValue)
            {
                return cachedTags.Value;
            }

            // اگر PatientID تغییر کرده باشد، مقدار جدید را از دیتابیس بگیر و کش کن
            cachedTags = ExecuteSelectQuery(settings, patientID);
            cachedPatientID = patientID;

            return cachedTags.Value;
        }

        private void btnreset_Click(object sender, EventArgs e)
        {
            btn.Enabled = true;
            txtpatientfamily.Text = string.Empty;
            txtpatientId.Text = string.Empty;
            ResetImageSetting();
        }
    }
}
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
﻿using System.Collections.Concurrent; // For ConcurrentBag

// ... other using statements ...

namespace Convert_to_dcm
{
    public partial class Main : System.Windows.Forms.Form
    {
        private SettingsModel SettingsModel { get; set; } = new SettingsModel();
        private PatientModel PatientModel { get; set; }
        private List<string> ImagePath = new List<string>(); // Stores paths of images currently displayed
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
                // pictureBox.Image will be stretched by StretchImage.
                // We need the original image dimensions to calculate the new panel size correctly.
                if (pictureBox.Tag is Size originalImageSize)
                {
                    // Calculate new dimensions for the container panel
                    int newWidth = (int)(originalImageSize.Width * this.ZoomFactor);
                    int newHeight = (int)(originalImageSize.Height * this.ZoomFactor);

                    // Ensure minimum dimensions
                    newWidth = Math.Max(newWidth, 50); // Example minimum width
                    newHeight = Math.Max(newHeight, 50); // Example minimum height

                    if (pictureBox.Parent is Panel containerPanel)
                    {
                        // Resize the container panel. The PictureBox inside will stretch its image.
                        containerPanel.Size = new Size(newWidth, newHeight);
                    }
                    // No need to set pictureBox.SizeMode here as it's StretchImage
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

                ImagePath.AddRange(fileNames); // Keep track of all image paths being processed

                // Use a concurrent bag to collect panels created by worker threads
                var itemPanels = new ConcurrentBag<Panel>();
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

                Parallel.ForEach(fileNames, parallelOptions, filePath =>
                {
                    Panel? itemPanel = null;
                    string extension = Path.GetExtension(filePath).ToLower();
                    if (extension == ".pdf")
                    {
                        itemPanel = DisplayPdf(filePath); // DisplayPdf now returns a Panel
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                    {
                        itemPanel = DisplayImage(filePath); // DisplayImage now returns a Panel
                    }
                    else
                    {
                        // Handle unsupported file types (perhaps log or show message later on UI thread)
                        // For now, skipping MessageBox from non-UI thread.
                    }
                    if (itemPanel != null)
                    {
                        itemPanels.Add(itemPanel);
                    }
                });

                // Add collected panels to the FlowLayoutPanel on the UI thread
                if (flowLayoutPanel.InvokeRequired)
                {
                    flowLayoutPanel.Invoke((MethodInvoker)delegate {
                        foreach (var panelToAdd in itemPanels)
                        {
                            flowLayoutPanel.Controls.Add(panelToAdd);
                        }
                    });
                }
                else
                {
                    foreach (var panelToAdd in itemPanels)
                    {
                        flowLayoutPanel.Controls.Add(panelToAdd);
                    }
                }

                flowLayoutPanel.Visible = true;
                panel1.Controls.Add(flowLayoutPanel); // Add the FlowLayoutPanel to the main panel
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
                        // DicomClient implements IAsyncDisposable and IDisposable
                        await using (var client = DicomClientFactory.Create(SettingsModel.ServerAddress, SettingsModel.ServerPort, SettingsModel.ServerUseTls, SettingsModel.ServerTitle, SettingsModel.ServerAET))
                        {
                            await client.AddRequestAsync(new DicomCStoreRequest(dicomFile));
                            await client.SendAsync();
                            // Note: Some DicomClient implementations might require an explicit association release
                            // or have specific cleanup, but SendAsync should handle the main operation.
                            // The using block will ensure DisposeAsync() or Dispose() is called.
                            return true;
                        }
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

        private Panel? CreateFlowLayoutItem(Image previewImage, string imagePath)
        {
            try
            {
                Panel itemPanel = new Panel
                {
                    // Initial size of the container panel should be based on ZoomFactor=1.0
                    // Or, a fixed thumbnail size if that's preferred for initial display,
                    // and then zooming adjusts from there.
                    // The current CreateFlowLayoutItem uses a fixed size (150,180),
                    // so zooming will make these panels larger or smaller.
                    Size = new Size((int)(previewImage.Width * ZoomFactor), (int)(previewImage.Height * ZoomFactor) + 25), // +25 for button
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle
                };
                // Ensure the panel size respects some minimums if image is too small
                itemPanel.Width = Math.Max(itemPanel.Width, 80); // Min width for panel
                itemPanel.Height = Math.Max(itemPanel.Height, 80 + 25); // Min height for panel + button


                PictureBox pictureBox = new PictureBox
                {
                    Image = new Bitmap(previewImage),
                    SizeMode = PictureBoxSizeMode.StretchImage, // Changed for smooth zoom
                    Dock = DockStyle.Fill,
                    Tag = previewImage.Size // Store original image size for zoom calculations
                };
                pictureBox.MouseWheel += pic1_MouseWheel;

                Button removeButton = new Button
                {
                    Text = "X",
                    Tag = imagePath, // Store imagePath to identify which image to remove
                    Dock = DockStyle.Bottom,
                    Height = 25
                };
                removeButton.Click += RemoveButton_Click;

                itemPanel.Controls.Add(pictureBox);
                itemPanel.Controls.Add(removeButton);

                return itemPanel;
            }
            catch (Exception ex)
            {
                LogError($"Error creating flow layout item for {imagePath}", ex);
                return null;
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (sender is Button clickedButton && clickedButton.Tag is string imagePath)
                {
                    // Remove from the main list of paths
                    ImagePath.Remove(imagePath);

                    // Find the parent Panel of the button
                    if (clickedButton.Parent is Panel itemPanel)
                    {
                        // Dispose controls within the itemPanel
                        foreach (Control ctrl in itemPanel.Controls)
                        {
                            if (ctrl is PictureBox pb)
                            {
                                pb.Image?.Dispose();
                            }
                            ctrl.Dispose();
                        }
                        itemPanel.Controls.Clear(); // Not strictly necessary if disposing panel

                        // Remove itemPanel from its parent FlowLayoutPanel
                        if (itemPanel.Parent is FlowLayoutPanel flowPanel)
                        {
                            flowPanel.Controls.Remove(itemPanel);
                        }
                        itemPanel.Dispose(); // Dispose the panel itself
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error removing image from layout", ex);
                MessageBox.Show($"Error removing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private Panel? DisplayImage(string filePath) // Now returns Panel, no FlowLayoutPanel param
        {
            try
            {
                using (Image sourceImage = Image.FromFile(filePath))
                {
                    // CreateFlowLayoutItem will make its own copy
                    return CreateFlowLayoutItem(sourceImage, filePath);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error displaying image {filePath}", ex);
                // Potentially show error for this specific image, or collect errors
                return null;
            }
        }

        // CreatePictureBox method is now effectively replaced by CreateFlowLayoutItem's PictureBox creation part
        // If CreatePictureBox was used elsewhere, that needs to be handled. For now, assuming it was only for DisplayImage/Pdf.

        private Panel? DisplayPdf(string filePath) // Now returns Panel, no FlowLayoutPanel param
        {
            try
            {
                using (var document = PdfDocument.Load(filePath))
                {
                    var page = document.Pages[0];
                    SizeF pageSize = page.Size;

                    // Render PDF to a size suitable for initial thumbnail display,
                    // matching the logic in CreateFlowLayoutItem's initial sizing if possible,
                    // or just a reasonable default thumbnail size.
                    // Let's aim for a width of around 150 for the image itself, button is extra.
                    int targetImageWidth = 150;
                    int targetImageHeight = (int)(pageSize.Height * (double)targetImageWidth / pageSize.Width);
                    targetImageHeight = Math.Max(targetImageHeight, 100); // Min image height
                    targetImageWidth = Math.Max(targetImageWidth, 100); // Min image width


                    using (var renderedImage = document.Render(0, targetImageWidth, targetImageHeight, false))
                    {
                        // CreateFlowLayoutItem will make its own copy
                        return CreateFlowLayoutItem(renderedImage, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error displaying PDF {filePath}", ex);
                // Potentially show error for this specific PDF
                return null;
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
                        foreach(Control itemCtrl in flowPanel.Controls)
                        {
                            if (itemCtrl is Panel itemPanel) // Each item is now a Panel
                            {
                                foreach(Control innerCtrl in itemPanel.Controls)
                                {
                                    if (innerCtrl is PictureBox pb)
                                    {
                                        pb.Image?.Dispose();
                                    }
                                    innerCtrl.Dispose(); // Dispose button, picturebox
                                }
                                itemPanel.Controls.Clear();
                            }
                            itemCtrl.Dispose(); // Dispose the itemPanel itself
                        }
                        flowPanel.Controls.Clear();
                        flowPanel.Dispose(); // Dispose the FlowLayoutPanel itself
                    }
                    else if (ctrl is PictureBox directPb) // Handle older direct PictureBox if any (though unlikely now)
                    {
                         directPb.Image?.Dispose();
                         directPb.Dispose();
                    }
                }
                panel1.Controls.Clear();
                
                cachedPatientID = null;
                Img?.Dispose();
                Img = null;
                cachedTags = null;

                // ImagePath list is now the source of truth for displayed images.
                // It's cleared here. When ResetImageSetting is called before adding new images,
                // this ensures a fresh start.
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
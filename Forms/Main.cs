using Convert_to_dcm.Helper;
using Convert_to_dcm.Sql;
using Convert_to_dcom.Class;
using Convert_to_dcom.Class.Helper;
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
        private readonly ErrHelper _errHelper = ErrHelper.Instance;

        public Main()
        {
            try
            {
                InitializeComponent();
                PatientModel = new PatientModel
                {
                    PatientID = string.Empty,
                    PatientName = string.Empty,
                    PatientBirthDate = string.Empty,
                    PatientSex = string.Empty,
                    PatientAge = string.Empty,
                    PatientDoc = string.Empty
                };
            }
            catch (Exception ex)
            {
                ErrHelper.Instance.LogError("Error handling Main constructor ", ex).Wait();
                MessageBox.Show($"Error initializing Main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void pic1_MouseWheel(object? sender, MouseEventArgs e)
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
                await ErrHelper.Instance.LogError("Error handling mouse wheel event ", ex);
                MessageBox.Show($"Error handling mouse wheel event: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void AdjustZoomFactor(int delta)
        {
            try
            {
                ZoomFactor += delta > 0 ? 0.1f : -0.1f;
                ZoomFactor = Math.Max(ZoomFactor, 0.1f);
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Adjust Zoom Factor ", ex);
                MessageBox.Show($"Error adjusting zoom factor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void UpdatePictureBoxSize(PictureBox pictureBox)
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
                await ErrHelper.Instance.LogError("Error handling Update PictureBox Size ", ex);
                MessageBox.Show($"Error updating picture box size: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            try
            {
                LoadFileModels();
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Main Load ", ex);
                MessageBox.Show($"Error loading main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadFileModels()
        {
            try
            {
                SettingsModel = await SerializationHelper.LoadSettings();
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Load File Models ", ex);
                MessageBox.Show($"Error loading file models: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnselect_Click(object sender, EventArgs e)
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
                await ErrHelper.Instance.LogError("Error handling btn select ", ex);
                MessageBox.Show($"Error selecting files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DisplaySelectedFiles(string[] fileNames)
        {
            try
            {
                FlowLayoutPanel? flowLayoutPanel = CreateFlowLayoutPanel();
                if (flowLayoutPanel == null)
                {
                    await ErrHelper.Instance.LogError("Error handling Display Selected Files : flowLayoutPanel is null ");
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
                await ErrHelper.Instance.LogError("Error handling Display Selected Files ", ex);
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
                ErrHelper.Instance.LogError("Error handling Create FlowLayout Panel ", ex).Wait();
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
                        var dicomFile = ConvertImageToDicom(bitmapimg, patientModel, additionalTags);

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
                await ErrHelper.Instance.LogError("Error handling Convert To Dicom And Send Async ", ex);
                MessageBox.Show($"Error converting to DICOM and sending: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private  bool IsServerSettingsValid()
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
                ErrHelper.Instance.LogError("Error handling Is Server Settings Valid ", ex).Wait();
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
                await ErrHelper.Instance.LogError("Error handling Send Dicom File To Server Async ", ex);
                MessageBox.Show($"Error sending DICOM file to server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private (string StudyInsUID, string SOPClassUID, string name) ExecuteSelectQuery(SettingsModel settings, string pid)
        {
            try
            {
                SQLCLASS sqlClass = new SQLCLASS(settings);
                DataTable resultTable = sqlClass.ExecuteSelectQuery(pid, settings.ServerModality.ToString());

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
            catch (Exception ex)
            {
                ErrHelper.Instance.LogError("Error handling Execute Select Query ", ex).Wait();
                MessageBox.Show($"Error executing select query: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
        }

        private async void AddDicomTags(DicomDataset dicomDataset, int width, int height, string photometricInterpretation, ushort samplesPerPixel, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null)
        {
            try
            {
                dicomDataset.Add(DicomTag.PatientName, !string.IsNullOrEmpty(additionalTags?.PName.Trim()) ? additionalTags?.PName : patientModel.PatientName);
                dicomDataset.Add(DicomTag.PatientID, patientModel.PatientID);
                dicomDataset.Add(DicomTag.StudyInstanceUID, !string.IsNullOrEmpty(additionalTags?.StudyInsUID.Trim()) ? additionalTags?.StudyInsUID : DicomUID.Generate().UID);
                dicomDataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate().UID);
                dicomDataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
                dicomDataset.Add(DicomTag.SOPClassUID, !string.IsNullOrEmpty(additionalTags?.SOPClassUID.Trim()) ? additionalTags?.SOPClassUID : DicomUID.SecondaryCaptureImageStorage.UID);
                dicomDataset.Add(DicomTag.PhotometricInterpretation, photometricInterpretation);
                dicomDataset.Add(DicomTag.TransferSyntaxUID, DicomUID.ExplicitVRLittleEndian);
                dicomDataset.Add(DicomTag.Rows, (ushort)height);
                dicomDataset.Add(DicomTag.Columns, (ushort)width);
                dicomDataset.Add(DicomTag.BitsAllocated, (ushort)8);
                dicomDataset.Add(DicomTag.BitsStored, (ushort)8);
                dicomDataset.Add(DicomTag.HighBit, (ushort)7);
                dicomDataset.Add(DicomTag.PixelRepresentation, (ushort)0);
                dicomDataset.Add(DicomTag.Modality, SettingsModel.ServerModality);
                dicomDataset.Add(DicomTag.SamplesPerPixel, samplesPerPixel);

                string currentTime = DateTime.Now.ToString("HHmmss");
                dicomDataset.Add(DicomTag.StudyTime, currentTime);
                dicomDataset.Add(DicomTag.SeriesTime, currentTime);
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Add Dicom Tags ", ex);
                MessageBox.Show($"Error adding DICOM tags: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DicomFile? ConvertImageToDicom(Bitmap bitmap, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags)
        {
            try
            {
                var dicomFile = new DicomFile();
                var dicomDataset = dicomFile.Dataset;
                dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

                AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, additionalTags);

                // فراخوانی GetBitmapPixels برای استخراج داده‌های دقیق پیکسل‌ها
                byte[] pixelDataArray = GetBitmapPixels(bitmap);

                // اضافه کردن داده‌های پیکسل به فایل DICOM
                DicomPixelData pixelData = DicomPixelData.Create(dicomDataset, true);
                pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
                pixelData.AddFrame(new MemoryByteBuffer(pixelDataArray));

                return dicomFile;
            }
            catch (Exception ex)
            {
                ErrHelper.Instance.LogError("Error converting image to DICOM", ex).Wait();
                MessageBox.Show($"Error converting image to DICOM: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }


        private async void DisplayImage(string filePath, FlowLayoutPanel panel)
        {
            try
            {
                if (Img == null)
                {
                    Img = new Bitmap(Image.FromFile(filePath));
                }
                PictureBox? pictureBox = CreatePictureBox(Img);
                panel.Controls.Add(pictureBox);
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Display Image ", ex);
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
                    Size = new Size(574, 454),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Visible = true,
                    Image = image
                };
                pictureBox.MouseWheel += pic1_MouseWheel;
                return pictureBox;
            }
            catch (Exception ex)
            {
                ErrHelper.Instance.LogError("Error handling Create PictureBox ", ex).Wait();
                MessageBox.Show($"Error creating picture box: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private async void DisplayPdf(string filePath, FlowLayoutPanel panel,int dpi=1200)
        {
            try
            {
                using (var document = PdfDocument.Load(filePath))
                {
                    if (document == null)
                    {
                        MessageBox.Show("خطا در بارگذاری PDF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    int pages = document.PageCount;

                    for (int i = 0; i < pages; i++)
                    {
                        using (var image = document.Render(i, dpi, dpi, true)) // استفاده از DPI بالا
                        {
                            PictureBox? pictureBox = CreatePictureBox(image);
                            if (pictureBox != null)
                            {
                                panel.Controls.Add(pictureBox);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Display Pdf ", ex);
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
                            await ErrHelper.Instance.LogError($"Error processing file {item}", ex);
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

            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling btn Click ", ex);
                MessageBox.Show($"Error handling button click: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        private async void ResetImageSetting()
        {
            try
            {
                panel1.Controls.Clear();

                cachedPatientID = null;
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
                await ErrHelper.Instance.LogError("Error handling Reset Image Setting ", ex);
                MessageBox.Show($"Error resetting image settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
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
                await ErrHelper.Instance.LogError("Error handling Settings ToolStripMenuItem ", ex);
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling exit ToolStripMenuItem ", ex);
                MessageBox.Show($"Error exiting application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private byte[] GetBitmapPixels(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            int stride = bmpData.Stride;
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerPixel = 3; // برای تصاویر RGB

            byte[] rawData = new byte[stride * height];
            Marshal.Copy(bmpData.Scan0, rawData, 0, rawData.Length);
            bitmap.UnlockBits(bmpData);

            byte[] result = new byte[width * height * bytesPerPixel];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bmpIndex = y * stride + x * bytesPerPixel;
                    int dcmIndex = (y * width + x) * bytesPerPixel;

                    // RGB به BGR تبدیل رنگ‌ها
                    result[dcmIndex] = rawData[bmpIndex + 2];     // R
                    result[dcmIndex + 1] = rawData[bmpIndex + 1]; // G
                    result[dcmIndex + 2] = rawData[bmpIndex];     // B
                }
            }

            return result;
        }


        private async void aboutToolStripMenuItem_Click(object sender, EventArgs e)
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
                await ErrHelper.Instance.LogError("Error handling about ToolStripMenuItem ", ex);
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
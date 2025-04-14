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
        private string ImagePath { get; set; } = string.Empty;
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
                MessageBox.Show($"Error selecting files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplaySelectedFiles(string[] fileNames)
        {
            try
            {
                FlowLayoutPanel flowLayoutPanel = CreateFlowLayoutPanel();

                foreach (var item in fileNames)
                {
                    ImagePath = item;
                    string extension = Path.GetExtension(ImagePath).ToLower();

                    if (extension == ".pdf")
                    {
                        DisplayPdf(ImagePath, flowLayoutPanel);
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                    {
                        DisplayImage(ImagePath, flowLayoutPanel);
                    }
                    else
                    {
                        MessageBox.Show("فایل انتخابی پشتیبانی نمیشود دوباره تلاش کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                flowLayoutPanel.Visible = true;
                panel1.Controls.Add(flowLayoutPanel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying selected files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private FlowLayoutPanel CreateFlowLayoutPanel()
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
                    additionalTags = ExecuteSelectQuery(SettingsModel, patientModel.PatientID);
                }

                if (Img != null)
                {
                    var dicomFile = ConvertImageToDicom(Img, patientModel, additionalTags);

                    if (dicomFile != null)
                    {
                        dicomFile.Save("output.dcm");
                        if (await SendDicomFileToServerAsync(dicomFile))
                        {
                            return true;
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
                MessageBox.Show($"Error executing select query: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (string.Empty, string.Empty, string.Empty);
            }
        }

        private void AddDicomTags(DicomDataset dicomDataset, int width, int height, string photometricInterpretation, ushort samplesPerPixel, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null)
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
                MessageBox.Show($"Error adding DICOM tags: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DicomFile ConvertImageToDicom(Bitmap bitmap, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags)
        {
            try
            {
                var dicomFile = new DicomFile();
                var dicomDataset = dicomFile.Dataset;
                dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
                AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, additionalTags);
                int bytesPerPixel = 3;
                byte[] pixelDataArray = new byte[bitmap.Width * bitmap.Height * bytesPerPixel];

                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                Marshal.Copy(bmpData.Scan0, pixelDataArray, 0, pixelDataArray.Length);
                bitmap.UnlockBits(bmpData);

                for (int i = 0; i < pixelDataArray.Length; i += 3)
                {
                    byte temp = pixelDataArray[i];
                    pixelDataArray[i] = pixelDataArray[i + 2];
                    pixelDataArray[i + 2] = temp;
                }

                DicomPixelData pixelData = DicomPixelData.Create(dicomDataset, true);
                pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
                pixelData.AddFrame(new MemoryByteBuffer(pixelDataArray));

                return dicomFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting image to DICOM: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void DisplayImage(string filePath, FlowLayoutPanel panel)
        {
            try
            {
                Img = new Bitmap(Image.FromFile(filePath));
                PictureBox pictureBox = CreatePictureBox(Img);
                panel.Controls.Add(pictureBox);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PictureBox CreatePictureBox(Image image)
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
                    var image = document.Render(0, 9000, 9000, false);
                    PictureBox pictureBox = CreatePictureBox(image);
                    panel.Controls.Add(pictureBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
                {
                    if (!string.IsNullOrEmpty(txtpatientId.Text.Trim()))
                    {
                        PatientModel = new PatientModel
                        {
                            PatientName = txtpatientfamily.Text.Trim(),
                            PatientID = txtpatientId.Text.Trim()
                        };

                        if (await ConvertToDicomAndSendAsync(ImagePath, PatientModel))
                        {
                            MessageBox.Show("فایل تبدیل و ارسال شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ResetImageSetting();
                        }
                        else
                        {
                            MessageBox.Show("خطا در تبدیل یا ارسال فایل به سرور PACS", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("شماره مراجعه بیمار نمیتواند خالی باشد", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("شما اول باید فایل را انتخاب کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling button click: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetImageSetting()
        {
            try
            {
                panel1.Controls.Clear();
                Img = null;
                ImagePath = string.Empty;
            }
            catch (Exception ex)
            {
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
                MessageBox.Show($"Error exiting application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
                MessageBox.Show($"Error opening about dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

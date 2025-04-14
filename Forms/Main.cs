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
        private SettingsModel settingsModel = new SettingsModel(); // لیست مدل‌های فایل
        PatientModel patientModel; // مدل بیمار
        private string imagePath = string.Empty; // مسیر تصویر
        private float zoomFactor = 1.0f; // Add a zoom factor


        public Main()
        {
            InitializeComponent();
            patientModel = new PatientModel(); // Initialize patientModel in the constructor
        }

        private void pic1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (sender is PictureBox)
            {
                if ((sender as PictureBox).Image == null)
                {
                    return; // Exit if there is no image to zoom
                }

                if (e.Delta > 0)
                {
                    zoomFactor += 0.1f; // Zoom in
                }
                else if (e.Delta < 0)
                {
                    zoomFactor -= 0.1f; // Zoom out
                }

                if (zoomFactor < 0.1f)
                {
                    zoomFactor = 0.1f; // Prevent zooming out too much
                }

                (sender as PictureBox).Width = (int)((sender as PictureBox).Image.Width * zoomFactor);
                (sender as PictureBox).Height = (int)((sender as PictureBox).Image.Height * zoomFactor);
                (sender as PictureBox).SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            LoadFileModels(); // Load the file models
        }

        // متد بارگذاری مدل‌های فایل
        private void LoadFileModels()
        {
            settingsModel = SerializationHelper.LoadSettings();
        }

        private void btnselect_Click(object sender, EventArgs e)
        {
            reset_image_setting();
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PDF And Image|*.pdf;*.jpg;*.jpeg;*.png;*.bmp";
                    openFileDialog.Title = "Select a PDF or Image File";
                    openFileDialog.Multiselect = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
                        flowLayoutPanel.BorderStyle = BorderStyle.FixedSingle;
                        flowLayoutPanel.Location = new Point(0, 0);
                        flowLayoutPanel.Size = new Size(574, 420);
                        flowLayoutPanel.AutoScroll = true;
                        flowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
                        flowLayoutPanel.WrapContents = true;
                        flowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                        foreach (var item in openFileDialog.FileNames)
                        {
                            imagePath = item;
                            string extension = Path.GetExtension(imagePath).ToLower();

                            if (extension == ".pdf")
                            {
                                DisplayPdf(imagePath, flowLayoutPanel);
                            }
                            else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                            {
                                DisplayImage(imagePath, flowLayoutPanel);
                            }
                            else
                            {
                                MessageBox.Show("فایل انتخابی پشتیبانی نمیشود دوباره تلاش کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        flowLayoutPanel.Visible = true;
                        panel1.Controls.Add(flowLayoutPanel);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task<bool> ConvertToDicomAndSendAsync(string filePath, PatientModel patientModel)
        {
            (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null;

            if (!string.IsNullOrEmpty(settingsModel.ServerAddress) &&
                !string.IsNullOrEmpty(settingsModel.Instance) &&
                !string.IsNullOrEmpty(settingsModel.username) &&
                !string.IsNullOrEmpty(settingsModel.password))
                additionalTags = ExecuteSelectQuery(settingsModel, patientModel.PatientID);

            if (img != null)
            {
                var dicomFile = ConvertImageToDicom(img, patientModel, additionalTags);

                if (dicomFile != null)
                {
                    dicomFile.Save("output.dcm");
                    if (settingsModel != null && settingsModel.ServerPort > 0 && Serverhelper.IsValidIP(settingsModel.ServerAddress))
                    {
                        if (Serverhelper.IsServerReachable(settingsModel))
                        {
                            // Send the DICOM file to PACS server
                            var client = DicomClientFactory.Create(settingsModel.ServerAddress, settingsModel.ServerPort, settingsModel.ServerUseTls, settingsModel.ServerTitle, settingsModel.ServerAET);
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
                }
            }
            else
            {
                MessageBox.Show("فایل پیدا نشد", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private (string StudyInsUID, string SOPClassUID, string name) ExecuteSelectQuery(SettingsModel settings, string pid)
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

        private void AddDicomTags(DicomDataset dicomDataset, int width, int height, string photometricInterpretation, ushort samplesPerPixel, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null)
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
            dicomDataset.Add(DicomTag.Modality, settingsModel.ServerModality);
            dicomDataset.Add(DicomTag.SamplesPerPixel, samplesPerPixel);

            string currentTime = DateTime.Now.ToString("HHmmss");
            dicomDataset.Add(DicomTag.StudyTime, currentTime);
            dicomDataset.Add(DicomTag.SeriesTime, currentTime);
        }

        private DicomFile ConvertImageToDicom(Bitmap bitmap, PatientModel patientModel, (string StudyInsUID, string SOPClassUID, string PName)? additionalTags)
        {
            // Create a DICOM file
            var dicomFile = new DicomFile();
            var dicomDataset = dicomFile.Dataset;
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            // Add necessary DICOM tags
            AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, additionalTags);
            int bytesPerPixel = 3; // RGB 
            byte[] pixelDataArray = new byte[bitmap.Width * bitmap.Height * bytesPerPixel];

            // Lock the bitmap's bits
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            // Copy the RGB values into the array
            Marshal.Copy(bmpData.Scan0, pixelDataArray, 0, pixelDataArray.Length);
            bitmap.UnlockBits(bmpData);

            // Swap the Red and Blue values to convert from BGR to RGB
            for (int i = 0; i < pixelDataArray.Length; i += 3)
            {
                byte temp = pixelDataArray[i];       // Blue
                pixelDataArray[i] = pixelDataArray[i + 2];   // Red
                pixelDataArray[i + 2] = temp;        // Blue to Red 
            }

            // Convert the image to byte array
            // Create DICOM pixel data and add the frame
            DicomPixelData pixelData = DicomPixelData.Create(dicomDataset, true);
            pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
            pixelData.AddFrame(new MemoryByteBuffer(pixelDataArray));

            return dicomFile;
        }

        Bitmap? img;
        private void DisplayImage(string filePath, FlowLayoutPanel panel)
        {
            try
            {
                img = new Bitmap(Image.FromFile(filePath));
                PictureBox pictureBox = new PictureBox()
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Size = new Size(574, 454),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Visible = true,
                    Image = img
                };
                pictureBox.MouseWheel += pic1_MouseWheel;
                panel.Controls.Add(pictureBox);                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayPdf(string filePath, FlowLayoutPanel panel)
        {
            using (var document = PdfDocument.Load(filePath))
            {
                using (var image = document.Render(0, 9000, 9000, false))
                {
                    PictureBox pictureBox = new PictureBox()
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        Size = new Size(574, 454),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Visible = true,
                        Image = image
                    };
                    pictureBox.MouseWheel += pic1_MouseWheel;
                    panel.Controls.Add(pictureBox);
                }
            }
        }

        private async void btn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                if (!string.IsNullOrEmpty(txtpatientId.Text.Trim()))
                {
                    patientModel = new PatientModel()
                    {
                        PatientName = txtpatientfamily.Text.Trim(),
                        PatientID = txtpatientId.Text.Trim()
                    };

                    if (await ConvertToDicomAndSendAsync(imagePath, patientModel))
                    {
                        MessageBox.Show("فایل تبدیل و ارسال شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Clear the image 
                        reset_image_setting();
                    }
                    else
                    {
                        MessageBox.Show("خطا در تبدیل یا ارسال فایل به سرور PACS", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("شماره مراجعه بیمار نمیتواند خالی باشد", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("شما اول باید فایل را انتخاب کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void reset_image_setting()
        {
            panel1.Controls.Clear();
            img = null;
            imagePath = string.Empty;
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Setting setting = new Setting())
            {
                setting.ShowDialog();
                LoadFileModels();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (About about = new About())
            {
                about.ShowDialog();
            }
        }
    }
}

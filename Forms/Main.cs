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
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace Convert_to_dcm
{
    public partial class Main : System.Windows.Forms.Form
    {
        private PdfViewer pdfViewer; // Add a PdfViewer control
        private SettingsModel settingsModel = new SettingsModel(); // لیست مدل‌های فایل
        PatientModel patientModel; // مدل بیمار
        private string imagePath = string.Empty; // مسیر تصویر
        private float zoomFactor = 1.0f; // Add a zoom factor


        public Main()
        {
            InitializeComponent();
            pdfViewer = new PdfViewer(); // Initialize pdfViewer in the constructor
            patientModel = new PatientModel(); // Initialize patientModel in the constructor
        }

        private void pic1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (pic1.Image == null)
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

            pic1.Width = (int)(pic1.Image.Width * zoomFactor);
            pic1.Height = (int)(pic1.Image.Height * zoomFactor);
            pic1.SizeMode = PictureBoxSizeMode.Zoom;
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
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF And Image|*.pdf;*.jpg;*.jpeg;*.png;*.bmp";
                openFileDialog.Title = "Select a PDF or Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    imagePath = openFileDialog.FileName;
                    string extension = Path.GetExtension(imagePath).ToLower();

                    if (extension == ".pdf")
                    {
                        DisplayPdf(imagePath);
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                    {
                        DisplayImage(imagePath);
                    }
                    else
                    {
                        MessageBox.Show("فایل انتخابی پشتیبانی نمیشود دوباره تلاش کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void ConvertToDicomAndSend(string filePath, PatientModel patientModel)
        {
            (string StudyInsUID, string SOPClassUID)? additionalTags = null;

            if (!string.IsNullOrEmpty(settingsModel.ServerAddress) &&
                !string.IsNullOrEmpty(settingsModel.Instance) &&
                !string.IsNullOrEmpty(settingsModel.username) &&
                !string.IsNullOrEmpty(settingsModel.password))
            {
                additionalTags = ExecuteSelectQuery(settingsModel, patientModel.PatientID);
            }
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
        }

        private (string StudyInsUID, string SOPClassUID) ExecuteSelectQuery(SettingsModel settings, string pid)
        {
            SQLCLASS sqlClass = new SQLCLASS(settings);
            DataTable resultTable = sqlClass.ExecuteSelectQuery(pid);

            if (resultTable.Rows.Count > 0)
            {
                DataRow row = resultTable.Rows[0];
                string sopClassUID = row["SOPClassUID"]?.ToString() ?? string.Empty;
                string studyInsUID = row["StudyInsUID"]?.ToString() ?? string.Empty;
                return (studyInsUID, sopClassUID);
            }
            else
            {
                throw new Exception("No matching records found.");
            }
        }

        private void AddDicomTags(DicomDataset dicomDataset, int width, int height, string photometricInterpretation, ushort samplesPerPixel, PatientModel patientModel, (string StudyInsUID, string SOPClassUID)? additionalTags = null)
        {
            dicomDataset.Add(DicomTag.PatientName, patientModel.PatientName);
            dicomDataset.Add(DicomTag.PatientID, patientModel.PatientID);
            dicomDataset.Add(DicomTag.StudyInstanceUID, additionalTags?.StudyInsUID ?? DicomUID.Generate().UID);
            dicomDataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate().UID);
            dicomDataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
            dicomDataset.Add(DicomTag.SOPClassUID, additionalTags?.SOPClassUID ?? DicomUID.SecondaryCaptureImageStorage.UID);
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

        private DicomFile ConvertImageToDicom(Bitmap bitmap, PatientModel patientModel, (string StudyInsUID, string SOPClassUID)? additionalTags)
        {
            // Create a DICOM file
            var dicomFile = new DicomFile();
            var dicomDataset = dicomFile.Dataset;
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            // Add necessary DICOM tags
            AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, additionalTags);

            // Convert the image to byte array
            var pixelData = DicomPixelData.Create(dicomDataset, true);
            pixelData.SamplesPerPixel = 3;
            pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
            pixelData.Height = (ushort)bitmap.Height;
            pixelData.Width = (ushort)bitmap.Width;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            byte[] pixelBytes = new byte[bitmapData.Stride * bitmapData.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelBytes, 0, pixelBytes.Length);
            pixelData.AddFrame(new MemoryByteBuffer(pixelBytes));
            bitmap.UnlockBits(bitmapData);

            return dicomFile;
        }
        Bitmap? img = null;
        private void DisplayImage(string filePath)
        {
            img = (Bitmap)Image.FromFile(filePath);
            pic1.Image = img;
            pic1.Visible = true;
        }

        private void DisplayPdf(string filePath)
        {
            using (var document = PdfDocument.Load(filePath))
            {
                using (var image = document.Render(0, 3000, 3000, false))
                {
                    img = (Bitmap?)image;
                    pic1.Image = img;
                    pic1.Visible = true;
                }
            }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            if (imagePath != string.Empty && File.Exists(imagePath))
            {
                patientModel = new PatientModel();
                patientModel.PatientName = txtpatientfamily.Text.Trim();
                patientModel.PatientID = txtpatientId.Text.Trim();

                ConvertToDicomAndSend(imagePath, patientModel);
                MessageBox.Show("فایل تبدیل و ارسال شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Clear the image and PDF viewer
                pic1.Image = null;
                pic1.Visible = false;
                imagePath = string.Empty;
            }
            else
            {
                MessageBox.Show("شما اول باید فایل را انتخاب کنید", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

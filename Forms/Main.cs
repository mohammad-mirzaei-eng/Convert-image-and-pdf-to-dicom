using Aspose.Pdf;
using Aspose.Pdf.Facades;
using Convert_to_dcm.Sql;
using Convert_to_dcom.Class;
using Convert_to_dcom.Class.Helper;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FileCopyer.Classes.Design_Patterns.Helper;
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
            InitializePdfViewer(); // Initialize the PDF viewer
            LoadFileModels(); // Load the file models
        }

        // متد بارگذاری مدل‌های فایل
        private void LoadFileModels()
        {
            settingsModel = SerializationHelper.LoadSettings();
        }

        private void InitializePdfViewer()
        {
            pdfViewer = new PdfViewer();
            pdfViewer.CoordinateType = PageCoordinateType.MediaBox; // Use MediaBox instead of User
            pdfViewer.AutoResize = true; // Set AutoResize to true instead of AutoSize
            this.Controls.Add(new System.Windows.Forms.Control() { Name = "pdfViewerControl" }); // Add a placeholder control
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

            var dicomFile = CreateDicomFile(filePath, patientModel, additionalTags);

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

        private DicomFile CreateDicomFile(string filePath, PatientModel patientModel, (string StudyInsUID, string SOPClassUID)? additionalTags)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".pdf")
            {
                return ConvertPdfToDicom(filePath, patientModel, additionalTags);
            }
            else
            {
                return ConvertImageToDicom(filePath, patientModel, additionalTags);
            }
        }

        private DicomFile ConvertImageToDicom(string imagePath, PatientModel patientModel, (string StudyInsUID, string SOPClassUID)? additionalTags)
        {
            // Load the image
            Bitmap bitmap = new Bitmap(imagePath);

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

        private DicomFile ConvertPdfToDicom(string pdfPath, PatientModel patientModel, (string StudyInsUID, string SOPClassUID)? additionalTags)
        {
            // Load the PDF document
            using (var document = new Aspose.Pdf.Document(pdfPath))
            {
                // Create a DICOM file
                var dicomFile = new DicomFile();
                var dicomDataset = dicomFile.Dataset;
                dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

                // Add necessary DICOM tags
                AddDicomTags(dicomDataset, 0, 0, PhotometricInterpretation.Monochrome2.Value, 1, patientModel, additionalTags);

                // Convert each page of the PDF to an image and add to DICOM
                for (int i = 0; i < document.Pages.Count; i++)
                {
                    using (var imageStream = new MemoryStream())
                    {
                        var resolution = new Aspose.Pdf.Devices.Resolution(300);
                        var pngDevice = new Aspose.Pdf.Devices.PngDevice(resolution);
                        pngDevice.Process(document.Pages[i], imageStream);
                        imageStream.Seek(0, SeekOrigin.Begin);

                        var bitmap = new Bitmap(imageStream);
                        dicomDataset.AddOrUpdate(DicomTag.Rows, (ushort)bitmap.Height);
                        dicomDataset.AddOrUpdate(DicomTag.Columns, (ushort)bitmap.Width);

                        // Convert the image to byte array
                        var pixelData = DicomPixelData.Create(dicomDataset, true);
                        pixelData.SamplesPerPixel = 1;
                        pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
                        pixelData.Height = (ushort)bitmap.Height;
                        pixelData.Width = (ushort)bitmap.Width;
                        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                        byte[] pixelBytes = new byte[bitmapData.Stride * bitmapData.Height];
                        System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelBytes, 0, pixelBytes.Length);
                        pixelData.AddFrame(new MemoryByteBuffer(pixelBytes));
                        bitmap.UnlockBits(bitmapData);
                    }
                }

                return dicomFile;
            }
        }

        private void DisplayImage(string filePath)
        {
            pic1.Image = Image.FromFile(filePath);
            pic1.Visible = true;
            pdfViewer.Close(); // Use Close method instead of ClosePdfFile
        }

        private void DisplayPdf(string filePath)
        {
            pdfViewer.Close(); // Use Close method instead of ClosePdfFile
            pdfViewer.BindPdf(filePath); // Use BindPdf method instead of OpenPdfFile
            pic1.Visible = false;
            pic1.Image = null;
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
                pdfViewer.Close(); // Use Close method to close the PDF file
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

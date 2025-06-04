using Microsoft.VisualStudio.TestTools.UnitTesting;
using Convert_to_dcm.Helper;
using Convert_to_dcm.Model;
using FellowOakDicom; // Keep this for DicomDataset, DicomTag etc.
using System.Drawing;
using System.Drawing.Imaging; // For PixelFormat

namespace Convert_to_dcm.Tests
{
    [TestClass]
    public class DicomConversionHelperTests
    {
        [TestMethod]
        public void AddDicomTags_ValidInputs_AddsRequiredTags()
        {
            var dataset = new DicomDataset();
            var patientModel = new PatientModel { PatientName = "Test^Patient", PatientID = "TestPID123" };
            string serverModality = "CT";

            // Diagnostic: Explicitly declare and use PhotometricInterpretation type with FQN
            FellowOakDicom.PhotometricInterpretation pi = FellowOakDicom.PhotometricInterpretation.Rgb;
            DicomConversionHelper.AddDicomTags(dataset, 100, 100, pi.Value, 3, patientModel, serverModality, null);

            Assert.AreEqual(patientModel.PatientName, dataset.GetString(DicomTag.PatientName));
            Assert.AreEqual(patientModel.PatientID, dataset.GetString(DicomTag.PatientID));
            Assert.AreEqual(serverModality, dataset.GetString(DicomTag.Modality));
            Assert.IsFalse(string.IsNullOrEmpty(dataset.GetString(DicomTag.StudyInstanceUID)), "StudyInstanceUID should be generated and not empty.");
            Assert.IsFalse(string.IsNullOrEmpty(dataset.GetString(DicomTag.SeriesInstanceUID)), "SeriesInstanceUID should be generated and not empty.");
            Assert.IsFalse(string.IsNullOrEmpty(dataset.GetString(DicomTag.SOPInstanceUID)), "SOPInstanceUID should be generated and not empty.");
            Assert.AreEqual(pi.Value, dataset.GetString(DicomTag.PhotometricInterpretation)); // Use the variable here
            Assert.AreEqual(DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID, dataset.GetString(DicomTag.TransferSyntaxUID));
        }

        [TestMethod]
        public void AddDicomTags_WithAdditionalTags_OverridesPatientNameAndUIDs()
        {
            var dataset = new DicomDataset();
            var patientModel = new PatientModel { PatientName = "Original^Name", PatientID = "OrigID" };
            (string StudyInsUID, string SOPClassUID, string PName) additionalTags =
                ("1.2.3.Study", "1.2.3.SOPClass", "Additional^Name");
            string serverModality = "MR";

            FellowOakDicom.PhotometricInterpretation pi = FellowOakDicom.PhotometricInterpretation.Rgb;
            DicomConversionHelper.AddDicomTags(dataset, 100, 100, pi.Value, 3, patientModel, serverModality, additionalTags);

            Assert.AreEqual(additionalTags.PName, dataset.GetString(DicomTag.PatientName));
            Assert.AreEqual(patientModel.PatientID, dataset.GetString(DicomTag.PatientID));
            Assert.AreEqual(additionalTags.StudyInsUID, dataset.GetString(DicomTag.StudyInstanceUID));
            Assert.AreEqual(additionalTags.SOPClassUID, dataset.GetString(DicomTag.SOPClassUID));
            Assert.AreEqual(serverModality, dataset.GetString(DicomTag.Modality));
        }

        [TestMethod]
        public void GetBitmapPixels_SolidRed2x2Bitmap_ReturnsCorrectRGBByteArray()
        {
            using (var bitmap = new Bitmap(2, 2, PixelFormat.Format24bppRgb))
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        bitmap.SetPixel(x, y, Color.Red);
                    }
                }

                byte[] pixelData = DicomConversionHelper.GetBitmapPixels(bitmap);
                Assert.AreEqual(12, pixelData.Length, "Pixel data length is incorrect.");
                for (int i = 0; i < pixelData.Length; i += 3)
                {
                    Assert.AreEqual(255, pixelData[i], $"Byte {i} (Red) should be 255");
                    Assert.AreEqual(0, pixelData[i + 1], $"Byte {i + 1} (Green) should be 0");
                    Assert.AreEqual(0, pixelData[i + 2], $"Byte {i + 2} (Blue) should be 0");
                }
            }
        }

        [TestMethod]
        public void ConvertImageToDicom_ValidBitmap_ReturnsDicomFileWithTags()
        {
            using (var bitmap = new Bitmap(10, 10, PixelFormat.Format24bppRgb))
            {
                for(int y=0; y<bitmap.Height; y++)
                {
                    for(int x=0; x<bitmap.Width; x++)
                    {
                        bitmap.SetPixel(x,y, Color.Blue);
                    }
                }

                var patientModel = new PatientModel { PatientID = "PID001", PatientName = "Test^Convert" };
                string serverModality = "OT";

                DicomFile dicomFile = DicomConversionHelper.ConvertImageToDicom(bitmap, patientModel, serverModality, null);

                Assert.IsNotNull(dicomFile, "DicomFile should not be null.");
                Assert.IsNotNull(dicomFile.Dataset, "DicomFile.Dataset should not be null.");

                Assert.AreEqual(DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile.FileMetaInfo.TransferSyntax, "TransferSyntax is incorrect.");
                Assert.AreEqual(DicomUID.SecondaryCaptureImageStorage, dicomFile.FileMetaInfo.MediaStorageSOPClassUID, "MediaStorageSOPClassUID is incorrect.");
                Assert.IsFalse(string.IsNullOrEmpty(dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID?.UID), "MediaStorageSOPInstanceUID should be generated.");

                Assert.AreEqual(patientModel.PatientID, dicomFile.Dataset.GetString(DicomTag.PatientID), "PatientID is incorrect.");
                Assert.AreEqual(patientModel.PatientName, dicomFile.Dataset.GetString(DicomTag.PatientName), "PatientName is incorrect.");
                Assert.AreEqual(serverModality, dicomFile.Dataset.GetString(DicomTag.Modality), "Modality is incorrect.");
                Assert.IsTrue(dicomFile.Dataset.Contains(DicomTag.PixelData), "Dataset should contain PixelData.");
                Assert.AreEqual((ushort)bitmap.Width, dicomFile.Dataset.GetSingleValue<ushort>(DicomTag.Columns), "Columns tag is incorrect.");
                Assert.AreEqual((ushort)bitmap.Height, dicomFile.Dataset.GetSingleValue<ushort>(DicomTag.Rows), "Rows tag is incorrect.");
                // Using FQN for PhotometricInterpretation
                Assert.AreEqual(FellowOakDicom.PhotometricInterpretation.Rgb.Value, dicomFile.Dataset.GetString(DicomTag.PhotometricInterpretation), "PhotometricInterpretation is incorrect.");

                // Using FQN for DicomPixelData
                FellowOakDicom.DicomPixelData pixelDataElement = FellowOakDicom.DicomPixelData.Create(dicomFile.Dataset);
                Assert.AreEqual(1, pixelDataElement.NumberOfFrames, "Number of frames should be 1.");
                byte[] frameData = pixelDataElement.GetFrame(0).Data;
                Assert.AreEqual(bitmap.Width * bitmap.Height * 3, frameData.Length, "PixelData length is incorrect.");
            }
        }
    }
}

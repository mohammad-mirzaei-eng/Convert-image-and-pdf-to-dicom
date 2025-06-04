using FellowOakDicom;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Convert_to_dcm.Model; // For PatientModel
using FellowOakDicom.IO.Buffer; // For MemoryByteBuffer

namespace Convert_to_dcm.Helper
{
    public class DicomConversionHelper
    {
        public static byte[] GetBitmapPixels(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb); // Assuming 24bppRgb for DICOM conversion

            try
            {
                int stride = bmpData.Stride;
                int width = bitmap.Width;
                int height = bitmap.Height;
                int bytesPerPixel = 3; // For RGB

                byte[] rawData = new byte[stride * height];
                Marshal.Copy(bmpData.Scan0, rawData, 0, rawData.Length);

                byte[] result = new byte[width * height * bytesPerPixel];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bmpIndex = y * stride + x * bytesPerPixel;
                        int dcmIndex = (y * width + x) * bytesPerPixel;

                        // Standard DICOM pixel order is BGR if Photometric Interpretation is RGB
                        // Bitmap stores pixels as BGR by default (when Format24bppRgb).
                        // So, if we want RGB in DICOM, we might need to swap.
                        // However, the original code copied BGR (rawData[bmpIndex+2] is R) to RGB (result[dcmIndex] is R).
                        // Let's keep the original logic: rawData (BGR) -> result (RGB as per original variable names)
                        // rawData[bmpIndex + 2] is Blue in System.Drawing.Bitmap if it's truly BGR
                        // rawData[bmpIndex + 1] is Green
                        // rawData[bmpIndex] is Red
                        // The comment in original code "RGB به BGR تبدیل رنگ‌ها" (RGB to BGR color conversion)
                        // suggests result should be BGR.
                        // result[dcmIndex] = rawData[bmpIndex + 2];     // R (Original comment says R, but this is Blue if source is BGR)
                        // result[dcmIndex + 1] = rawData[bmpIndex + 1]; // G
                        // result[dcmIndex + 2] = rawData[bmpIndex];     // B (Original comment says B, but this is Red if source is BGR)
                        // This implies original rawData was treated as RGB and result is BGR.
                        // Let's clarify: PixelFormat.Format24bppRgb usually means BGR in memory for GDI+.
                        // So, rawData[bmpIndex] is Blue, rawData[bmpIndex+1] is Green, rawData[bmpIndex+2] is Red.
                        // The original code:
                        // result[dcmIndex] = rawData[bmpIndex + 2];     // Copies Red to first byte
                        // result[dcmIndex + 1] = rawData[bmpIndex + 1]; // Copies Green to second byte
                        // result[dcmIndex + 2] = rawData[bmpIndex];     // Copies Blue to third byte
                        // This means 'result' is indeed RGB.
                        // If PhotometricInterpretation is RGB, then this order (R, G, B) is correct.

                        result[dcmIndex] = rawData[bmpIndex + 2];     // Red
                        result[dcmIndex + 1] = rawData[bmpIndex + 1]; // Green
                        result[dcmIndex + 2] = rawData[bmpIndex];     // Blue
                    }
                }
                return result;
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        public static void AddDicomTags(
            DicomDataset dicomDataset,
            int width,
            int height,
            string photometricInterpretation,
            ushort samplesPerPixel,
            PatientModel patientModel,
            string serverModality, // Added parameter for modality
            (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null)
        {
            // Note: Removed try-catch from here; caller should handle exceptions.
            // Or, define specific exceptions to throw from helper. For now, let it propagate.

            dicomDataset.AddOrUpdate(DicomTag.PatientName, !string.IsNullOrEmpty(additionalTags?.PName.Trim()) ? additionalTags?.PName : patientModel.PatientName);
            dicomDataset.AddOrUpdate(DicomTag.PatientID, patientModel.PatientID);
            dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, !string.IsNullOrEmpty(additionalTags?.StudyInsUID.Trim()) ? additionalTags?.StudyInsUID : DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, !string.IsNullOrEmpty(additionalTags?.SOPClassUID.Trim()) ? additionalTags?.SOPClassUID : DicomUID.SecondaryCaptureImageStorage.UID);
            dicomDataset.AddOrUpdate(DicomTag.PhotometricInterpretation, photometricInterpretation);
            dicomDataset.AddOrUpdate(DicomTag.TransferSyntaxUID, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID); // Ensure we get the string UID
            dicomDataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
            dicomDataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
            dicomDataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dicomDataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dicomDataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dicomDataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dicomDataset.AddOrUpdate(DicomTag.Modality, serverModality); // Use passed parameter
            dicomDataset.AddOrUpdate(DicomTag.SamplesPerPixel, samplesPerPixel);

            string currentTime = DateTime.Now.ToString("HHmmss");
            dicomDataset.AddOrUpdate(DicomTag.StudyTime, currentTime);
            dicomDataset.AddOrUpdate(DicomTag.SeriesTime, currentTime);
            // Consider adding DicomTag.InstanceCreationDate, DicomTag.InstanceCreationTime
            // DicomTag.ContentDate, DicomTag.ContentTime might also be relevant
        }

        public static DicomFile? ConvertImageToDicom(
            Bitmap bitmap,
            PatientModel patientModel,
            string serverModality, // Added parameter
            (string StudyInsUID, string SOPClassUID, string PName)? additionalTags)
        {
            // Note: Removed try-catch from here; caller should handle exceptions.
            try
            {
                var dicomFile = new DicomFile();
                var dicomDataset = dicomFile.Dataset;
                // Ensure FileMetaInfo is created before accessing it
                dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
                dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.SecondaryCaptureImageStorage; // Example, adjust as needed
                dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUID.Generate();


                AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, serverModality, additionalTags);

                byte[] pixelDataArray = GetBitmapPixels(bitmap);

                DicomPixelData pixelData = DicomPixelData.Create(dicomDataset, true);
                pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved; // For RGB
                pixelData.AddFrame(new MemoryByteBuffer(pixelDataArray));

                return dicomFile;
            }
            catch (Exception)
            {
                // Optionally log here if you have a static logger available, or rethrow specific custom exception
                // For now, let it propagate to be handled by Main.cs's LogError
                throw;
            }
        }
    }
}

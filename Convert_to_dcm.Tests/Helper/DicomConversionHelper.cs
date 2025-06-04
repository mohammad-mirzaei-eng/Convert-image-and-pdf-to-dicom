using FellowOakDicom;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Convert_to_dcm.Model; // For PatientModel
// using FellowOakDicom.IO.Buffer; // Temporarily removed for diagnostics

namespace Convert_to_dcm.Helper
{
    public class DicomConversionHelper
    {
        public static byte[] GetBitmapPixels(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bmpData.Stride;
                int width = bitmap.Width;
                int height = bitmap.Height;
                int bytesPerPixel = 3;

                byte[] rawData = new byte[stride * height];
                Marshal.Copy(bmpData.Scan0, rawData, 0, rawData.Length);

                byte[] result = new byte[width * height * bytesPerPixel];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bmpIndex = y * stride + x * bytesPerPixel;
                        int dcmIndex = (y * width + x) * bytesPerPixel;

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
            string photometricInterpretation, // Keep as string as this is what DicomDataset.AddOrUpdate expects
            ushort samplesPerPixel,
            PatientModel patientModel,
            string serverModality,
            (string StudyInsUID, string SOPClassUID, string PName)? additionalTags = null)
        {
            dicomDataset.AddOrUpdate(DicomTag.PatientName, !string.IsNullOrEmpty(additionalTags?.PName.Trim()) ? additionalTags?.PName : patientModel.PatientName);
            dicomDataset.AddOrUpdate(DicomTag.PatientID, patientModel.PatientID);
            dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, !string.IsNullOrEmpty(additionalTags?.StudyInsUID.Trim()) ? additionalTags?.StudyInsUID : DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
            dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, !string.IsNullOrEmpty(additionalTags?.SOPClassUID.Trim()) ? additionalTags?.SOPClassUID : DicomUID.SecondaryCaptureImageStorage.UID);
            dicomDataset.AddOrUpdate(DicomTag.PhotometricInterpretation, photometricInterpretation);
            dicomDataset.AddOrUpdate(DicomTag.TransferSyntaxUID, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);
            dicomDataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
            dicomDataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
            dicomDataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dicomDataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dicomDataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dicomDataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dicomDataset.AddOrUpdate(DicomTag.Modality, serverModality);
            dicomDataset.AddOrUpdate(DicomTag.SamplesPerPixel, samplesPerPixel);

            string currentTime = DateTime.Now.ToString("HHmmss");
            dicomDataset.AddOrUpdate(DicomTag.StudyTime, currentTime);
            dicomDataset.AddOrUpdate(DicomTag.SeriesTime, currentTime);
        }

        public static DicomFile? ConvertImageToDicom(
            Bitmap bitmap,
            PatientModel patientModel,
            string serverModality,
            (string StudyInsUID, string SOPClassUID, string PName)? additionalTags)
        {
            try
            {
                var dicomFile = new DicomFile();
                var dicomDataset = dicomFile.Dataset;
                dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
                dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.SecondaryCaptureImageStorage;
                dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUID.Generate();

                // Use PhotometricInterpretation directly here, it's an enum so .Value is appropriate for its string representation
                AddDicomTags(dicomDataset, bitmap.Width, bitmap.Height, PhotometricInterpretation.Rgb.Value, 3, patientModel, serverModality, additionalTags);

                byte[] pixelDataArray = GetBitmapPixels(bitmap);

                // Use DicomPixelData and PlanarConfiguration directly
                DicomPixelData pixelData = DicomPixelData.Create(dicomDataset, true);
                // PlanarConfiguration is nested under DicomPixelData
                pixelData.PlanarConfiguration = DicomPixelData.PlanarConfiguration.Interleaved; // For RGB
                // Use FQN for MemoryByteBuffer as its using directive was removed for diagnostics
                pixelData.AddFrame(new FellowOakDicom.IO.Buffer.MemoryByteBuffer(pixelDataArray));

                return dicomFile;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

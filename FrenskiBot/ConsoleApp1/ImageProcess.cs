using System;
using OpenCvSharp;

class ImageProcessor
{
    public static void ProcessImage(string inputPath, string outputPath)
    {
        // Load the image and convert to grayscale
        Mat original = Cv2.ImRead(inputPath, ImreadModes.Color);
        Mat grayscale = new Mat();
        Cv2.CvtColor(original, grayscale, ColorConversionCodes.BGR2GRAY);

        // Apply sharpening BEFORE thresholding
        Mat sharpened = new Mat();
        Mat kernel = new Mat(3, 3, MatType.CV_32F, new float[]
        {
            0, -1,  0,
           -1,  5, -1,
            0, -1,  0
        });

        Cv2.Filter2D(grayscale, sharpened, -1, kernel);

        // Apply adaptive thresholding (tune block size & C if needed)
        Mat binary = new Mat();
        Cv2.AdaptiveThreshold(sharpened, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 15, 3);

        // Invert colors for better OCR accuracy
        Cv2.BitwiseNot(binary, binary);

        // Save the final processed image
        Cv2.ImWrite(outputPath, binary);
        Console.WriteLine($"Processed image saved to: {outputPath}");
    }
}

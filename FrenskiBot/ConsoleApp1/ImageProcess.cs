using System;
using OpenCvSharp;

class ImageProcessor
{
    public static void ProcessImage(string inputPath, string outputPath)
    {
        // Step 1: Load the image and convert to grayscale
        Mat original = Cv2.ImRead(inputPath, ImreadModes.Color);
        Mat grayscale = new Mat();
        Cv2.CvtColor(original, grayscale, ColorConversionCodes.BGR2GRAY);

        // Step 2: Apply adaptive thresholding to create a binary image
        Mat binary = new Mat();
        Cv2.AdaptiveThreshold(grayscale, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 15, 5);

        // Save the binary image for debugging
        Cv2.ImWrite("debug_binary.png", binary);

        // Step 3: Apply sharpening to enhance edges on the binary image
        Mat sharpened = new Mat();
        Mat kernel = new Mat(3, 3, MatType.CV_32F, new float[]
        {
            0, -1,  0,
           -1,  5, -1,
            0, -1,  0
        });

        // Convert binary image to float, apply filter, and convert back to 8-bit
        binary.ConvertTo(binary, MatType.CV_32F);
        Cv2.Filter2D(binary, sharpened, -1, kernel);
        sharpened.ConvertTo(sharpened, MatType.CV_8U);

        // Save the sharpened image for debugging
        Cv2.ImWrite("debug_sharpened.png", sharpened);

        // Save the final processed image
        Cv2.ImWrite(outputPath, sharpened);
        Console.WriteLine($"Processed image saved to: {outputPath}");
    }
}

using System;
using Tesseract;

class OcrProcessor
{
    public static void ProcessImage(string tessDataPath, string imagePath)
    {
        try
        {
            Console.WriteLine($"Processing image: {imagePath}");

            // Initialize the Tesseract engine with Bulgarian language
            using var ocrEngine = new TesseractEngine(tessDataPath, "bul", EngineMode.Default);
            
            // Load the image
            using var img = Pix.LoadFromFile(imagePath);
            
            // Perform OCR
            using var page = ocrEngine.Process(img);
            string text = page.GetText();

            // Output confidence score
            Console.WriteLine($"OCR Confidence: {page.GetMeanConfidence()}");

            // Display extracted text
            Console.WriteLine("Extracted Text:");
            Console.WriteLine(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during OCR: " + ex.Message);
        }
    }
}

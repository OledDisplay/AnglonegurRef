using System;
using Tesseract;

class OcrProcessor
{
    public static string ProcessImage(string tessDataPath, string imagePath)
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
            return text;
        }
        catch (Exception ex)
        {
            return "Error during OCR: " + ex.Message;
        }
    }
}

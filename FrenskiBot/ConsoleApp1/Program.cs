using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using OpenQA.Selenium.DevTools;

class Program
{
    public static string output;
    public static string projectDir = AppContext.BaseDirectory; // Use BaseDirectory for consistent path
    public static string tessDataPath = Path.Combine(projectDir, "tessdata");
    string textbookPath = Path.Combine(projectDir,"plant.txt");
    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string lang = "bul"; // language tesData

                SynopsisGenerator generator = new SynopsisGenerator();

        
        // Paths
        string nugetPath = Path.Combine(projectDir, "nuget.exe");
        string projectFile = Path.Combine(AppContext.BaseDirectory, "ConsoleApp1.csproj"); // csproj folder name
        string tessDataPath = Path.Combine(projectDir, "tessdata");
        string tessDataFile = Path.Combine(tessDataPath, $"{lang}.traineddata"); // Language model

        // Ocr image paths
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
        string imageFolder = Path.Combine(projectRoot, "Uchebnik");
        string imagePath = Path.Combine(imageFolder, "");

        //Download web page
        string url = "https://bg.e-prosveta.bg/free-book/399?page="; // Site url
        string LogName = "jmatrozova@abv.bg";
        string LogPass = "parisjm1603";
        
        // Ensure tessdata folder exists
        if (!Directory.Exists(tessDataPath))
        {
            Directory.CreateDirectory(tessDataPath);
            Console.WriteLine("Created tessdata folder.");
        }

        // Download tessdata if it doesn't exist
        if (!File.Exists(tessDataFile))
        {
            string tessDataUrl = $"https://github.com/tesseract-ocr/tessdata_best/raw/main/{lang}.traineddata";
            Console.WriteLine($"Downloading tessdata file from {tessDataUrl}...");
            DownloadFile(tessDataUrl, tessDataFile);
            Console.WriteLine($"Downloaded: {tessDataFile}");
        }
        else
        {
            Console.WriteLine($"Tessdata file already exists: {tessDataFile}");
        }

        // Ensure nuget.exe exists
        if (!File.Exists(nugetPath))
        {
            Console.WriteLine($"nuget.exe not found at {nugetPath}. Please download it and place it in the project directory.");
            return;
        }

        // Restore NuGet packages
        try
        {
            Console.WriteLine("Restoring NuGet packages...");
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = nugetPath,
                    Arguments = $"restore \"{projectFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectDir // Ensure correct working directory
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during NuGet restore: {ex.Message}");
        }
        // User input 
        int urok;
        int DownloadTextbook = 1;
        Console.WriteLine("Format Y/N for questions..\nServer access?:");
        if(Console.ReadLine() == "Y") {
         DownloadTextbook = 0;
        }
        do {
          Console.WriteLine("Enter urok num > 1, int:");
        }
        while (!int.TryParse(Console.ReadLine(), out urok) && urok < 2);

        // Get png from webpage
        DownloadInfoScript.DownloadScript(url,projectRoot, LogName, LogPass, urok,DownloadTextbook); // url to textbook page,project root,LoginName,LoginPass

        for(int i = 0;i < 2; i++ ) {
         imagePath = Path.Combine(imageFolder, $"{urok}",$"pishki{i}.png");
         // Call image post - prosses
         string prossesedPath = Path.Combine(imageFolder,Path.GetFileNameWithoutExtension(imagePath) + "Processed" + Path.GetExtension(imagePath));
         ImageProcessor.ProcessImage(imagePath,prossesedPath);

         // Call OCR functionality
         try
         {
            Console.WriteLine("Starting OCR...");
            output += "\n" +OcrProcessor.ProcessImage(tessDataPath, prossesedPath);
            Console.WriteLine("OCR Completed.");
         }
         catch (Exception ex)
         {
            Console.WriteLine($"Error during OCR: {ex.Message}");
         }
         
        }

        Console.WriteLine(output);
        string plan = File.ReadAllText(Path.Combine(projectRoot,"plan.txt"));
        string info = output;
        string writingStyle = File.ReadAllText(Path.Combine(projectRoot,"writingstyle.txt"));

        string synopsis = await generator.GenerateSynopsis(plan, info, writingStyle);
        Console.WriteLine("Generated Synopsis:");
        Console.WriteLine(synopsis);

    }

    private static void DownloadFile(string url, string savePath)
    {
        try
        {
            using HttpClient client = new HttpClient();
            byte[] fileBytes = client.GetByteArrayAsync(url).Result;
            File.WriteAllBytes(savePath, fileBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
        }
    }
}

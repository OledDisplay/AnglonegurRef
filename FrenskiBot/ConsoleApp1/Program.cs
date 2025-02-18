using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;

using Newtonsoft.Json;
using System.Text.Json.Serialization;
using OpenQA.Selenium.BiDi.Modules.Log;
using System.ComponentModel;

class Program
{
    public static string LogName = "";
    public static string LogPass = "";
    public static string ApiKey = "";
    public static string output;
    public static string projectDir = AppContext.BaseDirectory; // Use BaseDirectory for consistent path
    public static string tessDataPath = Path.Combine(projectDir, "tessdata");
    string textbookPath = Path.Combine(projectDir,"plant.txt");

    public static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string lang = "bul"; // language tesData

                Apiscript generator = new Apiscript();
   

        // Paths
        string nugetPath = Path.Combine(projectDir, "nuget.exe");
        string projectFile = Path.Combine(AppContext.BaseDirectory, "ConsoleApp1.csproj"); // csproj folder name
        string tessDataPath = Path.Combine(projectDir, "tessdata");
        string tessDataFile = Path.Combine(tessDataPath, $"{lang}.traineddata"); // Language model

        // Ocr image paths
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
        string imageFolder = Path.Combine(projectRoot, "Uchebnik");
        string imagePath = Path.Combine(imageFolder, "");

        //ConsolePath
        /*string pathConsole = Path.Combine(projectRoot,"ConsoleSaves");
        string ConsoleOptions = Path.Combine(pathConsole, "Options.json");
        File.Create(ConsoleOptions);*/
    

        //Download web page
        string url = "https://bg.e-prosveta.bg/free-book/399?page="; // Site url

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

        //await ConsoleScript(ConsoleOptions);

        // User input 
        int urok;
        int DownloadTextbook = 1;
        Console.WriteLine("Format Y/N for questions..\nDownload dynamicly or use local download?:");
        if(Console.ReadLine() == "Y") {
         DownloadTextbook = 0;
        }
        do {
          Console.WriteLine("Enter urok num > 1, int:");
        }
        while (!int.TryParse(Console.ReadLine(), out urok) && urok < 2);
        
        //EXTRA OPTIONS (un-need conventionally but are stil useful to have)

        //Estimate page before agressive download
        bool agro = false;
        if(DownloadTextbook == 0) {
            Console.WriteLine("Use Agressive download?:");
            if(Console.ReadLine() == "Y") agro = true;
        }

        //Summarize 
        bool summType = false;
        Console.WriteLine("Summarize info text (leaves more space for responce) or keep all?:");
        if(Console.ReadLine() == "Y"){
            summType = true;
        }


        // Get png from webpage
        await DownloadInfoScript.DownloadScript(url,projectRoot, LogName, LogPass, urok,DownloadTextbook,agro); // url to textbook page,project root,LoginName,LoginPass

        // Barebones ocr - cant read handwriting (handwriting is more taxing so we use that functionality sparingly)
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
            File.Delete(prossesedPath);
         }
         catch (Exception ex)
         {
            Console.WriteLine($"Error during OCR: {ex.Message}");
         }
        }
        

        // Remove empty spaces from ocr-d text
        string cleanedText = string.Join("\n", output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()) // Trim spaces around the lines
            .Where(line => !string.IsNullOrEmpty(line))); // Ensure non-empty lines


        Console.WriteLine(cleanedText);

        
        //Set paths for api
        string apiFolder = Path.Combine(projectRoot, "ApiMaterialsProg");
        string DebugFolder = Path.Combine(projectRoot, "DebugOutputs");
        string wrStyleContent = File.ReadAllText(Path.Combine(apiFolder,"GPT-generatedAPIwrstyle.txt"));
        string plan = File.ReadAllText(Path.Combine(apiFolder,"plan.txt"));
        string InputSys = File.ReadAllText(Path.Combine(apiFolder,"InputSystem.txt"));
        string InputUser = File.ReadAllText(Path.Combine(apiFolder,"InputUser.txt"));
        string writingNotes= File.ReadAllText(Path.Combine(apiFolder,"ExtraWritingStyleNotes.txt"));
        string summarizePrompt = File.ReadAllText(Path.Combine(apiFolder, summType ? "CleanupPreserve.txt" : "CleanupSummarize.txt"));
        string info = output;
       
        //Debug / saved output paths
        string infoCleanDB = Path.Combine(DebugFolder,"CleanInfo.txt");
        string infoSumDB = Path.Combine(DebugFolder,"info summarized.txt");
        string conspecutsDB = Path.Combine(DebugFolder,$"conspectus{urok}.txt");
        string BulkDB = Path.Combine(DebugFolder,"WRstyleBulk.txt");
        List<string> finalPropmt = Apiscript.SplitIntoChunks(info,2000);
        
    
        // Writing style folder
        string wrStyleFolder = Path.Combine(projectRoot,"WritingStyle");
        Directory.CreateDirectory(wrStyleFolder);

        
        // Check for writing style file and extract writing style
        do {
            Console.WriteLine("Check if writing style folder contains example text files. If not, add some! Confirm with input ");
        }
        while(Console.ReadLine() == "" && Directory.GetFiles(wrStyleFolder).Length == 0);
        switch(File.Exists(Path.Combine(wrStyleFolder,"writingstyle.txt")) ? 1: 0) {
            case 1:
             Console.WriteLine("Writing style file prescent. Replace with current example text? Y/N:");
             if(Console.ReadLine() == "Y") File.Delete(Path.Combine(wrStyleFolder,"writingstyle.txt"));
             else break;
             goto default;
             
            default:
             string WritingStyleBulk = "";
             string[] files = Directory.GetFiles(wrStyleFolder);
             for(int i = 0; i < files.Length; i++ ) {
              if(Path.GetExtension(Path.Combine(wrStyleFolder, files[i])) != ".txt"){ 
                // advanced hand writing ocr
                Console.WriteLine("Writing style reader not implemented yet.");
              }
              else WritingStyleBulk += File.ReadAllText(Path.Combine(wrStyleFolder, files[i]));
             }
             File.WriteAllText(BulkDB,WritingStyleBulk);
             await AnalyzeScript.SaveWrStyle(WritingStyleBulk,Path.Combine(wrStyleFolder,"writingstyle.txt"),Path.Combine(apiFolder,"WritingStyleInputDes.txt"));
             break;
        }

        //summerization info text a little
        Console.WriteLine("Cleaning up the text from the textbook...");
        List<string> preprocessedChunks = await Apiscript.PreprocessChunks(finalPropmt,summarizePrompt); //info text unedited, which summ prompt to use

        Console.WriteLine("Enter conspectus size in words:");;
        string ApiOutput= await Apiscript.GenerateSynopsis(preprocessedChunks,plan,wrStyleContent,Console.ReadLine(),writingNotes,InputSys,InputUser); // info text,writing style,conspectus size, extra writing notes, input
        Console.WriteLine("\n\nGenerated Conspectus:");
        Console.WriteLine(ApiOutput);

        //write to debug
        File.WriteAllText(infoCleanDB,cleanedText);
        File.WriteAllText(infoSumDB,string.Join("",preprocessedChunks));
        File.WriteAllText(conspecutsDB,ApiOutput);
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

    // Nov console script se oshte ne bachka
    
    private static void ConsoleScript(string FilePath){
      string sysMessage;
      int layer = 1;
      int option = 1;
      List<string> Lay1 = new List<string>{"Uchebnik and Dev settings","ApiSettings","MainSettings","GenerateConspectus"};
      List<string> Lay2 = new List<string>{"1DynamicDownload","1AgressiveDownload","1DevTerminal","2AgressiveSummarizer","2SaveApiMaterials","3UrokNum","3size","3WritingStyle"}; // put option number infront of name of nested options
      List<string> Lay3 = new List<string>{"4Change","4AddNew","4Delete"};
      List<string> Lay4 = new List<string>(), InputPackage = new List<string>(); // lay 4 is bulit dynamicly

      List<List<string>> Layers = new List<List<string>>{Lay1,Lay2,Lay3, Lay4};
      List<int> LevelGrid = [1,1,1,1];

      //setup options file 
      if(string.IsNullOrEmpty(File.ReadAllText(FilePath).Trim())){
        var userman = new User{
            DynamicDownload = "",
            AgressiveDownload = "",
            WrStylePath = "",
            AgroSumm = "",
            DevTerminal = "",
        };

        string json = JsonConvert.SerializeObject(userman, Formatting.Indented);
        File.WriteAllText(FilePath, json);
        sysMessage = "Options file created";
      }

     string jsonString = File.ReadAllText(FilePath);
     User textjson = JsonConvert.DeserializeObject<User>(jsonString);

     Console.WriteLine(@"Configure Frenskibot with a menu..");
     Thread.Sleep(2000);
     
    while(true)
    {
        List<string> DisplayedMenus = new List<string>();

        if(LevelGrid[layer] != -1)
        {
            switch(layer)
            {
                case 1: 
                    layer++;
                    break;
                case 2:
                    foreach(string menu in Layers[option])
                    {
                        if(GetMenuLevel(menu)==LevelGrid[layer])
                        {
                             DisplayedMenus.Add(menu);
                        }
                    }
                    layer++;
                    switch(LevelGrid[0])
                    {
                            case 1:
                              switch(LevelGrid[layer]){}
                            //DynamicDownload
                            break;

                            case 2:

                            //DynamicDownload
                            break;
                            case 3:

                            //DynamicDownload
                            break;


                           
                    }
                    break;
                case 3:

                break;
            }

        }
        LevelGrid[layer] = SwitchOptions(DisplayedMenus);
       // string updatedJson = JsonConvert.SerializeObject(user, Formatting.Indented);
        //File.WriteAllText(FilePath, updatedJson);

        Console.WriteLine("Updated JSON saved successfully!");
    }
   
     //end it off
    }

    private static int GetMenuLevel(string Menu)
    {
        char LevelChar = Menu.ToCharArray()[0];

        return Convert.ToInt16(Char.GetNumericValue(LevelChar));
    }


    private static int SwitchOptions(List<string> options){
     ConsoleKeyInfo key;
     int opt = 1, max_options = options.Count(),interaction = 0;

      do
        {
            Console.Clear();

            for (int i = 0; i < max_options; i++)
            {
                if (i == opt)
                    Console.WriteLine(">" + options[i]);  // Selected option
                else
                    Console.WriteLine(" " + options[i]);  // Non-selected option
            }

            key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                    if (opt < max_options) opt++;
                    else{
                        opt= 1;
                    }
                    break;
                case ConsoleKey.UpArrow:
                    if (opt > 1) opt--;
                    else{
                        opt= max_options;
                    }
                    break;
                case ConsoleKey.Enter:
                    interaction = opt;
                    break;
                case ConsoleKey.Escape:
                    interaction = -1;
                    break;
            }
        } while (interaction == 0);

        return interaction;
    }
}

public class User
{
    public string DynamicDownload { get; set; }
    public string AgressiveDownload { get; set; }
    public string WrStylePath { get; set; }
    public string AgroSumm { get; set; }
    public string DevTerminal { get; set; }
}

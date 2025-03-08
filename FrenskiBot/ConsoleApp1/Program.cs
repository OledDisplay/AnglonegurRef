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
using System.Reflection;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using OpenQA.Selenium.BiDi.Modules.Log;
using System.ComponentModel;
using OpenQA.Selenium;
using System.Collections.Specialized;
using OpenCvSharp.XPhoto;

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
        string pathConsole = Path.Combine(projectRoot,"ConsoleSaves");
        string ConsoleOptions = Path.Combine(pathConsole, "Options.json");
        Directory.CreateDirectory(pathConsole);
        if (!File.Exists(ConsoleOptions))
            {
                using (FileStream fs = File.Create(ConsoleOptions)) { }
            }

    

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
        
        // settings paths (all these paths are the actualy important code-wise so they are defined a bit lower)

        //Set paths for api
        string apiFolder = Path.Combine(projectRoot, "ApiMaterialsProg");
        string skeleton = Path.Combine(projectRoot, "skeleton.txt");
        string DebugFolder = Path.Combine(projectRoot, "DebugOutputs");
        string wrStyleContent = File.ReadAllText(Path.Combine(apiFolder,"GPT-generatedAPIwrstyle.txt"));
        string plan = File.ReadAllText(Path.Combine(apiFolder,"plan.txt"));
        string InputSys = File.ReadAllText(Path.Combine(apiFolder,"InputSystem.txt"));
        string InputUser = File.ReadAllText(Path.Combine(apiFolder,"InputUser.txt"));
        string writingNotes= File.ReadAllText(Path.Combine(apiFolder,"ExtraWritingStyleNotesSkeleton.txt"));
        string info = output;
       
        //Debug / saved output paths
        string infoCleanDB = Path.Combine(DebugFolder,"CleanInfo.txt");
        string infoSumDB = Path.Combine(DebugFolder,"info summarized.txt");
        string BulkDB = Path.Combine(DebugFolder,"WRstyleBulk.txt");
    
        // Writing style folder
        string wrStyleFolder = Path.Combine(projectRoot,"WritingStyle");
        Directory.CreateDirectory(wrStyleFolder);
        
        // Settings paths and further options
        //string[] options = await ConsoleScript(ConsoleOptions, wrStyleFolder,apiFolder,BulkDB); // launch console app
        int urok = 16;
        int DownloadTextbook = 1;
        bool agro = true;
        bool summType = false;
        // dependant paths
        string conspecutsDB = Path.Combine(DebugFolder,$"conspectus{urok}.txt");
        string summarizePrompt = File.ReadAllText(Path.Combine(apiFolder, "CLG.txt"));
        //string summarizePrompt = File.ReadAllText(Path.Combine(apiFolder, summType ? "CleanupSummarize.txt" : "CleanupPreserve.txt"));


        // Generation process
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

        //summarize info tex
        List<string> finalPropmt = Apiscript.SplitIntoChunks(cleanedText,2000);

        //summerization info text a little
        Console.WriteLine("Cleaning up the text from the textbook...");
        List<string> preprocessedChunks = await Apiscript.PreprocessChunks(finalPropmt,summarizePrompt); //info text unedited, which summ prompt to use

        Console.WriteLine("Enter conspectus size in words:");;
        string ApiOutput= await Apiscript.GenerateSynopsis(preprocessedChunks,plan,wrStyleContent,Console.ReadLine(),writingNotes,InputSys,InputUser,skeleton); // info text,writing style,conspectus size, extra writing notes, input
        Console.WriteLine("\n\nGenerated Conspectus:");
        Console.WriteLine(ApiOutput);

        //write to debug
        File.WriteAllText(infoCleanDB,cleanedText);
        File.WriteAllText(infoSumDB,string.Join("",preprocessedChunks));
        File.WriteAllText(conspecutsDB,ApiOutput);
    }


    private static async Task<string> WritingStyle(string WrStyleFolder,string ApiFolder, string DebugFolder, int type,string wrIndex){
        switch(type) {
            case -1:
             File.Delete(Path.Combine(WrStyleFolder,$"writingstyle{wrIndex}.txt"));
             return "";
            case 0:
             return File.ReadAllText($"writingstyle{wrIndex}.txt");
            default:
             string WritingStyleBulk = "";
             string[] files = Directory.GetFiles(WrStyleFolder);
             for(int i = 0; i < files.Length; i++ ) {
              if(Path.GetExtension(Path.Combine(WrStyleFolder, files[i])) != ".txt"){ 
                // advanced hand writing ocr
                Console.WriteLine("Writing style reader not implemented yet.");
              }
              else if(!files[i].Contains("writingstyle")) WritingStyleBulk += File.ReadAllText(Path.Combine(WrStyleFolder, files[i]));
             }
             File.WriteAllText(DebugFolder,WritingStyleBulk);
             await AnalyzeScript.SaveWrStyle(WritingStyleBulk,Path.Combine(WrStyleFolder,$"writingstyle{wrIndex}.txt"),Path.Combine(ApiFolder,"WritingStyleInputDes.txt"));
             return File.ReadAllText(Path.Combine(WrStyleFolder,$"writingstyle{wrIndex}.txt"));
        }

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

    // Demo console script, notes are to be added as changes are still happening rapidly
    private static List<string> InputPackage = new List<string>(), PathSettings = new List<string>(),  TempNumOptions = new List<string> { "UrokNum", "Size" },SettingsOutput = new List<string> { "", "", "" }; // Path is used in Add options bonus and it is easier to have it as static
    private static PropertyInfo prop;
    private static string jsonString;
    private static User textjson;
    private static async Task<string[]> ConsoleScript(string FilePath, string WrStyleFolder, string ApiFolder, string DebugFolder)
{
    string sysMessage = "", cOpt = "", a = "", WrStyleContents = "";

    ConsoleKeyInfo key;
    int layer = 0, option = 0, edit, oldlayer = 0;
    List<string> Lay1 = new List<string> { "0Uchebnik and Dev settings", "0ApiSettings", "0MainSettings", "0GenerateConspectus" };
    List<string> Lay2 = new List<string> { "1DynamicDownload", "1AgressiveDownload", "1DevTerminal", "2AgressiveSummarizer", "2SaveApimaterials", "3UrokNum", "3Size", "3WritingStyle" }; // put option number infront of name of nested options
    List<string> Lay3 = new List<string> { "3Change", "3AddNew", "3Delete" };
    List<string> Lay4 = new List<string>(); // lay 4 is built dynamically
    List<string> Bools = new List<string> { "DynamicDownload", "AgressiveDownload", "DevTerminal", "AgressiveSummarizer", "SaveAPImaterials" };
    List<List<string>> Layers = new List<List<string>> { Lay1, Lay2, Lay3, Lay4 };

    //prep prop info
    var userman = new User
    {
        DynamicDownload = "",
        AgressiveDownload = "",
        WrStylePath = "",
        AgressiveSummarizer = "",
        DevTerminal = "",
        SaveAPImaterials = "",
    };

    //setup options file 
    if (string.IsNullOrEmpty(File.ReadAllText(FilePath).Trim()))
    {
        string json = JsonConvert.SerializeObject(userman, Formatting.Indented);
        File.WriteAllText(FilePath, json);
        sysMessage = "Options file created";
    }

    jsonString = File.ReadAllText(FilePath);
    textjson = JsonConvert.DeserializeObject<User>(jsonString);

    Console.WriteLine(@"Configure Frenskibot with a menu..");
    Thread.Sleep(2000);

    prop = typeof(User).GetProperty("WrStylePath");
    if (!jsonString.Contains($"\"{prop}\": \"\"") || jsonString.Contains($"\"{prop}\": \"  \""))
    {
        WrStyleContents = prop.GetValue(textjson).ToString();
    }
    Console.Clear();
    while (true)
    {
        if (InputPackage.Count == 0) InputPackage = FindOptionsByIndex(option.ToString(), Layers[layer]); // inputpackage is built prematurely with wrstyle
        if (oldlayer != layer) option = 1;
        option = SwitchOptions(sysMessage, string.Join("", PathSettings).Replace(@"\", @"\\"));
        if (option != -1) cOpt = InputPackage[option - 1];
        oldlayer = layer;

        sysMessage = "";
        prop = typeof(User).GetProperty(cOpt);
        if (option == -1 && layer > 0)
        {
            layer--;
            PathSettings.RemoveAt(PathSettings.Count - 1);
            sysMessage = $"Moved back";
            option = 0;
            InputPackage.Clear();
        }
        else
        {
            // check for extra behaviour first and use goto
            if (string.Join("", PathSettings).Replace(@"\", @"\\") == "4")
            { // leave the script
                if (WrStyleContents == "") sysMessage = "No writing style file selected. Make sure all Main options are configured";
                else if (SettingsOutput[0] == "") sysMessage = "No lesson index given. Make sure all Main options are configured";
                else if (SettingsOutput[1] == "") sysMessage = "No size for the conspectus given. Make sure all Main options are configured";

                else
                {
                    Console.WriteLine("\nSystem Message:\nStarting generation process..");
                    goto exit_while;
                }
            }

            else if (Bools.Contains(cOpt))
            {
                sysMessage = $"Confirmed False for {cOpt}";
                if (prop.GetValue(textjson).ToString() == "Y") prop.SetValue(textjson, "N");
                else
                {
                    prop.SetValue(textjson, "Y");
                    sysMessage = $"Confirmed True for {cOpt}";
                }

                string json1 = JsonConvert.SerializeObject(textjson, Formatting.Indented);
                File.WriteAllText(FilePath, json1);
                continue;
            }
            else if (prop != null)
            {
                sysMessage = "Enter value";
                Update(option, string.Join("", PathSettings).Replace(@"\", @"\\"), sysMessage);

                edit = prop.GetValue(textjson).ToString().Length;
                Console.SetCursorPosition(edit, Console.CursorTop); // Move cursor to the end of the current line
                StringBuilder inputBuilder = new StringBuilder(prop.GetValue(textjson).ToString());

                while (true)
                {
                    key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        break; // Exit input loop on Enter
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (inputBuilder.Length > 0)
                        {
                            inputBuilder.Remove(inputBuilder.Length - 1, 1);
                            Console.Write("\b \b"); // Move cursor back, overwrite with space, move back again
                        }
                    }
                    else if (char.IsDigit(key.KeyChar))
                    {
                        inputBuilder.Append(key.KeyChar);
                        Console.Write(key.KeyChar); // Print the digit to the right
                    }
                }
                
                sysMessage = $"Confirmed {inputBuilder}";
                prop.SetValue(textjson, inputBuilder.ToString());
                string json1 = JsonConvert.SerializeObject(textjson, Formatting.Indented);
                File.WriteAllText(FilePath, json1);
                continue;
            }

            else if (TempNumOptions.Contains(cOpt))
            {
                sysMessage = "Enter value";
                Update(option, string.Join("", PathSettings).Replace(@"\", @"\\"), sysMessage);

                edit = SettingsOutput[TempNumOptions.IndexOf(cOpt)].Length;
                Console.SetCursorPosition(edit, Console.CursorTop); // Move cursor to the end of the current line
                StringBuilder inputBuilder = new StringBuilder(SettingsOutput[TempNumOptions.IndexOf(cOpt)]);

                while (true)
                {
                    key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        break; // Exit input loop on Enter
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (inputBuilder.Length > 0)
                        {
                            inputBuilder.Remove(inputBuilder.Length - 1, 1);
                            Console.Write("\b \b"); // Move cursor back, overwrite with space, move back again
                        }
                    }
                    else if (char.IsDigit(key.KeyChar))
                    {
                        inputBuilder.Append(key.KeyChar);
                        Console.Write(key.KeyChar); // Print the digit to the right
                    }
                }
                sysMessage = $"Confirmed {inputBuilder}";
                SettingsOutput[TempNumOptions.IndexOf(cOpt)] = inputBuilder.ToString();
                continue;
            }

            else if (layer < Layers.Count)
            {
                PathSettings.Add("\\" + option);
                layer++;
                sysMessage = $"Selected option {cOpt}. Moved to layer {layer}";
            }
            //debug
            string prepPath = string.Join("", PathSettings).Replace(@"\", @"\\").Substring(0, string.Join("", PathSettings).LastIndexOf("\\"));
            sysMessage = $"Debug: {prepPath}";
            InputPackage.Clear();
            if(layer == 3 && prepPath == @"3\\3\\2"){
                await WritingStyle(WrStyleFolder, ApiFolder, DebugFolder, 1, (option + 1).ToString());
                 prop.SetValue(textjson, $"writingstyle{option}.txt");
                 sysMessage = $"Added and selected new writingstyle{option}.txt file";
            }
            else if (layer == 3 && !sysMessage.Contains("Selected option"))
            {
                string[] files = Directory.GetFiles(WrStyleFolder);
                if(files.Length == 0){
                  sysMessage = "No writing style currently present. Use AddNew";
                  InputPackage.Add("No Files");
                } 
                foreach (var name in files)
                {
                    InputPackage.Add(name);
                }
            }
            else if (layer == 3)
            {
                prop = typeof(User).GetProperty("WrStylePath");

                if (prepPath == "3\\3\\1")
                { //change
                    WrStyleContents = await WritingStyle(WrStyleFolder, ApiFolder, DebugFolder, 0, option.ToString());
                    prop.SetValue(textjson, $"writingstyle{option + 1}.txt");
                    sysMessage = $"Changed writing style file to writingstyle{option + 1}.txt";
                }
                if (prepPath == @"3\\3\\3" && File.Exists(Path.Combine(WrStyleFolder, $"writingstyle{option - 1}.txt"))) 
                {//delete
                    WrStyleContents = await WritingStyle(WrStyleFolder, ApiFolder, DebugFolder, -1, option.ToString());
                    prop.SetValue(textjson, $"writingstyle{option - 1}.txt");
                    sysMessage = $"Deleted writingstyle{option}.txt file";
                }

            }
        }
        string json = JsonConvert.SerializeObject(textjson, Formatting.Indented);
        File.WriteAllText("propinfo.json", json);
    }
    exit_while:
    JObject jsonObj = JObject.Parse(jsonString);
    List<object> valuesList = new List<object>();

    foreach (var value in jsonObj.Values())
    {
        valuesList.Add(value);
    }
    Thread.Sleep(2000);
    SettingsOutput.AddRange(valuesList.Select(n => n.ToString()));
    //
    SettingsOutput.Add(WrStyleContents);
    return SettingsOutput.ToArray();
}

// Helper method that returns the bonus string for a given property name.
private static string GetBonusForOption(string property)
{
    if (string.IsNullOrWhiteSpace(property))
        return null; // Return null if the property is invalid.

    // Get the current value from the deserialized settings.
    PropertyInfo p = typeof(User).GetProperty(property);
    if (p == null)
    {
        // If the property does not exist in the JSON, return null.
        return null;
    }

    string currentValue = p.GetValue(textjson)?.ToString() ?? "";

    if (!string.IsNullOrWhiteSpace(currentValue))
    {
        // If a value is set, return it.
        return currentValue;
    }
    else
    {
        // If no current value, try to get the default property.
        PropertyInfo defaultProp = typeof(User).GetProperty("Default" + property);
        string defaultValue = defaultProp?.GetValue(textjson)?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            // Return the default value.
            return defaultValue + "~";
        }
        else
        {
            // If neither is set, return an empty string.
            return "";
        }
    }
}

// Updated Update method that prints each option with its bonus info on the same line.
private static void Update(int Fopt, string Fpath, string Message)
{
    Console.Clear();
    Console.WriteLine($"   Main{Fpath}\n   ------");

    // For each option in the InputPackage, append the bonus value.
    for (int i = 0; i < InputPackage.Count; i++)
    {
        string option = InputPackage[i];
        string bonus = GetBonusForOption(option);

        // Determine the prefix for selection (">" for selected, spaces for others).
        string prefix = (i + 1 == Fopt ? " >" : "  ");

        // Determine the special symbol and bonus text.
        string specialSymbol = "";
        string bonusText = "";

        // Handle numeric options separately
        if (TempNumOptions.Contains(option))
        {
            bonus = SettingsOutput[TempNumOptions.IndexOf(option)];
        }

        if (bonus == null && !TempNumOptions.Contains(option))
        {
            // If the option is not part of the JSON, print it as-is.
            prefix += " ";
            Console.WriteLine($"{prefix}{option}");
            continue;
        }
        else if (string.IsNullOrEmpty(bonus))
        {
            // If no value or default value exists, use a warning symbol.
            specialSymbol = "⚠️";
        }
        else
        {
            // If a value or default value exists, use a dot.
            if (bonus.EndsWith("~"))
            {
                specialSymbol = "•";
                bonus = bonus.Remove(bonus.Length - 1); // Remove the tilde
            }
            else specialSymbol = " ";
            bonusText = $"[{bonus}]"; // Format the bonus text
        }

        // Format the line with the special symbol at the beginning and the JSON value on the right.
        string formattedLine = $"{specialSymbol}{prefix}{option} {bonusText}";
        Console.WriteLine(formattedLine);
    }

    Console.WriteLine("   ------\n   System Message: " + Message);
}
    private static int SwitchOptions(string sysMessage,string Fpath){
     ConsoleKeyInfo key;
     int opt = 1, max_options = InputPackage.Count(),interaction = 0;
    
      do
        {
         Update(opt,Fpath,sysMessage);

         key = Console.ReadKey(true);

         switch (key.Key){
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

  private static List<string> FindOptionsByIndex(string Index,List<string> Layer){
    List<string> Output = new List<string>{};
    for(int i = 0; i<Layer.Count;i++){
        if(Layer[i][0].ToString() == Index || Index == "-1") Output.Add(Layer[i].Substring(1));
    }
    return Output;
  }
}
public class User
{
    public string DynamicDownload { get; set; }
    public string AgressiveDownload { get; set; }
    public string WrStylePath { get; set; }
    public string AgressiveSummarizer { get; set; }
    public string DevTerminal { get; set; }
    public string SaveAPImaterials { get; set; }

    public string DefaultDynamicDownload { get; set; } = "N";
    public string DefaultAgressiveDownload { get; set; } = "Y";
    public string DefaultAgressiveSummarizer { get; set; } = "N";
    public string DefaultDevTerminal { get; set; } = "N";
    public string DefaultSaveAPImaterials { get; set; } = "Y";
}

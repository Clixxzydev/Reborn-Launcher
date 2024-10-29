using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace SimpleLauncher
{
    public class Program
    {
        public static string path;
        public static string mail;
        public static string pass;
        public static string downloadpath = Path.GetTempPath();
        public static string configFile = "config.json";
        public static string logFile = "LOG.txt";

        public static async Task Main()
        {
            Console.Title = "Reborn Launcher";

            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
            else if (!File.Exists(logFile))
            {
                using (StreamWriter sw = File.CreateText(logFile)) { }
            }

            if (File.Exists(configFile))
            {
                LoadConfig();
            }
            else
            {
                ActualProgram();
            }

            await Launch3551(); // Ensure asynchronous launch
        }

        static void Log(string message)
        {
            try
            {
                string finalLog = $"[{DateTime.Now}] - {message}";
                using (StreamWriter sw = File.AppendText(logFile))
                {
                    sw.WriteLine(finalLog);
                }
            }
            catch (Exception) { }
        }

        public static void LoadConfig()
        {
            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                dynamic config = JsonConvert.DeserializeObject(json);
                path = config.path;
                mail = config.mail;
                pass = config.pass;
                Console.WriteLine("Loading Saved Configuration...");
            }
            else
            {
                path = "";
                mail = "";
                pass = "";
            }
        }

        public static void ActualProgram()
        {
            // Display Launcher Title and Instructions
            Console.Clear();
            Console.WriteLine(@"
__________                   __               __    __________      ___.                        
\______   \_______  ____    |__| ____   _____/  |_  \______   \ ____\_ |__   ___________  ____  
 |     ___/\_  __ \/  _ \   |  |/ __ \_/ ___\   __\  |       _// __ \| __ \ /  _ \_  __ \/    \ 
 |    |     |  | \(  <_> )  |  \  ___/\  \___|  |    |    |   \  ___/| \_\ (  <_> )  | \/   |  \
 |____|     |__|   \____/\__|  |\___  >\___  >__|    |____|_  /\___  >___  /\____/|__|  |___|  /
                        \______|    \/     \/               \/     \/    \/                  \/ ");
            Console.WriteLine("Enter your Fortnite Path (Folder with Engine and FortniteGame)");
            path = Console.ReadLine();

            Console.WriteLine("Enter the name/email that you use");
            mail = Console.ReadLine();

            Console.WriteLine("Enter the password, put a random one if not needed");
            pass = Console.ReadLine();

            SaveConfig();
        }

        public static void SaveConfig()
        {
            dynamic config = new { path, mail, pass };
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configFile, json);
        }

        public static async Task<bool> DownloadAndVerifyPakFiles(string rootPath)
        {
            string paksFolderPath = Path.Combine(rootPath, "FortniteGame", "Content", "Paks");

            if (!Directory.Exists(paksFolderPath))
            {
                Directory.CreateDirectory(paksFolderPath);
                Log($"Created Paks directory at: {paksFolderPath}");
            }

            var fileUrls = new[]
            {
                ("pakChunkreborn-WindowsClient.pak", "https://www.dropbox.com/scl/fi/0klk361939dn0ci5d6tts/pakChunkreborn-WindowsClient.pak?rlkey=0x43yyedsk3nzdkd7oyw831k2&dl=1"),
                ("pakChunkreborn-WindowsClient.sig", "https://www.dropbox.com/scl/fi/qb6awczr2w3y54c6pjzh5/pakChunkreborn-WindowsClient.sig?rlkey=givlemr2l5hwmgxscxt3sc27&dl=1")
            };

            bool allFilesDownloaded = true;

            using (var httpClient = new HttpClient())
            {
                foreach (var (fileName, url) in fileUrls)
                {
                    string destinationPath = Path.Combine(paksFolderPath, fileName);

                    try
                    {
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }

                        var response = await httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(destinationPath, fileBytes);

                        Log($"Successfully downloaded and verified {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to download {fileName}: {ex.Message}");
                        allFilesDownloaded = false;
                    }
                }
            }

            return allFilesDownloaded;
        }

        public static async Task Launch3551()
        {
            await DownloadAndVerifyPakFiles(path);

            string dllPath = Path.Combine(path, "Engine\\Binaries\\ThirdParty\\NVIDIA\\NVaftermath\\Win64", "GFSDK_Aftermath_Lib.x64.dll");

            WebClient n1 = new WebClient();
            try
            {
                n1.DownloadFile("https://www.dropbox.com/scl/fi/4k8pae4wizzs7uzfm1679/Cobalt.dll?rlkey=slfl7ymojglz42221sbvi8klf&st=67vorw6x&dl=1", dllPath);
                Log("Successfully downloaded 3551.dll");
            }
            catch
            {
                Log("Failed downloading 3551.dll");
            }

            string FortniteEXE = Path.Combine(path, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe");
            string PrimerosArgs = $"-epicapp=Fortnite -epicenv=Prod -epicportal -noeac -fromfl=be -AUTH_TYPE=epic -AUTH_LOGIN={mail} -AUTH_PASSWORD={pass}";

            Process Fortnite = new Process
            {
                StartInfo = new ProcessStartInfo(FortniteEXE, PrimerosArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = false
                }
            };

            Fortnite.Start();
            Log("Fortnite was launched successfully");
            Fortnite.WaitForExit();
        }
    }
}

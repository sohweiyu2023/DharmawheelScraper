using Box.V2.Config;
using Box.V2.JWTAuth;
using Box.V2.Models;
using Box.V2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression; // Include this at the top of your file to use the ZipFile class
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace AutoScrapper
{
    class Program
    {
        private static readonly string appPaths = ConfigurationManager.AppSettings["ConsoleAppPaths"];
        private static readonly string pdfPath = ConfigurationManager.AppSettings["CombinedTextFilesToWordPath"];
        private static readonly string tableOfContentsPath = ConfigurationManager.AppSettings["TableOfContentsPath"];
        private static readonly string keywordsCategorisedWordPDFPath = ConfigurationManager.AppSettings["KeywordsCategorisedWordPDFPath"];
        private static readonly string uploadThesePath = ConfigurationManager.AppSettings["UploadThesePath"];
        private static readonly string malcolm1PartPath = ConfigurationManager.AppSettings["Malcolm1PartPath"];
        private static readonly string malcolm3PartsPath = ConfigurationManager.AppSettings["Malcolm3PartsPath"];
        private static readonly string malcolm12PartsPath = ConfigurationManager.AppSettings["Malcolm12PartsPath"];

        static async Task Main(string[] args)
        {
            string date = DateTime.Now.ToString("yyyyMMdd"); // YYYYMMDD format
            string logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs"); // Logs folder

            // Create Logs folder if it doesn't exist
            Directory.CreateDirectory(logsFolder);

            // Look for existing log files and find the highest number used so far
            int maxNumber = Directory
                .GetFiles(logsFolder, $"AutoScrapper_{date}_*.log")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => int.TryParse(name.Split('_').Last(), out int n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            // in case there are no existing log files, the Max method will be called on a sequence containing a single element 0, and thus, it will return 0.


            // Increment the max number by 1 for the new log file
            int newNumber = maxNumber + 1;

            // Log filename
            string logFileName = $"AutoScrapper_{date}_{newNumber}.log";

            // Full log file path
            string logFilePath = Path.Combine(logsFolder, logFileName);

            Trace.Listeners.Add(new TextWriterTraceListener(logFilePath));


            Trace.AutoFlush = true;

            // Get the paths to the console applications from the App.config file;
            string[] paths = appPaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);


            DeleteFilesInDirectory(uploadThesePath, ".zip");


            /*
            // Start all console applications in parallel
            List<Task> tasks = new List<Task>();
            foreach (string path in paths)
            {
                // Change this line
                tasks.Add(ProcessDirectory(path));
            }

            // Wait for all console applications to finish
            await Task.WhenAll(tasks);*/

            // Start all console applications sequentially
            foreach (string path in paths)
            {
                // Change this line
                await ProcessDirectory(path);
            }

            Console.WriteLine("All AutoScrapper tasks completed.");

            Trace.WriteLine($"{DateTime.Now} - All AutoScrapper tasks completed.");

            OpenFolder();
        }

        static void OpenFolder()
        {
            if (Directory.Exists(uploadThesePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = uploadThesePath,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }
            else
            {
                Console.WriteLine(string.Format("{0} Directory does not exist!", uploadThesePath));
                Trace.WriteLine($"{DateTime.Now} - " + string.Format("{0} Directory does not exist!", uploadThesePath));
            }
        }
        static async Task ProcessDirectory(string path)
        {
            try
            {

                string DharmaOrDhamma = "";

                if (path.ToLower().Contains("dhamma"))
                {
                    DharmaOrDhamma = "Dhamma";
                }
                else
                {
                    DharmaOrDhamma = "Dharma";
                }

                // Step 1: Run DharmawheelScraper
                Trace.WriteLine($"{DateTime.Now} - Running " + DharmaOrDhamma + "wheelScraper... path: " + path);
                Console.WriteLine("Running " + DharmaOrDhamma + "wheelScraper... path: " + path);

                var dirInfo = new DirectoryInfo(path);


                var latestFile = dirInfo.GetFiles(DharmaOrDhamma + "wheelScraper*.exe", SearchOption.AllDirectories)
                                        .OrderByDescending(f => f.LastWriteTime)
                                        .FirstOrDefault();

                if (latestFile != null)
                {
                    Console.WriteLine(latestFile.FullName);
                }
                else
                {
                    Console.WriteLine("No " + DharmaOrDhamma + "wheelScraper*.exe found.");
                    Trace.WriteLine($"{DateTime.Now} - No " + DharmaOrDhamma + "wheelScraper*.exe found.");
                    return;
                }


                await RunConsoleApp(latestFile.FullName, "yes");//path + "DharmawheelScraper.exe", "yes");

                Console.WriteLine("Starting StepsTwoToFourAsync");
                Trace.WriteLine($"{DateTime.Now} - Starting StepsTwoToFourAsync");

                string authorName = await StepsTwoToFourAsync(path);

                Console.WriteLine("Finished StepsTwoToFourAsync");
                Trace.WriteLine($"{DateTime.Now} - Finished StepsTwoToFourAsync");

                if (authorName == "failed!")
                {
                    Console.WriteLine("authorName == failed!");
                    Trace.WriteLine($"{DateTime.Now} - authorName == failed!");
                    return;
                }

                // Step 5: Delete old zip files and zip all the newly produced files from steps 1 to 4 in this way:
                // if the path contains @"DharmawheelScraper\DharmawheelScraper", zip it into "DharmawheelScraper_ForMalcolm.zip" in that folder.
                // For all other directories, zip it into "{folder name}.zip"
                // for example: for C:\Users\cyber\source\repos\DharmawheelScraper_ForKrodha\DharmawheelScraper\bin\Debug\net6.0 , zip all newly created contents into , zip it into "DharmawheelScraper_ForKrodha.zip", so on and so forth.
                Trace.WriteLine($"{DateTime.Now} - Deleting old zip files...");
                Console.WriteLine("Deleting old zip files...");
                DeleteFilesInDirectory(path, ".zip");

                Trace.WriteLine($"{DateTime.Now} - Zipping files...");
                Console.WriteLine("Zipping files...");

                string zipFileName = "";
                string zipFileName2 = "";
                string zipFileName3 = "";

                if (path.Contains(@"DharmawheelScraper-gpt-o1-preview\DharmawheelScraper\bin\Debug\net6.0"))
                {
                    zipFileName = malcolm1PartPath + ".zip";
                    zipFileName2 = malcolm3PartsPath + ".zip";
                    zipFileName3 = malcolm12PartsPath + ".zip";

                    string categorisedWordsAndPDFsPath = path + @"CategorisedWordsAndPDFs\";

                    string zipFileNameCategorisedWordsAndPDFs = "MalcolmCategorisedWordsAndPDFs.zip";
                    string zipFilePathCategorisedWordsAndPDFs = Path.Combine(categorisedWordsAndPDFsPath, zipFileNameCategorisedWordsAndPDFs);
                    ZipDirectory(categorisedWordsAndPDFsPath, zipFilePathCategorisedWordsAndPDFs, "*.*");

                    File.Move(zipFilePathCategorisedWordsAndPDFs, uploadThesePath + @"\" + Path.GetFileName(zipFilePathCategorisedWordsAndPDFs));


                    DistributeFiles(path, path + malcolm3PartsPath, 3, "Malcolm");

                    DistributeFiles(path, path + malcolm12PartsPath, 12, "Malcolm");

                    authorName = await StepsTwoToFourAsync(path + malcolm3PartsPath);

                    if (authorName == "failed!")
                    {
                        return;
                    }

                    authorName = await StepsTwoToFourAsync(path + malcolm12PartsPath);

                    if (authorName == "failed!")
                    {
                        return;
                    }

                    // Zip all relevant files into the new zip file
                    string zipFilePath = Path.Combine(path, zipFileName);
                    ZipDirectory(path, zipFilePath, "*.*");

                    // Step 6: Upload the zip file to Box.com. Not gonna work cos i only got free account
                    // So instead, just move it to the "Upload These" folder to manually upload.


                    File.Move(zipFilePath, uploadThesePath + @"\" + Path.GetFileName(zipFilePath));


                    // Zip all relevant files into the new zip file
                    string zipFilePath2 = Path.Combine(path + malcolm3PartsPath, zipFileName2);
                    ZipAllDirectories(path + malcolm3PartsPath, zipFilePath2, "*.*");

                    // Step 6: Upload the zip file to Box.com. Not gonna work cos i only got free account
                    // So instead, just move it to the "Upload These" folder to manually upload.


                    File.Move(zipFilePath2, uploadThesePath + @"\" + Path.GetFileName(zipFilePath2));

                    // Zip all relevant files into the new zip file
                    string zipFilePath3 = Path.Combine(path + malcolm12PartsPath, zipFileName3);
                    ZipAllDirectories(path + malcolm12PartsPath, zipFilePath3, "*.*");

                    // Step 6: Upload the zip file to Box.com. Not gonna work cos i only got free account
                    // So instead, just move it to the "Upload These" folder to manually upload.


                    File.Move(zipFilePath3, uploadThesePath + @"\" + Path.GetFileName(zipFilePath3));
                }
                else
                {
                    zipFileName = DharmaOrDhamma + "wheelScraper_For" + authorName + ".zip";


                    // Zip all relevant files into the new zip file
                    string zipFilePath = Path.Combine(path, zipFileName);
                    ZipDirectory(path, zipFilePath, "*.*");

                    // Step 6: Upload the zip file to Box.com. Not gonna work cos i only got free account
                    // So instead, just move it to the "Upload These" folder to manually upload.


                    File.Move(zipFilePath, uploadThesePath + @"\" + Path.GetFileName(zipFilePath));
                }


                /*
                Trace.WriteLine($"{DateTime.Now} - Uploading files to Box...");
                Console.WriteLine("Uploading files to Box...");
                await UploadToBox(zipFileName);
                */

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in ProcessDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in ProcessDirectory: {ex.Message}\nStack trace: {ex.StackTrace}"); ;
            }
        }

        public static async Task<string> StepsTwoToFourAsync(string path)
        {

            Trace.WriteLine($"{DateTime.Now} - Running StepsTwoToFourAsync() for " + path + "...");
            Console.WriteLine($"{DateTime.Now} - Running StepsTwoToFourAsync() for " + path + "...");
            // Step 2: Delete Word and PDF files
            Trace.WriteLine($"{DateTime.Now} - Deleting Word and PDF files...");
            Console.WriteLine("Deleting Word and PDF files...");
            DeleteFilesInDirectory(path, ".docx", ".pdf");

            // Step 3: Run CombinePDFWord

            string directoryThreeLevelsUp = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path))));
            string programPath = Path.Combine(directoryThreeLevelsUp, "Program.cs");

            Console.WriteLine("Searching program.cs in " + programPath);
            Trace.WriteLine("Searching program.cs in " + programPath);
            string authorName = null;

            if (File.Exists(programPath))
            {
                Console.WriteLine("File exists in " + programPath);
                Trace.WriteLine("File exists in " + programPath);
                var lines = File.ReadLines(programPath);
                var authorLine = lines.FirstOrDefault(line => line.Contains("private const string AuthorName"));

                if (authorLine != null)
                {
                    int start = authorLine.IndexOf("\"") + 1; // start of the author name
                    int end = authorLine.LastIndexOf("\""); // end of the author name

                    authorName = authorLine.Substring(start, end - start);
                }
            }
            else
            {
                string directoryTwoLevelsUp = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                programPath = Path.Combine(directoryTwoLevelsUp, "Program.cs");


                Console.WriteLine("File doesn't exists in " + programPath + ", searching in " + programPath);
                Trace.WriteLine("File doesn't exists in " + programPath + ", searching in " + programPath);

                var lines = File.ReadLines(programPath);
                var authorLine = lines.FirstOrDefault(line => line.Contains("private const string AuthorName"));

                if (authorLine != null)
                {
                    int start = authorLine.IndexOf("\"") + 1; // start of the author name
                    int end = authorLine.LastIndexOf("\""); // end of the author name

                    authorName = authorLine.Substring(start, end - start);
                }
            }

            if (authorName == null)
            {
                Console.WriteLine("Cannot find authorname from program.cs!");
                Trace.WriteLine($"{DateTime.Now} - Cannot find authorname from program.cs!");
                return "failed!";
            }

            Trace.WriteLine($"{DateTime.Now} - Running CombinePDFWord...");
            Console.WriteLine("Running CombinePDFWord...");
            if (path.Contains(malcolm3PartsPath) || path.Contains(malcolm12PartsPath))
            {
                string[] subDirectories = Directory.GetDirectories(path);
                foreach (string subdir in subDirectories)
                {
                    await RunConsoleAppCombinePDFWord(authorName, pdfPath, subdir);

                    Trace.WriteLine($"{DateTime.Now} - Running DharmawheelTableOfContentsCreator... for " + subdir);
                    Console.WriteLine("Running DharmawheelTableOfContentsCreator... for " + subdir);
                    await RunConsoleAppDharmawheelTableOfContentsCreator(tableOfContentsPath, subdir);
                }
            }
            else
            {
                await RunConsoleAppCombinePDFWord(authorName, pdfPath, path);
            }

            // Step Optional: If Malcolm, re generate categorised word PDFs
            if (path.Contains(@"DharmawheelScraper\DharmawheelScraper") && !path.Contains(malcolm3PartsPath) && !path.Contains(malcolm12PartsPath))
            {

                string categorisedWordsAndPDFsPath = path + @"CategorisedWordsAndPDFs\";

                if (Directory.Exists(categorisedWordsAndPDFsPath))
                    Directory.Delete(categorisedWordsAndPDFsPath, true);

                Directory.CreateDirectory(categorisedWordsAndPDFsPath);


                Trace.WriteLine($"{DateTime.Now} - Running KeywordsCategorisedWordPDF...");
                Console.WriteLine("Running KeywordsCategorisedWordPDF...");
                await RunConsoleAppKeywordsCategorisedWordPDF(keywordsCategorisedWordPDFPath, path);


                // Step 4: Run DharmawheelTableOfContentsCreator

                Trace.WriteLine($"{DateTime.Now} - Running DharmawheelTableOfContentsCreator...");
                Console.WriteLine("Running DharmawheelTableOfContentsCreator...");
                await RunConsoleAppDharmawheelTableOfContentsCreator(tableOfContentsPath, categorisedWordsAndPDFsPath);
            }

            // Step 4: Run DharmawheelTableOfContentsCreator
            Trace.WriteLine($"{DateTime.Now} - Running DharmawheelTableOfContentsCreator...");
            Console.WriteLine("Running DharmawheelTableOfContentsCreator...");
            await RunConsoleAppDharmawheelTableOfContentsCreator(tableOfContentsPath, path);

            return authorName;
        }
        public static void ZipDirectory(string sourceDirectoryName, string destinationArchiveFileName, string searchPattern)
        {
            try
            {
                sourceDirectoryName = sourceDirectoryName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var sourceDirectory = new DirectoryInfo(sourceDirectoryName);

                using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
                {
                    //foreach (var file in sourceDirectory.GetFiles(searchPattern, SearchOption.AllDirectories)
                    foreach (var file in sourceDirectory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
                      .Where(file => file.FullName.ToLower().EndsWith("docx") || file.FullName.ToLower().EndsWith("pdf") || file.FullName.ToLower().EndsWith("txt")))
                    {
                        var relativePath = file.FullName.Substring(sourceDirectoryName.Length);
                        archive.CreateEntryFromFile(file.FullName, relativePath);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        public static void ZipAllDirectories(string sourceDirectoryName, string destinationArchiveFileName, string searchPattern)
        {
            int retryCount = 5;
            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                try
                {
                    sourceDirectoryName = sourceDirectoryName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    var sourceDirectory = new DirectoryInfo(sourceDirectoryName);

                    Trace.WriteLine($"{DateTime.Now} - Attempt {attempt} - Starting to create: {destinationArchiveFileName}");
                    using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
                    {
                        foreach (var file in sourceDirectory.GetFiles(searchPattern, SearchOption.AllDirectories)
                          .Where(file => file.FullName.ToLower().EndsWith("docx") || file.FullName.ToLower().EndsWith("pdf") || file.FullName.ToLower().EndsWith("txt")))
                        {
                            var relativePath = file.FullName.Substring(sourceDirectoryName.Length);
                            archive.CreateEntryFromFile(file.FullName, relativePath);
                        }
                    }

                    // If no exception has been thrown, the file has been successfully created, so we can break the loop

                    if (File.Exists(destinationArchiveFileName))
                    {
                        Trace.WriteLine($"{DateTime.Now} - Finished creating: {destinationArchiveFileName}");
                        break;
                    }
                    else
                    {
                        Trace.WriteLine($"{DateTime.Now} - File does not exist! Unable to finish creating in attempt {attempt}: {destinationArchiveFileName}");
                    }

                }
                catch (IOException ex)
                {
                    Console.WriteLine($"An IO error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
                    Trace.WriteLine($"{DateTime.Now} - An IO error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");

                    // If there was an IO exception (like file lock), wait for a bit before trying again
                    System.Threading.Thread.Sleep(2000); // Wait for 2 seconds
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
                    Trace.WriteLine($"{DateTime.Now} - An error occurred in ZipDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");

                    // For any other type of exception, we probably can't recover by simply retrying, so we should just fail immediately
                    break;
                }
            }
        }


        static void DeleteFilesInDirectory(string directoryPath, params string[] fileExtensions)
        {
            try
            {
                int retryCount = 0;
                foreach (var fileExtension in fileExtensions)
                {
                    string[] filesToDelete = Directory.GetFiles(directoryPath, "*" + fileExtension);
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            Trace.WriteLine($"{DateTime.Now} - Deleting file: {file}...");
                            Console.WriteLine($"Deleting file: {file}...");
                            File.Delete(file);
                        }
                        catch (IOException ex)
                        {
                            Trace.WriteLine($"{DateTime.Now} - Failed to delete {file} on attempt {retryCount}. Exception: {ex.Message}\nStack trace: {ex.StackTrace}");

                            if (retryCount < 3) // if not the last attempt
                            {
                                retryCount++;
                                KillWordProcesses();
                                System.Threading.Thread.Sleep(1000); // Wait before next attempt
                            }
                            else // If this was the last attempt and it still failed, rethrow the exception
                            {
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in DeleteFilesInDirectory: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in DeleteFilesInDirectory: {ex.Message}\nStack trace: {ex.StackTrace}"); ;
            }
        }
        private static void KillWordProcesses()
        {
            try
            {

                foreach (var process in Process.GetProcesses())
                {
                    if (process.ProcessName.ToLower().Contains("winword"))
                    {
                        try
                        {
                            process.Kill();
                            Trace.WriteLine($"{DateTime.Now} - Successfully terminated Word process with ID {process.Id}.");
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"{DateTime.Now} - Failed to terminate Word process with ID {process.Id}. Exception: {ex.Message}\nStack trace: {ex.StackTrace}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in KillWordProcesses: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in KillWordProcesses: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        static async Task RunConsoleApp(string path, string input)
        {
            try
            {
                Process process = new Process();

                process.StartInfo.FileName = path;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);

                process.Start();

                // Write the input to the console app's standard input stream
                using (StreamWriter sw = process.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(input);
                    }
                }

                // Read the output of the console app and display it in the AutoScrapper console
                while (!process.StandardOutput.EndOfStream)
                {
                    string output = await process.StandardOutput.ReadLineAsync();
                    Trace.WriteLine($"[{DateTime.Now} - {path}]: {output}");
                    Console.WriteLine($"[{path}]: {output}");
                }

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in RunConsoleApp: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in RunConsoleApp: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        static async Task RunConsoleAppCombinePDFWord(string authorName, string appPath, string directoryPath)
        {
            try
            {

                Process process = new Process();

                process.StartInfo.FileName = appPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(appPath);

                process.Start();

                // Send required inputs to the console app
                using (StreamWriter sw = process.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(authorName); // author name
                        sw.WriteLine(directoryPath); // directory path
                        sw.WriteLine("n"); // process subdirectories: no
                        sw.WriteLine("b"); // order files: both
                    }
                }

                // Read the output of the console app and display it in the AutoScrapper console
                while (!process.StandardOutput.EndOfStream)
                {
                    string output = await process.StandardOutput.ReadLineAsync();
                    Trace.WriteLine($"[{DateTime.Now} - {appPath}]: {output}");
                    Console.WriteLine($"[{appPath}]: {output}");
                }

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in RunConsoleAppCombinePDFWord: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in RunConsoleAppCombinePDFWord: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        static async Task UploadToBox(string filePath)
        {
            try
            {

                var boxConfig = new BoxConfig(
                    clientId: "3och6wlzh5kn4063vzcqi340czvhbwzm",
                    clientSecret: "2UuMvV5O9k0xleUe1qJ8zvDHo0MJQiCp",
                    enterpriseId: "0",
                    jwtPrivateKey: "-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIFDjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQIqlzQFOI2CRECAggA\nMBQGCCqGSIb3DQMHBAjZeMBCKMUWmASCBMiKz3lYUuEdbJu0pbqoicJA1WYRLFdE\nsvx+MIRP9Aq0I2c+ipcA30A+9q7WjvhtUE+JAZCQQddQwSj3Icy9A+EngxC7Bjti\nnT99urFj2kZrAgctt6GgPZHrbLwlkp6JA/fiOQtVoNeCNxZ3Iobh65GzEqtRuTAh\nm2X9AP6as1cFenCY9JnT1JR58kZjksLP0jWBDTiKa6OFGSKq9atw+jEGiIpCSNJ2\nNnIwKXozlsXsa+iTisG37BtNikissaNyeaPu+WN/WR15T+SnrvLX5//vpxo+rBIz\n0LEmt8YhOB0hbd2V29+JhKDAnIN9cnXESB4X41smDdIFqkdH5lIyagCf6rv3P4SH\ngIbOqOKiMw2UC1rVoCRjaQMser55PvPgqAgWhh2I8qJBTSYcD3DTaF8b0X7gK6bq\n/BubIdedCK2GSxJQefq2kk9w6rjGKlVUCQIEICFHDqRJO4aWRVW8Nxm2RfHQZfUG\nVAWKpRaI05XT1HsxMoOKXqGBBW2RzYBuLEJCj9JFWRO+6MUgBfbp5loDt6G1pY88\n99ge9pVbLweujteQc6p8Msh3S3z3GVRsxqacfJ+1FzEgtzDALGIXoC1SYNDPOLVn\nBfHHwJZA3crySWcjCHpiBvpeF35xu23xf3IkD98sWcMseJcRM2gX1+bYO5fK/NMJ\nyZByevRKCRQCpRk2GIt+X7Zwwb2qHgcZJON8GCtBBM4LIdXJPEqpyW6T1wh5vb/j\nphL9yuQgoP+N3k1GStXY8ZCVz7P+Em0H7r1X8UPGX5/PSMSSrZOOlbmeE5pNJ7i/\nWkqdhDcM2+L6FOjqjpK3q3WAZT4+G/XryhUkq6DWw57EX6PGNGiQbutEzlr1OKv+\nT0JehAxtfGE8nBmJ67uv6d4c0VsnpBrIZBZ7AbuL6bdd+lP3pnBtvLPb6fWeykOD\ndK62O/ztQHY1MOVltsnNsABR8Yndod4KsEfM8r66sxYqyKN6fsyR/acBivtcird3\nJrq2SnFQ6eTxgF2V78ZXXkW7O0jkx6OD+Vnoud5lxSqmdle+nUe6zGMV3oTZO9yE\nOPhX9tlYElg1yyt6ZrvKi2RkIgoIkYIEna7QlDVko3Q39pl0YN8EvZNUEoiW6Rbi\ni7FBY/d516h97GCY0dXQEYdl8w5Sm1tIY1pUZgms1PuLnVUndivbu0DzRSfEBzPf\nGRaUgXPGGyI9gbwR20+k3uM3SVD4aDKU8j0xtQjRCrfIq1YkatBBBLkso7jcPC30\n6DAgMSJBY3cXkY2q3r4FShPBE8Fl0AQGllaYj3aBggy9hH44qjj42mLF4GNicXy+\nV0FGyWm1lCf3tHaaOTHQaz+b1Im4FtHjSyQ/xmTiy3hQTJ90v3qnZv6MQgjl5uN0\nRaMf0HunqspIQnq6c3XAHCUDZhaqlGMulTFFg0lO4kCKylOSOCZjE53kVWE7DYtB\nBYGutgqYfDu4SkmgoWxOLHbrJrvo7gy1oaWI9PlOOMXb8mq2xXh5VyF/eybICJZf\nPI/++v2SHjg4As5vBJ743HUD+6Dlez05DtX4JyT2yyP0KmE4ujhV4mZlOl/j1QaS\nKPKpz/m1Gt2s+Z6Sz1k3Zr5svvHmR22OWQtD6fjhNVD6il5HTmKtEc4EySZECTNj\n2+Y=\n-----END ENCRYPTED PRIVATE KEY-----\n",
                    jwtPrivateKeyPassword: "1853b70328cb51a055a409d73e0d2323",
                    jwtPublicKeyId: "niyq5uca"
                );

                var boxJWT = new BoxJWTAuth(boxConfig);

                var token = await boxJWT.AdminTokenAsync();

                var adminClient = boxJWT.AdminClient(token);

                /* foreach (var filePath in filePaths)
                 {*/
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var fileName = Path.GetFileName(filePath);
                    var files = await adminClient.SearchManager.QueryAsync(fileName, limit: 1);
                    BoxFile file;

                    if (files.TotalCount > 0)
                    {
                        file = await adminClient.FilesManager.UploadNewVersionAsync(files.Entries[0].Id, Path.GetFileName(filePath), fileStream);

                        Trace.WriteLine($"{DateTime.Now} - Updated file {file.Name} to Box.");
                    }
                    else
                    {
                        var request = new BoxFileRequest()
                        {
                            Name = fileName,
                            Parent = new BoxRequestEntity() { Id = "0" }
                        };
                        file = await adminClient.FilesManager.UploadAsync(request, fileStream);
                        Trace.WriteLine($"{DateTime.Now} - Uploaded file {file.Name} to Box.");
                    }
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in UploadToBox: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in UploadToBox: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        static async Task RunConsoleAppKeywordsCategorisedWordPDF(string appPath, string directoryPath)
        {
            try
            {
                Process process = new Process();

                process.StartInfo.FileName = appPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(appPath);

                process.Start();

                // Send required inputs to the console app
                using (StreamWriter sw = process.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(directoryPath); // directory path
                        sw.WriteLine("both"); // ascending/descending/both: both
                        sw.WriteLine("yes to all"); // file already exists. Replace it? (yes/no/yes to all/no to all): yes to all
                    }
                }

                // Read the output of the console app and display it in the AutoScrapper console
                while (!process.StandardOutput.EndOfStream)
                {
                    string output = await process.StandardOutput.ReadLineAsync();
                    Trace.WriteLine($"[{DateTime.Now} - {appPath}]: {output}");
                    Console.WriteLine($"[{appPath}]: {output}");
                }

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in RunConsoleAppKeywordsCategorisedWordPDF: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in RunConsoleAppKeywordsCategorisedWordPDF: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        static async Task RunConsoleAppDharmawheelTableOfContentsCreator(string appPath, string directoryPath)
        {
            try
            {

                Process process = new Process();

                process.StartInfo.FileName = appPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(appPath);

                process.Start();

                // Send required inputs to the console app
                using (StreamWriter sw = process.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine("1"); // process subdirectories: yes
                        sw.WriteLine(directoryPath); // directory path
                        sw.WriteLine("y"); // proceed: yes
                    }
                }

                // Read the output of the console app and display it in the AutoScrapper console
                while (!process.StandardOutput.EndOfStream)
                {
                    string output = await process.StandardOutput.ReadLineAsync();
                    Trace.WriteLine($"[{DateTime.Now} - {appPath}]: {output}");
                    Console.WriteLine($"[{appPath}]: {output}");
                }

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in RunConsoleAppDharmawheelTableOfContentsCreator: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in RunConsoleAppDharmawheelTableOfContentsCreator: {ex.Message}\nStack trace: {ex.StackTrace}"); ;
            }
        }

        public static void DistributeFiles(string sourceDir, string targetDir, int numberOfFolders, string author)
        {
            var files = Directory.GetFiles(sourceDir, $"{author}_posts_*.txt")
                        .Select(x => new
                        {
                            FileName = x,
                            Number = int.Parse(Regex.Match(Path.GetFileName(x), @"(?<=_posts_)\d+").Value)
                        })
                        .OrderBy(x => x.Number)
                        .Select(x => x.FileName)
                        .ToArray();

            long totalSizeBytes = files.Sum(f => new FileInfo(f).Length);
            long maxFolderSizeBytes = totalSizeBytes / numberOfFolders;

            long currentSize = 0;
            int folderIndex = 0;
            int fileStartIndex = 0;

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
                Trace.WriteLine($"[{DateTime.Now} - Directory.Delete]: {targetDir}");
                Console.WriteLine($"[{DateTime.Now} - Directory.Delete]: {targetDir}");
            }

            Directory.CreateDirectory(targetDir);

            for (int i = 0; i < files.Length; i++)
            {
                var fileInfo = new FileInfo(files[i]);
                currentSize += fileInfo.Length;

                // If size limit is exceeded or if this is the last file, create new folder and move files
                if (currentSize > maxFolderSizeBytes || i == files.Length - 1)
                {
                    var startNumber = Regex.Match(Path.GetFileName(files[fileStartIndex]), @"(?<=_posts_)\d+").Value;
                    var endNumber = Regex.Match(Path.GetFileName(files[i]), @"(?<=_posts_)\d+").Value;
                    var newFolder = Path.Combine(targetDir, $"{author}_{folderIndex:D2}_{startNumber}to{endNumber}");

                    Directory.CreateDirectory(newFolder);

                    for (int j = fileStartIndex; j <= i; j++)
                    {
                        var fileName = Path.GetFileName(files[j]);
                        File.Copy(files[j], Path.Combine(newFolder, fileName), true);
                    }

                    // Reset the current size counter and start a new folder
                    currentSize = 0;
                    folderIndex++;
                    fileStartIndex = i + 1;
                }
            }
        }

    }
}

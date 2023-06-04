using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Word = Microsoft.Office.Interop.Word;

namespace KeywordsCategorisedWordPDF
{
    class Program
    {
        static void Main(string[] args)
        {
            string authorName = "Malcolm";
            var keywords = File.ReadAllLines("Keywords.txt");

            Console.WriteLine("Enter folder path:");
            string folderPath;
            while (true)
            {
                folderPath = Console.ReadLine();
                if (Directory.Exists(folderPath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid directory. Please enter an existing directory:");
                }
            }

            // Create new subdirectory
            string newFolderPath = Path.Combine(folderPath, "CategorisedWordsAndPDFs");
            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
            }
            else
            {
                Directory.Delete(newFolderPath);
                Directory.CreateDirectory(newFolderPath);
            }

            Console.WriteLine($"Processing folder {folderPath}...");

            var textFiles = Directory.GetFiles(folderPath, "*.txt");

            Console.WriteLine($"Found {textFiles.Length} files.");

            var keywordDocs = keywords.ToDictionary(keyword => keyword, keyword => new List<string>());
            var synonymDict = keywords.ToDictionary(keyword => keyword, keyword => keyword.Split('/').Select(s => s.Trim()).ToList());

            // Processing order
            string[] orders = null;
            while (true)
            {
                Console.WriteLine("Do you want to create files in Ascending order, Descending order or Both?");
                string orderInput = Console.ReadLine().Trim().ToLower();
                if (orderInput == "ascending")
                {
                    orders = new string[] { "Ascending" };
                    break;
                }
                else if (orderInput == "descending")
                {
                    orders = new string[] { "Descending" };
                    break;
                }
                else if (orderInput == "both")
                {
                    orders = new string[] { "Ascending", "Descending" };
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter either 'Ascending', 'Descending', or 'Both'.");
                }
            }

            string replaceOrSkipAll = null;
            foreach (var textFile in textFiles)
            {
                Console.WriteLine($"Processing {textFile}.");
                var currentPostLines = File.ReadAllLines(textFile);

                for (var i = 0; i < currentPostLines.Length; i++)
                {
                    if (currentPostLines[i].StartsWith("Author:"))
                    {
                        var post = new StringBuilder();
                        post.AppendLine(currentPostLines[i]); // Author line
                        post.AppendLine(currentPostLines[i + 1]); // Date line
                        post.AppendLine(currentPostLines[i + 2]); // Title line
                        post.AppendLine(currentPostLines[i + 3]); // Content line

                        // add all subsequent lines to Content until next Author line is encountered
                        var j = i + 4;
                        while (j < currentPostLines.Length && !currentPostLines[j].StartsWith("Author:"))
                        {
                            post.AppendLine(currentPostLines[j]);
                            j++;
                        }

                        var postString = post.ToString();

                        // Check for keywords and synonyms in each post
                        foreach (var keyword in keywords)
                        {
                            var synonyms = synonymDict[keyword];
                            foreach (var synonym in synonyms)
                            {
                                // Use regular expressions for case-insensitive complete word match
                                if (Regex.IsMatch(postString, $@"\b{synonym}\b", RegexOptions.IgnoreCase))
                                {
                                    keywordDocs[keyword].Add(postString);
                                    break;  // if a synonym is found, no need to check the other synonyms
                                }
                            }
                        }

                        i = j - 1; // adjust index to start of next post
                    }
                }
            }

            // After processing all files, sort the posts in each keyword document
            foreach (var keyword in keywords)
            {
                var posts = keywordDocs[keyword];
                string[] keywordsArr = keyword.Split('/');
                string sanitizedKeyword = string.Join("-", keywordsArr.Take(3));

                foreach (var order in orders)
                {
                    List<string> sortedPosts;
                    if (order == "Ascending")
                    {
                        sortedPosts = posts
                            .Select(post =>
                            {
                                var lines = post.Split('\n');
                                var dateString = lines[1].Substring(6); // remove "Date: " prefix
                                var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
                                return new { Post = post, Date = date };
                            })
                            .OrderBy(x => x.Date) // sort by date in ascending order
                            .Select(x => x.Post)
                            .ToList();
                    }
                    else // order == "Descending"
                    {
                        sortedPosts = posts
                            .Select(post =>
                            {
                                var lines = post.Split('\n');
                                var dateString = lines[1].Substring(6); // remove "Date: " prefix
                                var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
                                return new { Post = post, Date = date };
                            })
                            .OrderByDescending(x => x.Date) // sort by date in descending order
                            .Select(x => x.Post)
                            .ToList();
                    }

                    string wordFilePath = Path.Combine(newFolderPath, $"Malcolm_{sanitizedKeyword}_{order}.docx");
                    string pdfFilePath = Path.Combine(newFolderPath, $"Malcolm_{sanitizedKeyword}_{order}.pdf");

                    WriteEntriesToWordFile(sortedPosts, wordFilePath, ref replaceOrSkipAll);
                    SaveWordToPdf(wordFilePath, pdfFilePath, ref replaceOrSkipAll);
                }


            }
        }
        private static void WriteEntriesToWordFile(List<string> entries, string filePath, ref string replaceOrSkipAll)
        {

            int retryCount = 0;
            try
            {

                if (File.Exists(filePath))
                {
                    ReplaceOrSkipFile(filePath, ref replaceOrSkipAll);
                    if (replaceOrSkipAll.StartsWith("no"))
                    {
                        return;
                    }
                }

                using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    foreach (string entry in entries)
                    {
                        string[] lines = entry.Split('\n');

                        Paragraph paragraph = body.AppendChild(new Paragraph());
                        Run run = paragraph.AppendChild(new Run());

                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Author:"))
                            {
                                paragraph = body.AppendChild(new Paragraph());
                                run = paragraph.AppendChild(new Run());
                            }
                            run.AppendChild(new Text(line));
                            run.AppendChild(new Break());
                        }
                    }
                    mainPart.Document.Save();


                    Console.WriteLine("Saved " + filePath);


                }


            }
            catch (IOException ex)
            {
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

        private static void SaveWordToPdf(string wordFilePath, string pdfFilePath, ref string replaceOrSkipAll)
        {
            int retryCount = 0;
            if (File.Exists(pdfFilePath))
            {
                ReplaceOrSkipFile(pdfFilePath, ref replaceOrSkipAll);
                if (replaceOrSkipAll.StartsWith("no"))
                {
                    return;
                }
            }

            Word.Application application = new Word.Application();
            Word.Document document = null;

            try
            {
                document = application.Documents.Open(wordFilePath);
                document.SaveAs2(pdfFilePath, Word.WdSaveFormat.wdFormatPDF);


                Console.WriteLine("Saved " + pdfFilePath);
            }
            catch (IOException ex)
            {
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
            finally
            {
                document?.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
                application.Quit();
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
                            Trace.WriteLine($"{DateTime.Now} - Failed to terminate Word process with ID {process.Id}. Exception: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in KillWordProcesses: {ex.Message}");
                // Log the exception as needed
                Trace.WriteLine($"{DateTime.Now} - An error occurred in KillWordProcesses: {ex.Message}");
            }
        }
        private static void ReplaceOrSkipFile(string filePath, ref string replaceOrSkipAll)
        {
            if (replaceOrSkipAll == "yes to all")
            {
                File.Delete(filePath);
            }
            else if (replaceOrSkipAll == "no to all")
            {
                return;
            }
            else
            {
                Console.WriteLine($"File {filePath} already exists. Replace it? (yes/no/yes to all/no to all)");
                var response = Console.ReadLine().ToLower();

                // Handle invalid input
                while (response != "yes" && response != "no" && response != "yes to all" && response != "no to all")
                {
                    Console.WriteLine("Invalid input. Please enter 'yes', 'no', 'yes to all' or 'no to all'.");
                    response = Console.ReadLine().ToLower();
                }

                replaceOrSkipAll = response;

                if (response == "yes" || response == "yes to all")
                {
                    File.Delete(filePath);
                }
            }
        }


    }
}

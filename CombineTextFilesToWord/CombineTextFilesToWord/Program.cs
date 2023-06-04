using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Word = Microsoft.Office.Interop.Word; // Add this line

namespace CombineTextFilesToWord
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter author's username (caps sensitive, e.g. Malcolm, krodha, etc):");
            string authorName = Console.ReadLine();

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

            Console.WriteLine("Do you want to process each subdirectory within the parent folder? (y/n):");
            string processSubdirectories = Console.ReadLine().ToLower();

            if (processSubdirectories == "y")
            {
                // Process each subdirectory
                string[] subdirectories = Directory.GetDirectories(folderPath);
                foreach (string folderPathX in subdirectories)
                {
                    string[] files = Directory.GetFiles(folderPathX, $"{authorName}_posts_*.txt");
                    if (files.Count() > 0)
                    {
                        ProcessFolder(folderPathX, "A", authorName);
                        ProcessFolder(folderPathX, "D", authorName);
                    }
                }
            }
            else
            {


                Console.WriteLine("Order files (A)scending or (D)escending or (B)oth? A = start from smallest file number. D = start from largest file number.");
                string sortOrder;
                while (true)
                {
                    sortOrder = Console.ReadLine().ToUpper();

                    if (sortOrder == "A" || sortOrder == "D")
                    {
                        // Process only the parent folder
                        Console.WriteLine("Starting to process " + folderPath);
                        ProcessFolder(folderPath, sortOrder, authorName);
                        break;
                    }
                    else if (sortOrder == "B")
                    {
                        // Process only the parent folder
                        Console.WriteLine("Starting to process " + folderPath);
                        ProcessFolder(folderPath, "A", authorName);
                        ProcessFolder(folderPath, "D", authorName);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter (A)scending or (D)escending or (B)oth:");
                    }
                }


            }
        }

        public static void ProcessFolder(string folderPath, string sortOrder, string authorName)
        {

            string[] files = Directory.GetFiles(folderPath, $"{authorName}_posts_*.txt");

            // Sort the files based on their numeric part
            // Sort the files based on their numeric part
            Array.Sort(files, (x, y) =>
            {
                Match xMatch = Regex.Match(Path.GetFileName(x), $@"{authorName}_posts_(\d+)\.txt");
                Match yMatch = Regex.Match(Path.GetFileName(y), $@"{authorName}_posts_(\d+)\.txt");

                Match xDateMatch = Regex.Match(Path.GetFileName(x), $@"{authorName}_posts_0_\d+-\d+-\d+\.txt");
                Match yDateMatch = Regex.Match(Path.GetFileName(y), $@"{authorName}_posts_0_\d+-\d+-\d+\.txt");

                int xNumber = xMatch.Success ? int.Parse(xMatch.Groups[1].Value) : (xDateMatch.Success ? 0 : int.MaxValue);
                int yNumber = yMatch.Success ? int.Parse(yMatch.Groups[1].Value) : (yDateMatch.Success ? 0 : int.MaxValue);
                return xNumber.CompareTo(yNumber);
            });


            if (sortOrder == "D")
            {
                Array.Reverse(files);
            }


            // Find a unique file name for the Word document
            int uniqueNumber = 1;
            string baseFileName = $"{authorName}CombinedPosts_" + (sortOrder == "D" ? "SortByAscendingDate" : "SortByDescendingDate");
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(folderPath, $"{baseFileName}_{uniqueNumber}.docx");
                uniqueNumber++;
            }
            while (File.Exists(newFilePath));
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(newFilePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                DocumentFormat.OpenXml.Wordprocessing.Body body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

                // Add the code snippet here
                // Define the default paragraph style
                Style style = new Style()
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "DefaultParagraphStyle",
                    Default = true
                };

                // Set the spacing properties for the default paragraph style
                style.Append(new StyleParagraphProperties(
                    new SpacingBetweenLines { After = "0", Before = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                ));

                // Create the StyleDefinitionsPart and add the default paragraph style
                StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
                stylePart.Styles = new Styles();
                stylePart.Styles.Append(style);
                stylePart.Styles.Save();

                foreach (string file in files)
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (string line in lines)
                    {
                        DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
                        paragraph.Append(new Run(new Text(line)));
                        paragraph.ParagraphProperties = new ParagraphProperties(
                            new ParagraphStyleId { Val = "DefaultParagraphStyle" },
                            new Justification { Val = JustificationValues.Left }
                        );
                        body.AppendChild(paragraph);
                    }

                    //  body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()); // Add an empty paragraph as separator
                }

                mainPart.Document.Save();
            }


            if (sortOrder == "D")
            {
                ProcessAndReplaceOriginalFile(newFilePath, false); // put opposite of descending due to definitional difference. false = For files that start with oldest date (descending), true = for those that start with latest date (ascending)
            }

            if (sortOrder == "A")
            {
                ProcessAndReplaceOriginalFile(newFilePath, true); // put opposite of ascending due to definitional difference. false = For files that start with oldest date (descending), true = for those that start with latest date (ascending)
            }

            Console.WriteLine(newFilePath + " has been created.");
            string pdfFilePath = Path.ChangeExtension(newFilePath, ".pdf");
            SaveWordToPdf(newFilePath, pdfFilePath);
            Console.WriteLine(pdfFilePath + " has been created.");


        }
        private static void SaveWordToPdf(string wordFilePath, string pdfFilePath)
        {
            int retryCount = 0;

            Word.Application application = new Word.Application();
            Word.Document document = null;

            try
            {
                document = application.Documents.Open(wordFilePath);
                document.SaveAs2(pdfFilePath, Word.WdSaveFormat.wdFormatPDF);
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
                if (document != null)
                {
                    document.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
                }

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
        private static List<string> ReadEntriesFromWordFile(string filePath)
        {
            List<string> entries = new List<string>();

            using (WordprocessingDocument document = WordprocessingDocument.Open(filePath, false))
            {
                DocumentFormat.OpenXml.Wordprocessing.Body body = document.MainDocumentPart.Document.Body;
                string[] paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText).ToArray();
                string entrySeparator = "Author: ";

                string entryContent = string.Join("\n", paragraphs).Replace("\r", "");
                string[] entryArray = entryContent.Split(new[] { entrySeparator }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < entryArray.Length; i++)
                {
                    entries.Add(entrySeparator + entryArray[i]);
                }
            }

            return entries;
        }
        public static void ProcessAndReplaceOriginalFile(string inputFilePath, bool descending)
        {
            List<string> entries = ReadEntriesFromWordFile(inputFilePath);
            List<string> sortedEntries = SortEntriesByDate(entries, descending);

            string tempFilePath = Path.GetTempFileName();

            try
            {
                WriteEntriesToWordFile(sortedEntries, tempFilePath);

                // Replace the original file with the sorted file
                File.Delete(inputFilePath);
                File.Move(tempFilePath, inputFilePath);
            }
            finally
            {
                // Clean up the temporary file if it still exists
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        private static void WriteEntriesToWordFile(List<string> entries, string filePath)
        {
            using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                DocumentFormat.OpenXml.Wordprocessing.Body body = new DocumentFormat.OpenXml.Wordprocessing.Body();
                mainPart.Document.Append(body);

                foreach (string entry in entries)
                {
                    string[] lines = entry.Split('\n');
                    DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
                    paragraph.ParagraphProperties = new ParagraphProperties(
                        new SpacingBetweenLines { After = "0", Before = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                        new Justification { Val = JustificationValues.Left }
                    );

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        paragraph.Append(new Run(new Text(line)));

                        // Add a line break if it's not the last line of the entry
                        if (i < lines.Length - 1)
                        {
                            paragraph.Append(new Run(new Break()));
                        }
                    }

                    body.AppendChild(paragraph);

                    //   body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()); // Add a blank paragraph between entries
                }

                mainPart.Document.Save();
            }
        }

        private static List<string> SortEntriesByDate(List<string> entries, bool descending)
        {
            Regex dateRegex = new Regex(@"Date:\s*(.+)\s*\n", RegexOptions.Compiled);
            CultureInfo cultureInfo = new CultureInfo("en-US");

            List<string> sortedEntries = entries.OrderBy(entry =>
            {
                Match dateMatch = dateRegex.Match(entry);

                if (dateMatch.Success)
                {
                    DateTime date = DateTime.Parse(dateMatch.Groups[1].Value.Trim(), cultureInfo);
                    return descending ? -date.Ticks : date.Ticks;
                }
                else
                {
                    return descending ? long.MaxValue : long.MinValue;
                }
            }).ToList();

            return sortedEntries;
        }
    }
}
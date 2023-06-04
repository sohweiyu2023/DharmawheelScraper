using System;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout;
using iText.Layout.Element;


namespace DharmawheelTableOfContentsCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter '1' to create table of contents for all files in a directory and its subdirectories, or '2' to create a table of contents for a specific file:");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.WriteLine("Enter the directory path:");
                string directoryPath = Console.ReadLine();
                string[] files = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.TopDirectoryOnly);

                Console.WriteLine($"This will process {files.Length} files. Are you sure you want to proceed? (y/n)");
                string confirmation = Console.ReadLine();

                if (confirmation.ToLower() == "y")
                {
                    int count = 1;
                    foreach (string file in files)
                    {
                        Console.WriteLine($"Processing file {count} of {files.Length}...");
                        GenerateTableOfContents(file);
                        count++;
                    }
                }
                else
                {
                    Console.WriteLine("Operation cancelled.");
                }
            }
            else if (choice == "2")
            {
                Console.WriteLine("Enter the file path:");
                string filePath = Console.ReadLine();

                GenerateTableOfContents(filePath);
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter '1' or '2'.");
            }
        }
        public static void GenerateTableOfContents(string pdfFilePath)
        {
            PdfReader reader = new PdfReader(pdfFilePath);
            PdfDocument pdfDoc = new PdfDocument(reader);
            Dictionary<string, List<int>> tableOfContents = new Dictionary<string, List<int>>();

            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                string text = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                string[] lines = text.Split('\n');

                foreach (string line in lines)
                {
                    if (line.StartsWith("Title: "))
                    {
                        string title = line.Substring("Title: ".Length);

                        if (!tableOfContents.ContainsKey(title))
                        {
                            tableOfContents[title] = new List<int>();
                        }

                        tableOfContents[title].Add(page);
                    }
                }
            }

            string tocFilePath = Path.GetDirectoryName(pdfFilePath) + @"\" + Path.GetFileNameWithoutExtension(pdfFilePath) + "_TableOfContents.pdf";
            SaveTableOfContentsToPdf(tableOfContents, tocFilePath);

            pdfDoc.Close();
        }

        public static void SaveTableOfContentsToPdf(Dictionary<string, List<int>> tableOfContents, string pdfFilePath)
        {
            PdfWriter writer = new PdfWriter(pdfFilePath);
            PdfDocument pdfDoc = new PdfDocument(writer);
            Document document = new Document(pdfDoc);

            foreach (KeyValuePair<string, List<int>> entry in tableOfContents)
            {
                string line = $"{entry.Key}: {string.Join(", ", entry.Value)}";
                document.Add(new Paragraph(line));
            }

            document.Close();
        }
    }
}
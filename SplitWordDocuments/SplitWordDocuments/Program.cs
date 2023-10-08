using System;
using System.Collections.Generic;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocxSplitter
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Enter the path to the source DOCX file:");
            var docPath = Console.ReadLine();

            Console.WriteLine("Enter the estimated number of characters per document:");
            int maxCharsPerChunk;
            while (!int.TryParse(Console.ReadLine(), out maxCharsPerChunk) || maxCharsPerChunk <= 0)
            {
                Console.WriteLine("Please enter a valid number.");
            }

            Console.WriteLine("Enter the output directory for the split files:");
            var outputDir = Console.ReadLine();
            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"Directory '{outputDir}' does not exist. Creating now...");
                Directory.CreateDirectory(outputDir);
            }

            SplitDocxByCharCount(docPath, maxCharsPerChunk, outputDir);

            Console.WriteLine("Process completed!");
        }

        public static void SplitDocxByCharCount(string docPath, int maxCharsPerChunk, string outputDir)
        {
            using (var sourceDoc = WordprocessingDocument.Open(docPath, false))
            {
                List<Paragraph> currentChunk = new List<Paragraph>();
                int currentCharCount = 0;
                int chunkCount = 1;
                WordprocessingDocument targetDoc = null;

                foreach (var para in sourceDoc.MainDocumentPart.Document.Body.Elements<Paragraph>())
                {
                    int paraCharCount = para.InnerText.Length;
                    bool isAuthorStart = para.InnerText.StartsWith("Author:");

                    if (currentCharCount + paraCharCount > maxCharsPerChunk && isAuthorStart)
                    {
                        if (targetDoc != null)
                        {
                            targetDoc.Close();
                        }

                        string newDocPath = Path.Combine(outputDir, $"chunk_{chunkCount}.docx");
                        targetDoc = WordprocessingDocument.Create(newDocPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
                        MainDocumentPart mainPart = targetDoc.AddMainDocumentPart();
                        mainPart.Document = new Document(new Body());

                        foreach (var chunkPara in currentChunk)
                        {
                            targetDoc.MainDocumentPart.Document.Body.Append(chunkPara.CloneNode(true));
                        }

                        currentChunk.Clear();
                        currentCharCount = 0;
                        chunkCount++;
                    }

                    currentChunk.Add(para);
                    currentCharCount += paraCharCount;
                }

                if (currentCharCount > 0 && targetDoc != null)
                {
                    foreach (var chunkPara in currentChunk)
                    {
                        targetDoc.MainDocumentPart.Document.Body.Append(chunkPara.CloneNode(true));
                    }

                    targetDoc.Close();
                }
            }
        }
    }
}

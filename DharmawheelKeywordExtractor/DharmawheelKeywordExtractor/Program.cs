using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace KeywordExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var keywordFile = @"C:\Users\cyber\source\repos\DharmawheelKeywordExtractor\DharmawheelKeywordExtractor\bin\Debug\net7.0\OriginalKeywords.txt"; // The file that contains the keywords

            Console.WriteLine("Enter directory path:");
            string directoryPath;

            while (true)
            {
                directoryPath = Console.ReadLine();

                if (Directory.Exists(directoryPath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid directory. Please enter an existing directory:");
                }
            }

            var keywordOccurrences = ExtractKeywords(keywordFile, directoryPath);

            SaveKeywordsToFiles(keywordOccurrences);
        }
        static Dictionary<string, int> ExtractKeywords(string keywordFile, string directoryPath)
        {
            var keywords = File.ReadAllLines(keywordFile)
                .SelectMany(line => line.Split(','))
                .Select(word => word.Trim().ToLower())
                .Distinct()
                .Where(word => !string.IsNullOrEmpty(word)) // Ensure keyword is not empty
                .ToList();

            var keywordOccurrences = new Dictionary<string, int>();

            var textFiles = Directory.GetFiles(directoryPath, "*.txt");

            foreach (var textFile in textFiles)
            {
                var text = File.ReadAllText(textFile);

                foreach (var keyword in keywords)
                {
                    // Initialize the keyword count
                    if (!keywordOccurrences.ContainsKey(keyword))
                    {
                        keywordOccurrences[keyword] = 0;
                    }

                    // If the keyword contains a "/", treat it as synonyms
                    if (keyword.Contains("/"))
                    {
                        var synonyms = keyword.Split('/');
                        foreach (var synonym in synonyms)
                        {
                            var escapedSynonym = Regex.Escape(synonym); // Escape special characters
                            var regex = new Regex($@"\b{escapedSynonym}\b", RegexOptions.IgnoreCase);
                            var matches = regex.Matches(text);

                            keywordOccurrences[keyword] += matches.Count;
                        }
                    }
                    else
                    {
                        var escapedKeyword = Regex.Escape(keyword); // Escape special characters
                        var regex = new Regex($@"\b{escapedKeyword}\b", RegexOptions.IgnoreCase);
                        var matches = regex.Matches(text);

                        keywordOccurrences[keyword] += matches.Count;
                    }
                }
            }

            return keywordOccurrences;
        }


        static void SaveKeywordsToFiles(Dictionary<string, int> keywordOccurrences)
        {
            // Order by occurrence count
            var sortedKeywordOccurrences = keywordOccurrences
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            File.WriteAllLines("KeywordsOccurrences.txt", sortedKeywordOccurrences
                .Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            // Only write keywords to file that have at least one occurrence
            File.WriteAllLines("Keywords.txt", sortedKeywordOccurrences
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key));
        }
    }
}

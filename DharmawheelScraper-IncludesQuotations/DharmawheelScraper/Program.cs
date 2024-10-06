using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl;
using HtmlAgilityPack;
using RestSharp;
using static System.Net.Mime.MediaTypeNames;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ConsoleApp
{
    class Program
    {
        private const string BaseUrl = "https://dharmawheel.net";
        private const string AuthorName = "Malcolm";
        private const string AuthorId = "638";
        private static int CurrentPage = 0;
        private static int FileCounter = 1;
        private static readonly List<string> CollectedPosts = new List<string>();
        private static readonly HashSet<string> SeenPostDatetimes = new HashSet<string>();

        private static readonly HashSet<string> ScrapedPostsUrls = new HashSet<string>();

        public static int TotalPosts = 0;/* Retrieve this number from the author's profile page. */

        private static readonly RestClient client = new RestClient();


        private static int PostsPerSearchPage = 20;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Do you want to scrape the latest posts only? (yes/no)");
            var response = Console.ReadLine().ToLower();

            DateTime? stopDate = null;
            var latestPostsOnly = response == "yes";


            if (latestPostsOnly)
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{AuthorName}_posts_*.txt");
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    var firstEntry = File.ReadLines(latestFile).Take(4).ToList();
                    if (firstEntry.Count >= 2)
                    {
                        var dateString = firstEntry[1].Replace("Date: ", "").Trim();
                        dateString = RemoveOrdinalSuffix(dateString); // Remove ordinal suffixes

                        DateTime parsedDate;
                        if (DateTime.TryParseExact(
                            dateString,
                            "dddd, MMMM d, yyyy 'at' h:mm tt",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out parsedDate))
                        {
                            stopDate = parsedDate;
                            Console.WriteLine("Stop date: " + stopDate);
                        }
                        else
                        {
                            Console.WriteLine("Could not parse date: " + dateString);
                        }

                        if (stopDate == null || stopDate < new DateTime(2023, 1, 1)) // Adjust the date as needed
                        {
                            Console.WriteLine("The stop date is too old or not found. Do you want to enter a stop date manually? (yes/no)");
                            var manualDateResponse = Console.ReadLine().ToLower();

                            if (manualDateResponse == "yes")
                            {
                                Console.WriteLine("Enter the stop date in the format yyyy-MM-dd HH:mm:ss");
                                var dateInput = Console.ReadLine();
                                if (DateTime.TryParseExact(
                                    dateInput,
                                    "yyyy-MM-dd HH:mm:ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime manualStopDate))
                                {
                                    stopDate = manualStopDate;
                                    Console.WriteLine("Stop date set to: " + stopDate);
                                }
                                else
                                {
                                    Console.WriteLine("Invalid date format. Exiting.");
                                    //driver.Quit();
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Proceeding without a stop date. All posts will be collected.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Could not find a valid date in the latest file.");
                    }
                }
                else
                {
                    Console.WriteLine("No existing files found to determine the stop date.");
                }
            }
            /*if (latestPostsOnly)
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{AuthorName}_posts_*.txt");
                if (files.Length > 0)
                {
                    DateTime maxDate = DateTime.MinValue;

                    foreach (var file in files)
                    {
                        Console.WriteLine("Processing " + file);
                        var lines = File.ReadLines(file);

                        for (int i = 0; i < lines.Count(); i++)
                        {
                            var line = lines.ElementAt(i);
                            if (line.StartsWith("Date: "))
                            {
                                var dateString = line.Replace("Date: ", "").Trim();
                                dateString = RemoveOrdinalSuffix(dateString);

                                if (DateTime.TryParseExact(
                                    dateString,
                                    "dddd, MMMM d, yyyy 'at' h:mm tt",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime parsedDate))
                                {
                                    if (parsedDate > maxDate)
                                    {
                                        maxDate = parsedDate;
                                    }
                                }
                            }
                        }
                    }

                    if (maxDate != DateTime.MinValue)
                    {
                        stopDate = maxDate;
                        Console.WriteLine("Stop date set to latest date in files: " + stopDate);
                    }
                    else
                    {
                        Console.WriteLine("Could not parse any dates from existing files.");
                    }
                }
                else
                {
                    Console.WriteLine("No existing files found to determine the stop date.");
                }
            }*/
            else
            {
                // Add the prompts for starting page number and file counter
                Console.WriteLine("Do you want to start from a specific page number? (yes/no)");
                var startPageResponse = Console.ReadLine().ToLower();

                if (startPageResponse == "yes")
                {
                    Console.WriteLine("Enter the starting page number:");
                    var pageNumberInput = Console.ReadLine();
                    if (int.TryParse(pageNumberInput, out int pageNumber) && pageNumber >= 0)
                    {
                        CurrentPage = pageNumber;
                    }
                    else
                    {
                        Console.WriteLine("Invalid page number input. Starting from page 0.");
                        CurrentPage = 0;
                    }
                }
                else
                {
                    CurrentPage = 0; // Ensure CurrentPage is initialized
                }

                Console.WriteLine("Do you want to start from a specific file counter? (yes/no)");
                var fileCounterResponse = Console.ReadLine().ToLower();

                if (fileCounterResponse == "yes")
                {
                    Console.WriteLine("Enter the starting file counter:");
                    var fileCounterInput = Console.ReadLine();
                    if (int.TryParse(fileCounterInput, out int fileCounter) && fileCounter >= 1)
                    {
                        FileCounter = fileCounter;
                    }
                    else
                    {
                        Console.WriteLine("Invalid file counter input. Starting from file counter 1.");
                        FileCounter = 1;
                    }
                }
                else
                {
                    FileCounter = 1; // Ensure FileCounter is initialized
                }
            }

            var options = new ChromeOptions();
            // Uncomment the below line to run Chrome in headless mode
            // options.AddArgument("--headless");

            IWebDriver driver = new ChromeDriver(options);
            int maxAttempts = 5;  // Adjust this value according to your requirements

            // Login process
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    driver.Navigate().GoToUrl($"{BaseUrl}/ucp.php?mode=login");

                    var usernameInput = driver.FindElement(By.Name("username"));
                    var passwordInput = driver.FindElement(By.Name("password"));
                    var submitButton = driver.FindElement(By.Name("login"));

                    /* Warning: Make sure to use an older account with many previous activities for 
                     * scraping. I've noticed that when using newer accounts like "nyingje," 
                     * it can retrieve far fewer posts compared to an older account like "xabir," 
                     * which has a longer history of posts. */

                    usernameInput.SendKeys("xabir"); // Replace with your username
                    passwordInput.SendKeys("thisisthewrongpassword"); // Replace with your password
                    submitButton.Click();
                    // If we successfully found the elements, break out of the loop
                    break;
                }
                catch (WebDriverException ex)
                {
                    Console.WriteLine($"Attempt {attempt}: WebDriverException occurred: {ex.Message}");

                    // If it was the last attempt, rethrow the exception
                    if (attempt == maxAttempts)
                    {
                        throw;
                    }

                    // Wait for a bit before the next attempt
                    System.Threading.Thread.Sleep(5000);  // Adjust this value according to your requirements
                }
            }

            // Navigate to the author's profile page to get total posts
            driver.Navigate().GoToUrl($"{BaseUrl}/memberlist.php?mode=viewprofile&u={AuthorId}");

            try
            {
                var totalPostsElement = driver.FindElement(By.XPath("//dt[text()='Total posts:']/following-sibling::dd[1]"));
                var totalPostsText = totalPostsElement.Text.Trim();

                // Use a regular expression to extract the number at the beginning
                var match = Regex.Match(totalPostsText, @"^\d+");
                if (match.Success)
                {
                    TotalPosts = int.Parse(match.Value, CultureInfo.InvariantCulture);
                    Console.WriteLine("Automatically retrieved total number of user's posts: " + TotalPosts);
                }
                else
                {
                    throw new Exception("Could not parse total posts number from text: " + totalPostsText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not retrieve total number of posts: " + ex.Message);
                driver.Quit();
                return;
            }

            if (!latestPostsOnly)
            {
                // Prompt the user for manual total posts entry
                Console.WriteLine("Do you want to manually enter the total number of posts? (yes/no)");
                var manualTotalPostsResponse = Console.ReadLine().ToLower();

                if (manualTotalPostsResponse == "yes")
                {
                    Console.WriteLine("Enter the total number of posts:");
                    var totalPostsInput = Console.ReadLine();
                    if (int.TryParse(totalPostsInput, out int manualTotalPosts) && manualTotalPosts >= 1)
                    {
                        TotalPosts = manualTotalPosts;
                        Console.WriteLine("Using manually entered total posts: " + TotalPosts);
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Proceeding with automatically retrieved total posts: " + TotalPosts);
                    }
                }
                else
                {
                    Console.WriteLine("Proceeding with automatically retrieved total posts: " + TotalPosts);
                }
            }

            int totalPages = (TotalPosts + PostsPerSearchPage - 1) / PostsPerSearchPage;
            int totalCollectedPosts = ((FileCounter - 1) * 500); // Adjust if needed

            // Now you are logged in, navigate to the page you want to scrape
            driver.Navigate().GoToUrl($"{BaseUrl}/index.php");

            var url = $"{BaseUrl}/search.php?st=0&sk=t&sd=d&sr=posts&author_id={AuthorId}&start={CurrentPage * PostsPerSearchPage}";

            while (CurrentPage < totalPages)
            {
                try
                {
                    url = $"{BaseUrl}/search.php?st=0&sk=t&sd=d&sr=posts&author_id={AuthorId}&start={CurrentPage * PostsPerSearchPage}";

                    Console.WriteLine($"Fetching page {CurrentPage}: {url}");

                    driver.Navigate().GoToUrl(url);
                    var results = driver.FindElements(By.CssSelector("[class^='search post']"));

                    if (results.Count == 0)
                    {
                        Console.WriteLine("No more posts found on this page.");
                        break;
                    }

                    int resultsCount = results.Count;

                    for (int i = 0; i < resultsCount; i++)
                    {
                        // Refresh the "results" after navigating back from the post page
                        results = driver.FindElements(By.CssSelector("[class^='search post']"));
                        var result = results[i];

                        var postLinkElement = result.FindElement(By.CssSelector("h3 a"));
                        var postLink = postLinkElement.GetAttribute("href");

                        // Skip this post if we have already scraped it
                        if (ScrapedPostsUrls.Contains(postLink))
                        {
                            continue;
                        }

                        ScrapedPostsUrls.Add(postLink);

                        var author = result.FindElement(By.CssSelector($"a.username")).Text.Trim();
                        // Existing code to parse postTime
                        var postTimeStrRaw = result.FindElement(By.CssSelector("dd.search-result-date")).Text.Trim();
                        var postTime = DateTime.ParseExact(
                            postTimeStrRaw,
                            "ddd MMM d, yyyy h:mm tt",
                            CultureInfo.InvariantCulture);

                        // Print postTime and stopDate
                        Console.WriteLine("Parsed postTime: " + postTime.ToString("o")); // Print in ISO 8601 format

                        if (stopDate.HasValue)
                        {
                            Console.WriteLine("Comparing postTime <= stopDate: " + (postTime <= stopDate.Value));
                            Console.WriteLine($"postTime: {postTime.ToString("o")} <= stopDate: {stopDate.Value.ToString("o")} ?");
                        }
                        else
                        {
                            Console.WriteLine("stopDate is null.");
                        }

                        if (stopDate.HasValue && postTime <= stopDate.Value)
                        {
                            Console.WriteLine("Reached previously scraped post, stopping");

                            if (CollectedPosts.Any())
                            {
                                SaveToFileLatestPosts(CollectedPosts);
                            }

                            driver.Quit();
                            return;
                        }

                        var postTitle = result.FindElement(By.CssSelector("h3")).Text.Trim();
                        var postId = postLink.Split('#')[1].Substring(1);

                        driver.Navigate().GoToUrl($"{postLink}");
                        var postPageHtml = driver.PageSource;

                        var postPageParser = new HtmlParser();
                        var postPage = await postPageParser.ParseDocumentAsync(postPageHtml);

                        var correctPost = GetCorrectPost(postPage, $"p{postId}");

                        if (correctPost == null)
                        {
                            Console.WriteLine($"Error fetching post page: Could not find post with ID {postId}");
                            continue;
                        }

                        var postContent = FormatPostContent(correctPost, author);

                        // Get the day and its ordinal suffix
                        int day = postTime.Day;
                        string daySuffix = GetOrdinalSuffix(day);

                        // Construct the date string using string interpolation
                        var postTimeStr = $"{postTime:dddd}, {postTime:MMMM} {day}{daySuffix}, {postTime:yyyy} at {postTime:h:mm tt}";

                        var postEntry = $"Author: {author}\nDate: {postTimeStr}\nTitle: {postTitle}\nContent:\n{postContent}\n";

                        CollectedPosts.Add(postEntry);
                        totalCollectedPosts++;
                        Console.WriteLine($"Collecting post {totalCollectedPosts}");

                        if (CollectedPosts.Count >= 500)
                        {
                            SaveToFileAllPosts(CollectedPosts, FileCounter);
                            CollectedPosts.Clear();
                            FileCounter++;
                        }

                        if (totalCollectedPosts >= TotalPosts)
                        {
                            Console.WriteLine("Collected all posts. Stopping.");
                            if (CollectedPosts.Any())
                            {
                                SaveToFileAllPosts(CollectedPosts, FileCounter);
                                CollectedPosts.Clear();
                            }
                            driver.Quit();
                            return;
                        }

                        // After processing each post, navigate back to the results page:
                        driver.Navigate().Back();
                        // Wait for the page to load
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }

                    CurrentPage++;
                }
                catch (WebDriverException ex)
                {
                    maxAttempts = 5;

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        try
                        {
                            Console.WriteLine($"Caught WebDriverException: {ex.Message}. Attempting to recover.");

                            // Wait a bit before trying to recover
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            // Dispose the current driver instance
                            driver.Dispose();

                            // Create a new driver instance
                            driver = new ChromeDriver(options);

                            // You'll need to log in again here, since this is a new driver instance
                            driver.Navigate().GoToUrl($"{BaseUrl}/ucp.php?mode=login");
                            var usernameInput = driver.FindElement(By.Name("username"));
                            var passwordInput = driver.FindElement(By.Name("password"));
                            var submitButton = driver.FindElement(By.Name("login"));

                            /* Warning: Make sure to use an older account with many previous activities for 
                             * scraping. I've noticed that when using newer accounts like "nyingje," 
                             * it can retrieve far fewer posts compared to an older account like "xabir," 
                             * which has a longer history of posts. */

                            usernameInput.SendKeys("xabir"); // Replace with your username
                            passwordInput.SendKeys("thisisthewrongpassword"); // Replace with your password
                            submitButton.Click();

                            // Now you are logged in, navigate to the page you were trying to scrape
                            driver.Navigate().GoToUrl(url);

                            // If we successfully found the elements, break out of the loop
                            break;
                        }
                        catch (WebDriverException ex0)
                        {
                            Console.WriteLine($"Attempt {attempt}: WebDriverException occurred: {ex0.Message}");

                            // If it was the last attempt, rethrow the exception
                            if (attempt == maxAttempts)
                            {
                                throw;
                            }

                            // Wait for a bit before the next attempt
                            System.Threading.Thread.Sleep(5000);  // Adjust this value according to your requirements
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}. Moving to next page.");
                    CurrentPage++;
                }
            }

            if (CollectedPosts.Any())
            {
                SaveToFileAllPosts(CollectedPosts, FileCounter);
            }

            driver.Quit();
        }
        private static string GetOrdinalSuffix(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 13)
            {
                return "th";
            }
            else
            {
                switch (number % 10)
                {
                    case 1:
                        return "st";
                    case 2:
                        return "nd";
                    case 3:
                        return "rd";
                    default:
                        return "th";
                }
            }
        }

        private static string AddOrdinalSuffix(DateTime dateTime, string formattedDate)
        {
            int day = dateTime.Day;
            string suffix = GetOrdinalSuffix(day);

            return formattedDate.Replace("st", suffix); // Replace 'st' with the correct suffix
        }



        private static IElement GetCorrectPost(IDocument postPage, string postId)
        {
            IElement? targetPost = postPage.QuerySelector($"div#{postId}");
            return targetPost;
        }
        private static string FormatPostContent(IElement post, string authorName)
        {
            if (post == null)
            {
                return "";
            }

            StringBuilder output = new StringBuilder();

            var contentDiv = post.QuerySelector("div.content");

            if (contentDiv != null)
            {
                string lastSpeaker = null;
                string lastNonAuthorSpeaker = null;
                ProcessNode(contentDiv, output, authorName, authorName, ref lastSpeaker, ref lastNonAuthorSpeaker);
            }

            return output.ToString().TrimEnd(); // Trim any extra newlines at the end
        }

        private static void ProcessNode(INode node, StringBuilder output, string currentSpeaker, string authorName,
                                ref string lastSpeaker, ref string lastNonAuthorSpeaker)
        {
            if (node == null) return;

            if (node is IElement element)
            {
                if (element.TagName.Equals("BLOCKQUOTE", StringComparison.OrdinalIgnoreCase))
                {
                    var cite = element.QuerySelector("cite");
                    string speaker = null;

                    if (cite != null)
                    {
                        var speakerElement = cite.QuerySelector("a");
                        if (speakerElement != null)
                        {
                            speaker = speakerElement.TextContent.Trim();
                        }
                        else
                        {
                            // Fallback to the text content
                            speaker = cite.TextContent.Trim();
                            if (speaker.Contains(" wrote:"))
                            {
                                speaker = speaker.Substring(0, speaker.IndexOf(" wrote:"));
                            }
                        }
                        // Remove the cite to prevent it from appearing in content
                        cite.Remove();
                        lastNonAuthorSpeaker = speaker; // Update last non-author speaker
                    }
                    else
                    {
                        // Handle blockquotes without a cite
                        // If the blockquote has class 'uncited', assume the speaker is the last non-author speaker
                        if (element.ClassList.Contains("uncited") && !string.IsNullOrEmpty(lastNonAuthorSpeaker))
                        {
                            speaker = lastNonAuthorSpeaker;
                        }
                        else
                        {
                            speaker = "Unknown";
                        }
                    }

                    // Process the content inside the blockquote
                    foreach (var child in element.ChildNodes)
                    {
                        ProcessNode(child, output, speaker, authorName, ref lastSpeaker, ref lastNonAuthorSpeaker);
                    }
                }
                else if (element.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine();
                }
                else if (IsBlockElement(element))
                {
                    // Process block-level elements
                    foreach (var child in element.ChildNodes)
                    {
                        ProcessNode(child, output, currentSpeaker, authorName, ref lastSpeaker, ref lastNonAuthorSpeaker);
                    }

                    // Add a newline after block elements if not already present
                    if (output.Length > 0 && output[output.Length - 1] != '\n')
                    {
                        output.AppendLine();
                    }
                }
                else
                {
                    // Process inline elements
                    string text = GetTextContent(element);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AppendTextWithSpeaker(output, text, currentSpeaker, authorName, ref lastSpeaker, ref lastNonAuthorSpeaker);
                    }
                }
            }
            else if (node.NodeType == NodeType.Text)
            {
                string text = node.TextContent;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AppendTextWithSpeaker(output, text.Trim(), currentSpeaker, authorName, ref lastSpeaker, ref lastNonAuthorSpeaker);
                }
            }
        }
        private static void AppendTextWithSpeaker(StringBuilder output, string text, string currentSpeaker, string authorName,
                                          ref string lastSpeaker, ref string lastNonAuthorSpeaker)
        {
            // Only prepend speaker if it's not the same as the last speaker
            if (currentSpeaker != lastSpeaker)
            {
                // Add one blank line before changing speaker, unless at the very start
                if (output.Length > 0)
                {
                    output.AppendLine();
                }

                if (currentSpeaker == authorName)
                {
                    output.AppendLine($"{currentSpeaker} wrote:");
                }
                else
                {
                    output.AppendLine($"{currentSpeaker} said:");
                    lastNonAuthorSpeaker = currentSpeaker; // Update last non-author speaker
                }
                lastSpeaker = currentSpeaker;
            }

            // Trim leading and trailing spaces in text
            text = text.Trim();

            // Check if the text starts with punctuation
            bool startsWithPunctuation = text.StartsWith(".") || text.StartsWith(",") || text.StartsWith(";") ||
                                         text.StartsWith(":") || text.StartsWith("?") || text.StartsWith("!");

            // Append a space if needed
            if (output.Length > 0 && output[output.Length - 1] != '\n' && !startsWithPunctuation)
            {
                output.Append(" ");
            }

            output.Append(text);
        }
        private static string GetTextContent(INode node)
        {
            if (node.NodeType == NodeType.Text)
            {
                return node.TextContent;
            }
            else if (node is IElement element)
            {
                if (element.TagName.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle links
                    var href = element.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        return href;
                    }
                    else
                    {
                        return element.TextContent;
                    }
                }
                else
                {
                    StringBuilder textBuilder = new StringBuilder();

                    foreach (var child in element.ChildNodes)
                    {
                        textBuilder.Append(GetTextContent(child));
                    }

                    return textBuilder.ToString();
                }
            }
            else
            {
                return "";
            }
        }



        // Helper method to collect inline text content recursively
        private static void CollectInlineText(INode node, StringBuilder textBuffer)
        {
            if (node.NodeType == NodeType.Text)
            {
                textBuffer.Append(node.TextContent);
            }
            else if (node is IElement element)
            {
                if (element.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase))
                {
                    textBuffer.AppendLine();
                }
                else if (IsInlineElement(element))
                {
                    foreach (var child in element.ChildNodes)
                    {
                        CollectInlineText(child, textBuffer);
                    }
                }
                else if (element.TagName.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle links
                    var href = element.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        textBuffer.Append(href);
                    }
                }
                else if (element.TagName.Equals("IMG", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle emoticons
                    var altText = element.GetAttribute("alt")?.Trim();
                    if (!string.IsNullOrEmpty(altText))
                    {
                        textBuffer.Append($"[{altText} Emoticon]");
                    }
                }
                else
                {
                    // For other elements, process their children
                    foreach (var child in element.ChildNodes)
                    {
                        CollectInlineText(child, textBuffer);
                    }
                }
            }
        }

        // Helper method to check if an element is inline
        private static bool IsInlineElement(INode node)
        {
            if (node is IElement element)
            {
                string[] inlineElements = { "A", "SPAN", "EM", "STRONG", "I", "B", "U", "IMG", "CODE" };
                return inlineElements.Contains(element.TagName.ToUpper());
            }
            return false;
        }

        // Helper method to check if an element is a block-level element
        private static bool IsBlockElement(IElement element)
        {
            string[] blockElements = { "DIV", "P", "H1", "H2", "H3", "H4", "H5", "H6", "UL", "OL", "LI", "TABLE", "BLOCKQUOTE", "PRE", "BR" };
            return blockElements.Contains(element.TagName.ToUpper());
        }


        private static void SaveToFileAllPosts(IEnumerable<string> posts, int fileCounter)
        {
            var fileName = $"{AuthorName}_posts_{fileCounter}.txt";
            using var file = new StreamWriter(fileName, false, Encoding.UTF8);
            foreach (var post in posts)
            {
                file.WriteLine(post);
                file.WriteLine();
            }


            Console.WriteLine("Saved to file " + fileName);
        }
        private static void SaveToFileLatestPosts(IEnumerable<string> posts)
        {
            int fileNumber = 0;
            string fileName;

            do
            {
                fileName = $"{AuthorName}_posts_0_{DateTime.Today.ToString("dd-M-yyyy")}_{fileNumber++}.txt";
            }
            while (File.Exists(fileName));

            using var file = new StreamWriter(fileName, false, Encoding.UTF8);
            foreach (var post in posts)
            {
                file.WriteLine(post);
                file.WriteLine();
            }

            Console.WriteLine("Saved to file " + fileName);
        }
        private static RestClient CreateRestClient(string baseUrl)
        {
            var client = new RestClient(baseUrl);
            client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            return client;
        }
        private static string RemoveOrdinalSuffix(string dateString)
        {
            return Regex.Replace(dateString, @"(\d{1,2})(st|nd|rd|th)", "$1");
        }

    }
}

public static class WebDriverExtensions
{
    public static IWebElement WaitUntilVisible(this IWebDriver driver, By by, int timeout = 10)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
        return wait.Until(drv => drv.FindElement(by));
    }

    public static IWebElement RetryAction(this IWebDriver driver, By by, int maxRetryCount = 3)
    {
        for (int attempt = 0; attempt < maxRetryCount; attempt++)
        {
            try
            {
                // Try to perform the operation
                return driver.FindElement(by);
            }
            catch (StaleElementReferenceException)
            {
                if (attempt == maxRetryCount - 1) throw; // Rethrow the exception if we've reached max retries
            }
        }
        throw new Exception("An unexpected error occurred in the RetryAction method.");
    }
}



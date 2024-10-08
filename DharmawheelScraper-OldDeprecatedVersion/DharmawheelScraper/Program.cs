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
                        var dateMatch = Regex.Match(dateString, @"\w+ \w+ \d{1,2}, \d{4} \d{1,2}:\d{2} \w+");
                        if (dateMatch.Success)
                        {
                            stopDate = DateTime.ParseExact(dateMatch.Value, "ddd MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                            Console.WriteLine("Stop date: " + stopDate);
                        }
                    }
                }
            }

            var options = new ChromeOptions();
            // Uncomment below line to run Chrome in headless mode
            // options.AddArgument("--headless");

            //IWebDriver driver = null;// new ChromeDriver(options);

            IWebDriver driver = new ChromeDriver(options);


            //IWebDriver driver = null;
            int maxAttempts = 5;  // Adjust this value according to your requirements

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    driver.Navigate().GoToUrl($"{BaseUrl}/ucp.php?mode=login");

                    var usernameInput = driver.FindElement(By.Name("username"));
                    var passwordInput = driver.FindElement(By.Name("password"));
                    var submitButton = driver.FindElement(By.Name("login"));

                    usernameInput.SendKeys("nyingje");
                    passwordInput.SendKeys("kila8118");
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
            /*Please be aware that with new accounts on the Dharmawheel platform such as "nyingje", 
             * there may be limitations on scraping the most recent posts from the same day. 
             * This limitation seems to be tied to the age and activity level of the account used for scraping. 
             * It appears that the platform places restrictions on viewing same-day posts for newer accounts, 
             * possibly to limit automated activity or to encourage engagement with the community. 
             * As such, you may only be able to scrape posts from previous days with a new account. 
             * If you need to scrape posts from the same day, 
             * you may need to use an older account that has some level of activity. 
             * Please make sure to respect the platform's guidelines and terms of use when using this scraping tool."
             * */


            // Now you are logged in, navigate to the page you want to scrape
            driver.Navigate().GoToUrl($"{BaseUrl}/index.php");

            var url = $"{BaseUrl}/search.php?st=0&sk=t&sd=d&sr=posts&author_id={AuthorId}&start={CurrentPage * PostsPerSearchPage}";

            while (true)
            {
                try
                {
                    url = $"{BaseUrl}/search.php?st=0&sk=t&sd=d&sr=posts&author_id={AuthorId}&start={CurrentPage * PostsPerSearchPage}";

                    Console.WriteLine($"Fetching page {CurrentPage}: {url}");

                    driver.Navigate().GoToUrl(url);
                    var results = driver.FindElements(By.CssSelector("[class^='search post']"));

                    if (results.Count == 0)
                    {
                        Console.WriteLine("No more posts found");
                        driver.Quit();
                        break;
                    }

                    int resultsCount = results.Count;

                    for (int i = 0; i < resultsCount; i++)
                    {
                        // Refresh the "results" after navigating back from the post page
                        results = driver.FindElements(By.CssSelector("[class^='search post']"));
                        var result = results[i];

                        var postLinkElementBy = By.CssSelector("h3 a");
                        // Search within the result element, not the whole driver

                        var postLinkElement = result.FindElement(postLinkElementBy);


                        var postLink = postLinkElement.GetAttribute("href");


                        // Skip this post if we have already scraped it
                        if (CollectedPosts.Count > TotalPosts && ScrapedPostsUrls.Contains(postLink))
                        {
                            Console.WriteLine("Encountered previously scraped post, stopping");

                            if (CollectedPosts.Count > 0)
                                SaveToFileAllPosts(CollectedPosts, FileCounter);
                            else
                                Console.WriteLine("0 collected posts written.");

                            driver.Quit();
                            return;
                        }
                        else if (CollectedPosts.Count <= TotalPosts && ScrapedPostsUrls.Contains(postLink))
                        {
                            continue;
                        }


                        ScrapedPostsUrls.Add(postLink);

                        var author = result.FindElement(By.CssSelector($"a.username{((AuthorName == "Dhammanando") ? "-coloured" : "")}")).Text.Trim();
                        var postTime = DateTime.ParseExact(result.FindElement(By.CssSelector("dd.search-result-date")).Text.Trim(), "ddd MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);

                        if (stopDate.HasValue && postTime <= stopDate.Value)
                        {
                            Console.WriteLine("Reached previously scraped post, stopping");

                            if (CollectedPosts.Count > 0)
                                SaveToFileLatestPosts(CollectedPosts);
                            else
                                Console.WriteLine("0 collected posts written.");

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


                        if (TotalPosts == 0)
                        {

                            string pattern = @"<dd class=""profile-posts""><strong>Posts:</strong> <a href="".*?"">(\d+)</a></dd>";
                            Match match = Regex.Match(correctPost.InnerHtml, pattern);
                            if (match.Success)
                            {
                                string posts = match.Groups[1].Value;
                                Console.WriteLine("Total number of user's posts: " + posts);
                                TotalPosts = Convert.ToInt32(posts);
                            }
                        }

                        var postContent = FormatPostContent(correctPost);


                        var postTimeStr = postTime.ToString("ddd MMM d, yyyy h:mm tt");

                        if (CollectedPosts.Count > TotalPosts && SeenPostDatetimes.Contains(postTimeStr))
                        {
                            Console.WriteLine("Encountered duplicate post datetime after exceeding total post count, stopping");

                            if (CollectedPosts.Count > 0)
                                SaveToFileLatestPosts(CollectedPosts);
                            else
                                Console.WriteLine("0 collected posts written.");

                            driver.Quit();
                            return;
                        }

                        SeenPostDatetimes.Add(postTimeStr);

                        var postEntry = $"Author: {author}\nDate: {postTimeStr}\nTitle: {postTitle}\nContent:\n{postContent}\n";
                        CollectedPosts.Add(postEntry);
                        Console.WriteLine($"Collecting post {CollectedPosts.Count}");

                        if (CollectedPosts.Count >= 500)
                        {
                            SaveToFileAllPosts(CollectedPosts, FileCounter);
                            CollectedPosts.Clear();
                            FileCounter++;
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

                            usernameInput.SendKeys("nyingje");
                            passwordInput.SendKeys("kila8118");
                            submitButton.Click();

                            // Now you are logged in, navigate to the page you were trying to scrape
                            driver.Navigate().GoToUrl(url);

                            // Since we're in a loop, the next iteration will try to scrape the posts again
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
                    Console.WriteLine($"Stale error {ex.ToString()}. Skipping to next post.");
                    continue;
                }

            }

            if (CollectedPosts.Any())
            {
                if (latestPostsOnly)
                {
                    SaveToFileLatestPosts(CollectedPosts);
                }
                else
                {
                    SaveToFileAllPosts(CollectedPosts, FileCounter);
                }
            }
        }

        private static async Task LoginToWebsite(RestClient client)
        {
            var loginPageRequest = new RestRequest($"{BaseUrl}/ucp.php?mode=login", Method.Get);
            var loginPageResponse = await client.ExecuteAsync(loginPageRequest);
            var loginPageContent = loginPageResponse.Content;

            var doc = new HtmlDocument();
            doc.LoadHtml(loginPageContent);
            var sid = doc.GetElementbyId("sid").GetAttributeValue("value", "");
            var creationTime = doc.GetElementbyId("creation_time").GetAttributeValue("value", "");
            var formToken = doc.GetElementbyId("form_token").GetAttributeValue("value", "");

            var loginRequest = new RestRequest("ucp.php?mode=login", Method.Post);
            loginRequest.AddParameter("username", "nyingje", ParameterType.GetOrPost);
            loginRequest.AddParameter("password", "kila8118", ParameterType.GetOrPost);
            loginRequest.AddParameter("redirect", "./ucp.php?mode=login&redirect=index.php", ParameterType.GetOrPost);
            loginRequest.AddParameter("creation_time", creationTime, ParameterType.GetOrPost);
            loginRequest.AddParameter("form_token", formToken, ParameterType.GetOrPost);
            loginRequest.AddParameter("sid", sid, ParameterType.GetOrPost);
            loginRequest.AddParameter("login", "Login", ParameterType.GetOrPost);

            loginRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            loginRequest.AddHeader("Origin", "https://www.dharmawheel.net");
            loginRequest.AddHeader("Referer", "https://www.dharmawheel.net/ucp.php?mode=login&redirect=index.php");

            var loginResponse = await client.ExecuteAsync(loginRequest);
            var loginResponseContent = loginResponse.Content;

            Console.WriteLine($"Response Status Code: {loginResponse.StatusCode}");
            Console.WriteLine($"Response Content: {loginResponseContent}");
        }

        private static IElement GetCorrectPost(IDocument postPage, string postId)
        {
            IElement? targetPost = postPage.QuerySelector($"div#{postId}");
            return targetPost;
        }
        private static string FormatPostContent(IElement post)
        {
            if (post == null)
            {
                return "";
            }

            var mainContent = post.QuerySelector("div.inner");
            if (mainContent == null)
            {
                return "";
            }

            var outputText = "";

            var postContentDiv = mainContent.QuerySelector("div.content");

            // Find the div containing the Dharmawheel User's message
            var userDiv = postContentDiv.QuerySelectorAll("div").LastOrDefault(div =>
            {
                var nextSibling = div.NextElementSibling;
                return nextSibling != null && nextSibling.ClassName == "signature";
            });

            if (userDiv != null)
            {
                // Get the content message from Dharmawheel User
                var content = userDiv.InnerHtml.Trim();
                var lastQuoteIndex = content.LastIndexOf("</blockquote>");
                if (lastQuoteIndex != -1)
                {
                    content = content.Substring(lastQuoteIndex, content.Length - lastQuoteIndex).Trim();
                }

                // Extract URLs
                content = ExtractAndReplaceUrls(content);

                // Remove HTML tags from the content and exclude blockquotes
                content = Regex.Replace(content, "<blockquote>.*?</blockquote>", "", RegexOptions.Singleline);
                content = Regex.Replace(content, "<.*?>", "");

                outputText = content.Trim();

            }
            else
            {
                // Get the content message from Dharmawheel User without a signature
                var content = postContentDiv.InnerHtml.Trim();
                var lastQuoteIndex = content.LastIndexOf("</blockquote>");
                if (lastQuoteIndex != -1)
                {
                    content = content.Substring(lastQuoteIndex, content.Length - lastQuoteIndex).Trim();
                }

                // Extract URLs
                content = ExtractAndReplaceUrls(content);

                // Remove HTML tags from the content and exclude blockquotes
                content = Regex.Replace(content, "<blockquote>.*?</blockquote>", "", RegexOptions.Singleline);
                content = Regex.Replace(content, "<.*?>", "");

                outputText = content.Trim();
            }

            return outputText;
        }

        private static string ExtractAndReplaceUrls(string content)
        {
            var htmlParser = new HtmlParser();
            var document = htmlParser.ParseDocument(content);
            var links = document.QuerySelectorAll("a");

            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                content = content.Replace(link.OuterHtml, href);
            }

            return content;
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



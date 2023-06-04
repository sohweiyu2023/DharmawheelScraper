using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl;
using RestSharp;
using static System.Net.Mime.MediaTypeNames;

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
                    var latestFile = files
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .First();

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

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            var documentX = await context.OpenAsync($"{BaseUrl}/ucp.php?mode=login");
            var sid = documentX.QuerySelector("input[name='sid']").GetAttribute("value");
            var creationTime = documentX.QuerySelector("input[name='creation_time']").GetAttribute("value");
            var formToken = documentX.QuerySelector("input[name='form_token']").GetAttribute("value");

            var loginData = new
            {
                username = "enter your dharmawheel username",
                password = "enter your dharmawheel password",
                redirect = "index.php",
                sid = sid,
                creation_time = creationTime,
                form_token = formToken
            };

            using var client = CreateRestClient($"{BaseUrl}/ucp.php?mode=login");
            var request = new RestRequest("/ucp.php?mode=login", Method.Post);
            request.AddObject(loginData);
            await client.ExecuteAsync(request);


            Console.WriteLine("Log in may have failed. Get response from client.ExecuteAsync(request) for more details.");



            while (true)
            { 


                var url = $"{BaseUrl}/search.php?st=0&sk=t&sd=d&sr=posts&author_id={AuthorId}&start={CurrentPage * 20}";
                Console.WriteLine($"Fetching page {CurrentPage}: {url}");
                var document = await context.OpenAsync(url);
                var results = document.QuerySelectorAll("[class^='search post']");

                if (results.Length == 0)
                {
                    Console.WriteLine("No more posts found");
                    break;
                }

                foreach (var result in results)
                {
                    var postLink = result.QuerySelector("h3 a").GetAttribute("href");

                    // Skip this post if we have already scraped it
                    if (ScrapedPostsUrls.Contains(postLink))
                    {
                        Console.WriteLine("Encountered previously scraped post, stopping");

                        if (CollectedPosts.Count > 0)
                            SaveToFileAllPosts(CollectedPosts, FileCounter);
                        else
                            Console.WriteLine("0 collected posts written.");

                        return;
                    }

                    ScrapedPostsUrls.Add(postLink);


                    var author = result.QuerySelector("a.username").TextContent.Trim();
                    var postTime = DateTime.ParseExact(result.QuerySelector("dd.search-result-date").TextContent.Trim(), "ddd MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);


                    if (stopDate.HasValue && postTime <= stopDate.Value)
                    {
                        Console.WriteLine("Reached previously scraped post, stopping");

                        if (CollectedPosts.Count > 0)
                            SaveToFileLatestPosts(CollectedPosts);
                        else
                            Console.WriteLine("0 collected posts written.");

                        return;
                    }


                    var postTitle = result.QuerySelector("h3").TextContent.Trim();
                    //var postLink = result.QuerySelector("h3 a").GetAttribute("href");
                    var postId = postLink.Split('#')[1].Substring(1);
                    var postPageRequest = new RestRequest(postLink.Substring(1), Method.Get);
                    var postPageResponse = client.Execute(postPageRequest);

                    var postPageParser = new HtmlParser();
                    var postPage = await postPageParser.ParseDocumentAsync(postPageResponse.Content);

                    var correctPost = GetCorrectPost(postPage, $"p{postId}");

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

                    if (!postPageResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error fetching post page: {postPageResponse.StatusCode}");
                        continue;
                    }

                    var postTimeStr = postTime.ToString("ddd MMM d, yyyy h:mm tt");

                    if (CollectedPosts.Count > TotalPosts && SeenPostDatetimes.Contains(postTimeStr))
                    {
                        Console.WriteLine("Encountered duplicate post datetime after exceeding total post count, stopping");

                        if (CollectedPosts.Count > 0)
                            SaveToFileLatestPosts(CollectedPosts);
                        else
                            Console.WriteLine("0 collected posts written.");

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
                }


                await Task.Delay(TimeSpan.FromSeconds(2));

                CurrentPage++;
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

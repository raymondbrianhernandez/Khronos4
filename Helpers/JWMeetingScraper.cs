using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Khronos4.Helpers
{
    public class JWMeetingScraper
    {
        public async Task<Dictionary<string, string>> ScrapeMeetingDetailsAsync(string url)
        {
            var meetingData = new Dictionary<string, string>();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.5735.110 Safari/537.36",
                    JavaScriptEnabled = true,
                    ExtraHTTPHeaders = new Dictionary<string, string>
                    {
                        { "Accept-Language", "en-US,en;q=0.9" }
                    }
                });

                var page = await context.NewPageAsync();
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                // ✅ Extract the Bible Verses for the Week
                var weeklyBibleVersesElement = await page.QuerySelectorAsync(".jsBibleLink strong");
                meetingData["Weekly Bible Verses"] = weeklyBibleVersesElement != null
                    ? await weeklyBibleVersesElement.InnerTextAsync()
                    : "Not Found";


                // ✅ Extract All Songs (Opening, Middle, Closing) by Selecting All h3.dc-icon--music
                var songElements = await page.QuerySelectorAllAsync("//h3[contains(., 'Song')]");
                if (songElements.Count >= 3)
                {
                    meetingData["Opening Song"] = trimSong(await songElements[0].InnerTextAsync());
                    meetingData["Middle Song"] = await songElements[1].InnerTextAsync(); // No need to trim mid song
                    meetingData["Closing Song"] = trimSong(await songElements[2].InnerTextAsync());
                }
                else
                {
                    meetingData["Opening Song"] = songElements.Count > 0 ? trimSong(await songElements[0].InnerTextAsync()) : "Not Found";
                    meetingData["Middle Song"] = songElements.Count > 1 ? await songElements[1].InnerTextAsync() : "Not Found";
                    meetingData["Closing Song"] = songElements.Count > 2 ? trimSong(await songElements[2].InnerTextAsync()) : "Not Found";
                }

                // ✅ Extract "Treasures from God's Word" Section
                var treasuresSection = await page.QuerySelectorAsync("div.dc-bleedToArticleEdge h3");
                meetingData["Treasures Talk Part"] = treasuresSection != null ? await treasuresSection.InnerTextAsync() : "Not Found";
                meetingData["Treasures Talk Time"] = "(10 min.)";

                // ✅ Set Spiritual Gems
                meetingData["Spiritual Gems Part"] = "2. Spiritual Gems";
                meetingData["Spiritual Gems Time"] = "(10 min.)";

                // ✅ Set Bible Reading
                meetingData["Bible Reading Part"] = "3. Bible Reading";
                meetingData["Bible Reading Time"] = "(4 min.)";

                // ✅ Extract Student Assignments (Gold Font)
                var studentAssignments = await page.QuerySelectorAllAsync("h3.du-fontSize--base.du-color--gold-700");
                int studentCount = 1;
                foreach (var assignment in studentAssignments)
                {
                    string title = await assignment.InnerTextAsync();
                    string time = "Not Found";

                    // Extract time for this assignment
                    var timeElement = await assignment.EvaluateAsync<string>("el => el.nextElementSibling?.querySelector('p')?.innerText");
                    if (!string.IsNullOrEmpty(timeElement))
                    {
                        time = trimTime(timeElement.Trim()); // Apply trimTime() function
                    }

                    meetingData[$"Student Assignment {studentCount} Part"] = title;
                    meetingData[$"Student Assignment {studentCount} Time"] = time;
                    studentCount++;
                }

                // ✅ Extract Elder Assignments (Maroon Font)
                var elderAssignments = await page.QuerySelectorAllAsync("h3.du-fontSize--base.du-color--maroon-600");
                int elderCount = 1;
                foreach (var assignment in elderAssignments)
                {
                    string title = await assignment.InnerTextAsync();
                    string time = "Not Found";

                    // Extract time for this assignment
                    var timeElement = await assignment.EvaluateAsync<string>("el => el.nextElementSibling?.querySelector('p')?.innerText");
                    if (!string.IsNullOrEmpty(timeElement))
                    {
                        time = trimTime(timeElement.Trim()); // Apply trimTime() function
                    }

                    meetingData[$"Elder Assignment {elderCount} Part"] = title;
                    meetingData[$"Elder Assignment {elderCount} Time"] = time;
                    elderCount++;
                }

                return meetingData;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string> { { "Error", ex.Message } };
            }
        }

        private string trimSong(string songText)
        {
            if (string.IsNullOrEmpty(songText)) return "Not Found";

            // If already in correct format, return it as is
            if (System.Text.RegularExpressions.Regex.IsMatch(songText, @"^Song \d+$"))
                return songText;

            // Extract only "Song XX" using regex
            var match = System.Text.RegularExpressions.Regex.Match(songText, @"Song \d+");
            return match.Success ? match.Value : "Not Found";
        }

        private string trimTime(string inputText)
        {
            if (string.IsNullOrEmpty(inputText)) return "Not Found";

            // Extract only "(X min.)" using regex
            var match = System.Text.RegularExpressions.Regex.Match(inputText, @"\(\d+ min\.\)");
            return match.Success ? match.Value : "Not Found";
        }

    }
}

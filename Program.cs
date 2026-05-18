using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Dynamic;

namespace PlaywrightApp
{
    class Program
    {
        //
        //Major Variables and constants
        //
        //contains path for url for gemini
        private const string ProfilePath = "PlaywrightProfile";
        private static readonly string url = "https://gemini.google.com/";

        private static void ClearCurrentPromptLine()
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            var lineToClear = Math.Max(Console.CursorTop - 1, 0);
            Console.SetCursorPosition(0, lineToClear);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, lineToClear);
        }

        public static async Task Main(string[] args)
        {

           //
           // Create an instance of Playwright and launch a Chromium browser. The browser will be launched in non-headless mode
           //
           // contains vasiable for the browser instance
           // and also indicates the browsers mode and user agent to mimic a real user
            using var playwright = await Playwright.CreateAsync();
            var context = await playwright.Chromium.LaunchPersistentContextAsync(ProfilePath, new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = false,
                    IgnoreDefaultArgs = new[] { "--enable-automation" },
                    Args = new[] {
                        "--disable-blink-features=AutomationControlled",
                        "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
                    },
                    Permissions = new[] 
                        { 
                            "geolocation", 
                            "midi", 
                            "notifications", 
                            "push", 
                            "camera", 
                            "microphone", 
                            "background-sync", 
                            "ambient-light-sensor", 
                            "accelerometer", 
                            "gyroscope", 
                            "magnetometer", 
                            "accessibility-events", 
                            "clipboard-read", 
                            "clipboard-write", 
                            "payment-handler" 
                        }
                });

            //
            //Browser context code for managing independent sessions
            //
            
            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 1200000
            });

            // This is here to check if there is an existing authentication state file, And do the needfull, just read.
            var SigninButton = page.GetByRole(AriaRole.Link, new() { Name = "Sign in" });
           
            if (await SigninButton.IsVisibleAsync())
            {
                page.GotoAsync("https://accounts.google.com/", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 1200000
                }).GetAwaiter().GetResult();
                Console.WriteLine("Login Here and cliclk enter when done.");
                Console.ReadLine();
                Console.WriteLine("Session automatically saved to your profile folder!");
            }else{
                Console.WriteLine("Existing session found and loaded from profile folder!");
            }
            // Just incase the sidebar doesnt open automatically
            try
            {
                // Wait up to 3 secs for the sidebar to naturally appear
                await page.Locator("[data-test-id=\"all-conversations\"]").WaitForAsync(new() 
                { 
                    State = WaitForSelectorState.Visible, 
                    Timeout = 7000 
                });
            }
            catch (TimeoutException)
            {
                // If 3 secs pass and it's still missing, click the button
                Console.WriteLine("Sidebar remained hidden. Toggling it open manually...");
                await page.Locator("[data-test-id=\"side-nav-menu-button\"]").ClickAsync();
            }
            //
            // Creates a new page that loads to gemini and intaracts 
            //


            
            Console.WriteLine("Opened Gemini Successfully!");
            
            // infinite loop for main interface, will break when Gemini says goodbye
            while (true)
            {
                Console.Write("\nUser: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    ClearCurrentPromptLine();
                    continue;
                }
                //
                // Perfect place for tech conditins to check for specific commands or inputs from the user in the future.
                //
                
                // First command to check if the user wants to switch models.
                if (userInput == "/model")
                {
                    var menuButton = page.Locator("[data-test-id=\"bard-mode-menu-button\"]");
                    var fastModelButton = page.Locator("[data-test-id=\"bard-mode-option-fast\"]");
                    var thinkingModelButton = page.Locator("[data-test-id=\"bard-mode-option-thinking\"]");
                    var proModelButton = page.Locator("[data-test-id=\"bard-mode-option-pro\"]");


                    Console.WriteLine("1. Fast \n2. Thinking \n3. Pro");
                    var toolInput = Console.ReadLine();
                    switch (toolInput)
                    {
                        case "1":
                            await menuButton.ClickAsync();
                            
                            if (await fastModelButton.IsDisabledAsync())
                            {
                                Console.WriteLine("Fast model is currently unavailable. Please choose another model.");
                                break;
                            }else{
                                await fastModelButton.ClickAsync();
                                Console.WriteLine("Switched to Fast model.");
                            }
                     
                            break;
                        case "2":
                            await menuButton.ClickAsync();
                            if (await menuButton.IsDisabledAsync())
                            {
                                Console.WriteLine("Thinking model is currently unavailable. Please choose another model.");
                                break;
                            }else{
                                await thinkingModelButton.ClickAsync();
                                Console.WriteLine("Switched to Thinking model.");
                            }
                            
                            
                            break;
                        case "3":
                            await menuButton.ClickAsync();
                            if (await proModelButton.IsDisabledAsync())
                            {
                                Console.WriteLine("Pro model is currently unavailable. Please choose another model.");
                                break;
                            }else{
                                await proModelButton.ClickAsync();
                                Console.WriteLine("Switched to Pro model.");
                            }
                            
                            break;
                        default:
                            Console.WriteLine("Invalid input. Please enter 1, 2, or 3.");
                            break;
                    }
                    continue; // Skip the rest of the loop and prompt for user input agains
                }

                //
                // Navigate Chat history
                //

                /*
                    -User uses command "/history" to view past interactions.
                    [
                    if (userInput == "/history")
                    {
                     -past intaractions get listed with a number next to them.
                        [
                           -Extract all locatrs in the main locator "Locator("[data-test-id=\"all-conversations\"]")"
                           -loop through eaach locator and extract the text content
                           -ignore these locator "Locator("[data-test-id=\"bot-list-side-nav-entry-button\"] [data-test-id=\"side-nav-entry-button\"]")","GetByRole(AriaRole.Heading, new() { Name = "Chats" })"
                           -Display the text of the rest in an ordered listwith numbers
                           -Ask user to input a number to select the interaction they want to view and use ClickAsync() on the corresponding locator
                           -Scrape info from the wholescren, both user and gemini respponses(most complex)
                           
                        ]

                    }
                */

                if (userInput == "/history")
                {
                    var chatLogsParentLocator = page.Locator("[data-test-id=\"all-conversations\"]");
               

                    var chatLogLocators = page.Locator("[data-test-id='all-conversations'] a");
                    var chatHistoryListLocators = new List<string>();

                    var count = await chatLogLocators.CountAsync();
                    Console.WriteLine("\nChat History:");


                    for (int i = 0; i < count; i++){
                            var chatLogLocator = chatLogLocators.Nth(i);


                            var ListLogs = await chatLogLocator.InnerTextAsync();

                            int displayNum = chatHistoryListLocators.Count + 1;


                            ListLogs = ListLogs.Trim();

                            Console.WriteLine($"{displayNum}. {ListLogs}");

                             chatHistoryListLocators.Add(ListLogs);
                    }
                    if (chatHistoryListLocators.Count == 0)
                    {
                        Console.WriteLine("No past interactions found.");
                        continue;
                    }
                    //

                    // After listing the chat history, prompt the user to select a chat to view in detail.
                    Console.WriteLine("\nSelect a chat to view");

                    var historyInput = Console.ReadLine();
                    if (int.TryParse(historyInput, out int historyIndex) && historyIndex > 0 && historyIndex <= chatHistoryListLocators.Count)
                    {   
                        var selectedChatLocator = chatLogLocators.Nth(historyIndex - 1);
                        await selectedChatLocator.ClickAsync();

                        Console.WriteLine("Do you want to see the chat options for this chat? Use 'Y' for yes or 'N' for no to proceed.");

                        var optionsInput = Console.ReadLine().Trim().ToUpper();
                        while (true)
                        {
                            if (optionsInput == "Y")
                                {
                                    Console.WriteLine("{Chat options} \n1.Share conversation: \n2.Pin:\n3.Rename:\n4.Add to notebook:\n5.Delete:");
                                    var optionSelection  = Console.ReadLine().Trim();
                                    switch (optionSelection)
                                    {
                                        case "1":
                                            Console.WriteLine("Share conversation selected.");
                                            // learn: Use Playwright selector thingy, 
                                            // old , apperantly html doesnt do button in button and makes them siblings : await selectedChatLocator.GetByRole(AriaRole.Button, new() { Name = "More options for Greeting" }).ClickAsync();
                                            // await page.GetByRole(AriaRole.Button).RightOf(selectedChatLocator).First.ClickAsync();
                                            // RightOf doesnt exist apperently
                                            await selectedChatLocator.Locator("..").GetByRole(AriaRole.Button, new() { Name = "More options for Greeting" }).ClickAsync();
                                            await page.GetByRole(AriaRole.Menuitem, new() { Name = "Share conversation" }).ClickAsync();
                                            await page.Locator("[data-test-id=\"copy-link\"]").ClickAsync();
                                            await Task.Delay(500); // Small delay to ensure clipboard has the link uk.

                                            // get the link from clipboard
                                            var chatLink = await page.EvaluateAsync<string>("navigator.clipboard.readText()");
                                            Console.WriteLine($"Chat link: {chatLink}");
                                            await page.Locator("[data-test-id=\"close-dialog\"]").ClickAsync();
                                            Console.WriteLine("Returning to chat list.");
                                            break;
                                            /*
                                                    await page1.GetByRole(AriaRole.Button, new() { Name = "More options for Greeting and" }).ClickAsync();
                                                    await page1.GetByRole(AriaRole.Menuitem, new() { Name = "Share conversation" }).ClickAsync();
                                                    await page1.Locator("[data-test-id=\"copy-link\"]").ClickAsync();
                                                    await page1.Locator("[data-test-id=\"close-dialog\"]").ClickAsync();
                                            */
                                        case "2":
                                            Console.WriteLine("Pin selected. (Not implemented yet)");
                                            break;
                                        case "3":
                                            Console.WriteLine("Rename selected. (Not implemented yet)");
                                            break;
                                        case "4":
                                            Console.WriteLine("Add to notebook selected. (Not implemented yet)");
                                            break;
                                        case "5":
                                            Console.WriteLine("Delete selected. (Not implemented yet)");
                                            break;
                                        default:
                                            Console.WriteLine("Invalid input. Please enter a number from 1 to 5 corresponding to the chat options.");
                                            break;
                                    }
                                    break;

                                }else if (optionsInput == "N")
                                {
                                    Console.WriteLine("Skipping chat options.");
                                    continue;
                                }else{
                                    Console.WriteLine("Invalid input. Please enter 'Y' or 'N'.");
                                    continue;
                                }  
                        }
  
                        
                        //
                        //Scrape Full convo , abit more complex
                        //


                    }
                    else                    {
                        Console.WriteLine("Invalid input. Please enter a valid number from the chat history list.");
                    }
                    continue;
                }


                //
                //Tool selection code should be here
                //

                if (userInput.StartsWith("/tool"))
                {

                    var toolsButton = page.GetByRole(AriaRole.Button, new() { Name = "Tools" });
                    await toolsButton.ClickAsync();


                    Console.WriteLine("Tool selection is not implemented yet. Please try again later.");
                    continue; // Skip the rest of the loop and prompt for user input again
                }

              

                // if condition to check if the user input is empty.
                if (string.IsNullOrEmpty(userInput)) continue;

                // More intaraction stuff.
                await page.Locator("[data-test-id=\"textarea-inner\"]").GetByRole(AriaRole.Paragraph).ClickAsync();
                var textBox = page.GetByRole(AriaRole.Textbox, new() { Name = "Enter a prompt for Gemini" });

                // This is where the user input is filled into the text box on the Gemini interface.
                await textBox.FillAsync(userInput);


                var responseCount = await page.Locator(".container").CountAsync();

               
                var textButton = page.GetByRole(AriaRole.Button, new() { Name = "Send message" });
                await textButton.ClickAsync();

                Console.WriteLine("Waiting for Gemini to respond...");


                var latestReponseLocator = page.Locator(".container").Nth(responseCount);
                
                await latestReponseLocator.WaitForAsync(new() { State = WaitForSelectorState.Attached });

                string containerText = "";
                string previousText = "";
                int stabilityCounter = 0;

                //
                // Another loop to check constantly if the response is still being generated
                //

                while (true)
                {
                    await Task.Delay(500);

                    containerText = await latestReponseLocator.TextContentAsync() ?? "";
                    containerText = containerText.Trim();
                    if (!string.IsNullOrEmpty(containerText) && containerText == previousText)
                    {
                        stabilityCounter++;
                        if (stabilityCounter >= 2) // Confirmed stable for 1 full second
                        {
                            break;
                        }
                    }
                    else
                    {
                        previousText = containerText;
                        stabilityCounter = 0; // Reset counter if text is still growing
                    }

                }


                Console.WriteLine("Gemini has responded.");
                Console.WriteLine("Gemini: {0}", containerText);

                if (containerText.Contains("Goodbye", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Gemini has said goodbye. Ending the conversation.");
                    break;
                }
            }
        }
    }
}

/*       await Page.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Create image New" }).ClickAsync();
using Microsoft.Playwright.MSTest;
using Microsoft.Playwright;

[TestClass]
public class Tests : PageTest
{
    [TestMethod]
    public async Task MyTest()
    {
        await Page.Locator("[data-test-id=\"textarea-inner\"]").GetByRole(AriaRole.Par
        agraph).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Enter a prompt for Gemini" }).FillAsync("Hello");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send message" }).ClickAsync();
        await Page.GotoAsync("https://gemini.google.com/app/15be7562cf230acd");
        await Page.Locator(".container").ClickAsync();
    }
}

*/

//Second Output : Locator(".model-response-text.contains-extensions-response > .container")

//The diffrent states of the loading button
//await Page.Locator(".avatar.avatar_primary.ng-tns-c1545715222-21").ClickAsync();
//await Page.Locator(".avatar-gutter.ng-tns-c3883428771-22").ClickAsync();
/*
        await Page.Locator("[data-test-id=\"bard-mode-menu-button\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-option-fast\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-menu-button\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-option-fast\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-menu-button\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-option-thinking\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-menu-button\"]").ClickAsync();
        await Page.Locator("[data-test-id=\"bard-mode-option-pro\"]").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Create image New" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deselect Create image" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Canvas" }).ClickAsync();
        await Page.Locator("[data-test-id=\"close-button\"]").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deselect Canvas" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Create music New" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deselect Create music" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Guided learning" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deselect Guided learning" }).ClickAsync();
 


*/

Playwright Gemini Bot DocumentationThis project is a high-performance C# console application that automates interactions with Google Gemini using Microsoft Playwright for .NET. The bot includes advanced automation practices such as authentication session persistence, dynamic streaming output monitoring, and smart element targeting.1. Bot Architecture & PlanSetup PhasePlaywright Engine Initialization: Start the Playwright driver.Browser Configuration: Launch Chromium in non-headless mode (Headless: false) so the user can monitor operations. We use custom args (--disable-blink-features=AutomationControlled) and a real-world user-agent string to mimic natural human browser behavior and bypass simple bot detection.Persistent Session Management: Check for an existing auth.json file.If found, load the authenticated session state directly (skipping the login step).If missing, open a browser window pointing to Google Accounts, pause for the user to complete login manually, and then serialize and save the browser storage state (cookies, localStorage) to auth.json.Automation & Interaction LoopCommand Line Interface (CLI): Prompt the user for input in the terminal. Empty inputs are ignored using guard clauses to prevent UI deadlocks.Focus & Prompt Entry:Click/focus the prompt text area placeholder.Retrieve the active Textbox role locator and dynamically fill it with user input.Send Action: Locate the "Send message" button by its ARIA role and execute an asynchronous click.Response Sizing Guard: Retrieve and count the total number of .container elements present before sending. This allows the bot to know the precise index (.Nth(responseCount)) of the next incoming AI response.Streaming & Stability Monitor: Run a checking loop that captures the text content of the target container every 500 milliseconds. If the text length remains completely identical for two consecutive checks (1 full second of stability), the streaming is confirmed finished.State Termination: If Gemini outputs a goodbye sequence, break the loop and gracefully shut down.2. Technical Deep-DivesHow Session Persistence WorksBypassing multi-factor authentication (MFA) or login security checks programmatically is notoriously unreliable and brittle. Instead, this bot uses a Hybrid Authentication Pattern:// Saving state after user logs in manually once
await context.StorageStateAsync(new() { Path = "auth.json" });

// Restoring state on subsequent runs
context = await browser.NewContextAsync(new() { StorageStatePath = "auth.json" });
This stores session cookies and security tokens directly in a local JSON file, meaning you only need to log in manually the very first time you launch the bot.The Streaming Stability Loop (Dynamic Wait)Because Large Language Models (LLMs) stream text token-by-token, a standard WaitForAsync or simple Delay is insufficient. The bot monitors the text container's rate of change:while (true)
{
    await Task.Delay(500);
    containerText = await latestResponseLocator.TextContentAsync() ?? "";

    if (!string.IsNullOrEmpty(containerText) && containerText == previousText)
    {
        stabilityCounter++;
        if (stabilityCounter >= 2) // Remains unchanged for 1.0 second
        {
            break; 
        }
    }
    else
    {
        previousText = containerText;
        stabilityCounter = 0; // Reset if text is still streaming/growing
    }
}
3. Complete Source Codeusing System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightApp
{
    class Program
    {
        // Path to store authentication cookies/session state
        private const string StatePath = "auth.json";
        private static readonly string Url = "https://gemini.google.com/";

        public static async Task Main(string[] args)
        {
            // Initialize Playwright engine
            using var playwright = await Playwright.CreateAsync();
            
            // Launch Chromium in non-headless mode to allow monitoring and manual login
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false, // Must be false to view browser operations and handle login
                IgnoreDefaultArgs = new[] { "--enable-automation" },
                Args = new[] {
                    "--disable-blink-features=AutomationControlled", // Obfuscates WebDriver flag
                    "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
                }
            });

            IBrowserContext context;

            // Check if authentication session already exists
            if (File.Exists(StatePath) && new FileInfo(StatePath).Length > 0)
            {
                Console.WriteLine("Loading authentication state from file...");
                context = await browser.NewContextAsync(new()
                {
                    StorageStatePath = StatePath
                });
            }
            else
            {
                Console.WriteLine("No authentication state file found. Creating a new context...");
                context = await browser.NewContextAsync();

                var loginPage = await context.NewPageAsync();
                await loginPage.GotoAsync("https://accounts.google.com");

                Console.WriteLine("Please log in to your Google Account in the opened window.");
                Console.WriteLine("Press ENTER in this console once login is completely finished.");
                Console.ReadLine();
                
                Console.WriteLine("Saving authentication state to file...");
                await context.StorageStateAsync(new()
                {
                    Path = StatePath
                });
                Console.WriteLine($"\nAuthentication state saved successfully to '{StatePath}'");
            }

            // Open the automation page and navigate to Gemini
            var page = await context.NewPageAsync();
            await page.GotoAsync(Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 1200000 // High timeout limit for slow connections
            });
            
            Console.WriteLine("Opened Gemini Successfully!");
            
            // Primary interactive UI Loop
            while (true)
            {
                Console.Write("\nUser: ");
                var userInput = Console.ReadLine();

                // Guard clause: ignore empty prompts
                if (string.IsNullOrEmpty(userInput)) continue;

                // Focus the typing container placeholder
                await page.Locator("[data-test-id=\"textarea-inner\"]").GetByRole(AriaRole.Paragraph).ClickAsync();
                
                // Locate the active prompt textbox element and insert user input
                var textBox = page.GetByRole(AriaRole.Textbox, new() { Name = "Enter a prompt for Gemini" });
                await textBox.FillAsync(userInput);

                // Count current .containers to index the upcoming new response
                var responseCount = await page.Locator(".container").CountAsync();

                // Locate and click the Send button
                var textButton = page.GetByRole(AriaRole.Button, new() { Name = "Send message" });
                await textButton.ClickAsync();

                Console.WriteLine("Waiting for Gemini to respond...");

                // Locate the specific container that will hold the incoming response (.Nth is 0-indexed)
                var latestResponseLocator = page.Locator(".container").Nth(responseCount);
                await latestResponseLocator.WaitForAsync(new() { State = WaitForSelectorState.Attached });

                string containerText = "";
                string previousText = "";
                int stabilityCounter = 0;

                // Stability monitor loop: Checks if streaming has finished
                while (true)
                {
                    await Task.Delay(500);

                    containerText = await latestResponseLocator.TextContentAsync() ?? "";
                    containerText = containerText.Trim();

                    // If text remains identical across multiple intervals, generation has completed
                    if (!string.IsNullOrEmpty(containerText) && containerText == previousText)
                    {
                        stabilityCounter++;
                        if (stabilityCounter >= 2) // Unchanged for 1.0 second
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

                Console.WriteLine("\nGemini has responded.");
                Console.WriteLine("Gemini: {0}", containerText);

                // Exit sequence check
                if (containerText.Contains("Goodbye", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Gemini sent a goodbye token. Ending conversation.");
                    break;
                }
            }
            
            // Clean up and close browser
            await browser.CloseAsync();
        }
    }
}
4. Troubleshooting & TipsSelect/Locator Multiplicity Errors: If Playwright throws a strict mode violation error regarding .container, ensure you are counting current containers with CountAsync() first and targeting using .Nth(responseCount) to isolate your active message.Updating Selectors: If Google updates their front-end interface, the selector for the target container may change. You can switch .container out for the more specific target:Locator(".model-response-text.contains-extensions-response > .container")Bypassing Captchas: Running the Chromium browser with Headless = false and real-user simulation headers dramatically reduces the frequency of bot-mitigation triggers.
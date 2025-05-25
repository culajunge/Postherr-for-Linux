using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SmorcIRL.TempMail;
using System.Text;
using Newtonsoft.Json.Linq;
using SmorcIRL.TempMail.Models;
using System.Text.RegularExpressions;
using System.Text.Json;
using LinuxGlobalHotkeys;

class Program
{
    static string versionIdentifier = "v1.4.2l";

    #region Debug

    #endregion

    #region NameGen

    static string[] firstWord =
    {
        "funny", "sad", "lovely", "sophisticated", "dumb", "numb", "random", "hilarious", "silly", "suicidal",
        "spanish", "lonely"
    };

    static string[] secondWord =
    {
        "Driver", "Pedestrian", "Biker", "Addict", "Soldier", "JudyHoppsLover69", "Dude", "Guy", "Cop", "Fighter",
        "Murderer", "Gamer", "Programmer", "ArchUser"
    };

    static int numRange = 200;

    public static async Task<string> GetCoolUsername()
    {
        Random random = new Random();

        string randNum = random.Next(numRange).ToString();
        string name = await GetRandomAdjectiveNounAsync();

        if (String.IsNullOrEmpty(name))
        {
            name = (firstWord[random.Next(firstWord.Length)] + secondWord[random.Next(secondWord.Length)]);
        }

        string result = name + randNum;

        return result;
    }

    private static readonly HttpClient client = new HttpClient();
    private const string BaseUrl = "https://api.msmc.cc/api/dictionary/random";

    public static async Task<string> GetRandomAdjectiveNounAsync()
    {
        try
        {
            Random rand = new Random();
            string randomLetter = lower[rand.Next(lower.Length)].ToString();
            // Get a random adjective
            string adjective = await GetRandomWordAsync($"/a");
            adjective = adjective.ToLower();
            // Get a random noun
            string noun = await GetRandomWordAsync($"/n");
            noun = CapitalizeFirstLetter(noun);

            Log($"adj: {adjective}, noun: {noun}");
            // Combine the two words
            return $"{adjective}{noun}";
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the HTTP request
            Log($"An error occurred on username creation: {ex.Message}");
            return null;
        }
    }

    public class WordResponse
    {
        public string word { get; set; }
        public string pos { get; set; }
        public string[] definitions { get; set; }
    }

    private static async Task<string> GetRandomWordAsync(string endpoint)
    {
        // Make the GET request to the API
        HttpResponseMessage response = await client.GetAsync(BaseUrl + endpoint);

        // Ensure the request was successful
        response.EnsureSuccessStatusCode();

        // Read the response content as a string
        string jsonResponse = await response.Content.ReadAsStringAsync();


        try
        {
            // Deserialize as a single object, not an array
            WordResponse wordResponse = JsonSerializer.Deserialize<WordResponse>(jsonResponse);

            string word = wordResponse.word;
            if (word.Contains(";"))
            {
                word = word.Split(';')[0].Trim();
            }

            return word;
        }
        catch (JsonException)
        {
            // Handle JSON parsing errors
            Log("Failed to parse JSON response.");
            return null;
        }
    }


    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Capitalize the first letter and concatenate with the rest of the string
        input = input.ToLower();
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    #endregion

    #region Facts

    const string factFileName = "Facts.txt";

    public static async Task<string> GetRandomContent(string filePath = factFileName)
    {
        Random random = new Random();
        bool useFile = random.Next(2) == 0;

        string fact = "";
        if (useFile)
        {
            fact = GetRandomLine();
        }
        else
        {
            fact = await GetRandomFact();
        }

        if (fact == "ERROR")
        {
            fact = GetRandomLine();
            Log("Fact from API failed, using fact from file instead :(");
        }

        return string.IsNullOrEmpty(fact) ? "Error: Fact File not available" : fact;
    }

    private static List<string> _facts = new List<string>();

    public static bool InitializeFacts()
    {
        try
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(appDirectory, factFileName);

            //Just assuming the fact file is fine :)

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
            {
                Log("Error: The fact file is empty.");
                return false;
            }

            _facts = new List<string>(lines);
            Log($"Facts loaded successfully. Total facts: {_facts.Count}");
            return true;
        }
        catch (IOException ex)
        {
            Log($"IOException during fact initialization: {ex.Message}");
            AnalyzeFactFileIssue();
            return false;
        }
        catch (Exception ex)
        {
            Log($"Unexpected error during fact initialization: {ex.Message}");
            AnalyzeFactFileIssue();
            return false;
        }
    }


    static void AnalyzeFactFileIssue()
    {
        try
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(appDirectory, factFileName);

            Log($"Analyzing issues with the fact file: {filePath}");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                Log("Error: The fact file seems to not exist, even though it may.");
            }

            // Check if the file is accessible
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    Log("Fact file exists and is accessible.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Log("Error: The application does not have the required permissions to access the fact file.");
                return;
            }
            catch (IOException ex)
            {
                Log($"Error: The fact file is locked or inaccessible due to an IO issue: {ex.Message}");
                return;
            }

            // Check if the file is empty
            if (new FileInfo(filePath).Length == 0)
            {
                Log("Error: The fact file is empty.");
            }
        }
        catch (Exception ex)
        {
            Log($"Unexpected error during fact file analysis: {ex.Message}");
        }
    }

    public static string GetRandomLine()
    {
        try
        {
            if (_facts == null || _facts.Count == 0)
            {
                Log("Error: No facts are loaded or the fact file is empty.");
                return "Error: No facts available. Please check the file.";
            }

            Random random = new Random();
            int randomIndex = random.Next(_facts.Count);
            return _facts[randomIndex];
        }
        catch (Exception ex)
        {
            Log($"Unexpected error while retrieving a random fact: {ex.Message}");
            return "Error: " + ex.Message;
        }
    }

    private static bool IsFileReady(string filePath)
    {
        try
        {
            using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                return true;
            }
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static string GetApiKey(string serviceName, string fileName = "apikey.csv")
    {
        try
        {
            // Get the full path to the CSV file in the same directory as the executable
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(appDirectory, fileName);

            // Read all lines from the CSV file
            var lines = File.ReadAllLines(filePath);

            // Skip the header row and search for the matching service name
            var keyLine = lines
                .Skip(1) // Skip the header
                .FirstOrDefault(line => line.StartsWith(serviceName + ","));

            if (keyLine != null)
            {
                // Split the line by the comma and return the second column (the API key)
                var parts = keyLine.Split(',');
                return parts.Length > 1 ? parts[1].Trim() : null;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.Write(ex.ToString());
            return null;
        }
    }

    private static readonly HttpClient factClient = new HttpClient();
    private const string ApiUrl = "https://api.api-ninjas.com/v1/facts";

    public static async Task<string> GetRandomFact(string apiKeyFile = "apikey.csv")
    {
        try
        {
            // Get API Key from the CSV file
            var apiKey = GetApiKey("FactsAPI", apiKeyFile);
            if (string.IsNullOrEmpty(apiKey) || apiKey == null)
            {
                Log("ERROR: API Key not found or invalid.");
                return "ERROR";
            }

            factClient.DefaultRequestHeaders.Clear();
            factClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            var response = await factClient.GetAsync(ApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonArray = JArray.Parse(content);

                if (jsonArray.Count > 0 && jsonArray[0]["fact"] != null)
                {
                    return jsonArray[0]["fact"].ToString();
                }
            }

            Log("ERROR: API Fact retrieval failed.");
            return "ERROR";
        }
        catch (Exception ex)
        {
            Log($"Error during API fact retrieval: {ex.Message}, returned fact from file");
            return "ERROR";
        }
    }

    #endregion

    #region Shortcut

    private static GlobalHotkeyManager _hotkeyManager;
    private static HotkeyConfig _config;
    private static string _configFilePath;

    private static void RegisterAllHotkeysOnStart(string configFilePath = "hotkeys.json")
    {
        _configFilePath = configFilePath;
        _config = HotkeyConfig.LoadFromFile(_configFilePath);
        RegisterAllHotkeys();
    }

    private static void RegisterAllHotkeys()
    {
        // Register all hotkeys from the config
        _hotkeyManager.RegisterShortcut(_config.EmailShortcut, OnHotKeyEmailAddrPressed);
        _hotkeyManager.RegisterShortcut(_config.PasswordShortcut, OnHotKeyPSWDPressed);
        _hotkeyManager.RegisterShortcut(_config.UsernameShortcut, OnHotKeyUsernamePressed);
        _hotkeyManager.RegisterShortcut(_config.VerificationCodeShortcut, OnHotKeyVerificationCodePressed);
        _hotkeyManager.RegisterShortcut(_config.RegenerateAccountShortcut, OnAccountRegenerate);
        _hotkeyManager.RegisterShortcut(_config.KillLoopShortcut, OnEmailLoopEliminate);
        _hotkeyManager.RegisterShortcut(_config.FactShortcut, OnRandomFactPressed);
    }

    public void UpdateConfig(HotkeyConfig newConfig)
    {
        // Dispose the current hotkey manager to unregister all hotkeys
        _hotkeyManager.Dispose();

        // Update the config
        HotkeyConfig.SaveToFile(newConfig, _configFilePath);

        // Create a new hotkey manager and register the new hotkeys
        var newHotkeyManager = new GlobalHotkeyManager();
        _hotkeyManager = newHotkeyManager;
        RegisterAllHotkeys();
    }

    public void Dispose()
    {
        _hotkeyManager?.Dispose();
    }

    #endregion

    #region Window Visibility

// Linux doesn't have the same window handle concept as Windows
// We'll implement a simpler version for Linux
    static bool minimizeOnStart = false;

    static void HideConsoleWindow()
    {
        // On Linux, we can't easily hide the console window from within the app
        // We could start the app with a flag to run in the background
        Log("Console hiding not directly supported on Linux");
    }

    static void ShowConsoleWindow()
    {
        // Similarly, we can't easily show the console window if it was started hidden
        Log("Console showing not directly supported on Linux");
    }

    #endregion

    #region Password Generation

    const string lower = "abcdefghijklmnopqrstuvwxyz";
    const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string digits = "0123456789";
    const string symbols = "!@#$%^&*()_+[]{};:,.?";

    public static string GeneratePassword()
    {
        Random _random = new Random();


        const string allChars = lower + upper + digits + symbols;

        int passwordLength = _random.Next(12, 15); // Generates a length between 8 and 16

        StringBuilder password = new StringBuilder(passwordLength);

        // Ensure the password contains at least one character from each required set
        password.Append(lower[_random.Next(lower.Length)]);
        password.Append(upper[_random.Next(upper.Length)]);
        password.Append(digits[_random.Next(digits.Length)]);
        password.Append(symbols[_random.Next(symbols.Length)]);

        // Fill the rest of the password with random characters from all character sets
        for (int i = 4; i < passwordLength; i++)
        {
            password.Append(allChars[_random.Next(allChars.Length)]);
        }

        // Shuffle the password to prevent the first four characters from being predictable
        return new string(password.ToString().OrderBy(c => _random.Next()).ToArray());
    }

    #endregion

    #region Important Variables or something i guess

    public static bool runningMSGThread = false;
    public static bool killAllMSGThreads = false;

    public static int pasteWaitTime = 2000;
    public static int threadAliveTime = 1200000; //20 minutes
    public static int generateNewAccountTime = 600000; // more that 1,5 minutes (10min)
    public static int retryOnFailTime = 1150; // lil more than a sec

    public static bool typeOut = false;
    public static int typeOutDelayMS = 100;

    public static MailClient currentClient;
    public static string formattedEmailAddress;
    public static string currentPassword;
    public static string currentUsername;
    public static string[] currentVerificationCodes;


    public static bool deleteAccountWhenAbandoned = false;

    public static string newline = "NEWLINE";
    public static string rawBody = "RAWBODY";

    public static string NewLine(int amount)
    {
        string nl = "";

        for (int i = 0; i < amount; i++)
        {
            nl += newline;
        }

        return nl;
    }

    #endregion

    #region Clipboard Action

// Cross-platform clipboard handling
    static void BringToFront()
    {
        // Linux doesn't have a direct equivalent to SetForegroundWindow
        // This functionality would need X11 bindings or xdotool
        // For now, we'll leave this as a placeholder
        Log("BringToFront is not implemented on Linux");
    }

    static void PasteText(string text)
    {
        Thread clipboardThread = new Thread(() =>
        {
            try
            {
                // Save previous clipboard content
                string previousClipBoard = TextCopy.ClipboardService.GetText() ?? "";
                Log("Saved previous clipboard content");

                // Set new text to clipboard
                TextCopy.ClipboardService.SetText(text);
                Log("Set new text to clipboard");

                // Use the configured delay from hotkeys.json
                Thread.Sleep(_config.ClipboardProcessingDelay);

                // Try multiple paste methods for better compatibility
                bool pasteSuccess = false;

                // Method 1: Try wl-paste for Wayland
                if (IsWayland())
                {
                    pasteSuccess = TryWaylandPaste();
                }

                // Method 2: Try xdotool for X11
                if (!pasteSuccess && !IsWayland())
                {
                    pasteSuccess = TryXdotoolPaste();
                }

                // Method 3: Fallback to xclip direct paste
                if (!pasteSuccess)
                {
                    pasteSuccess = TryXclipPaste(text);
                }

                // Use the configured delay again before restoring clipboard
                Thread.Sleep(_config.ClipboardProcessingDelay);

                // Restore previous clipboard content
                if (!string.IsNullOrEmpty(previousClipBoard))
                {
                    TextCopy.ClipboardService.SetText(previousClipBoard);
                    Log("Restored previous clipboard content");
                }

                if (!pasteSuccess)
                {
                    Log(
                        "Warning: All paste methods failed. Text was copied to clipboard but may not have been pasted.");
                }
            }
            catch (Exception ex)
            {
                Log($"Pasting Error: {ex}");
            }
        });

        clipboardThread.Start();
        clipboardThread.Join();

        LogPasted(text);
    }

// Check if running on Wayland
    static bool IsWayland()
    {
        string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        bool arg1 = !string.IsNullOrEmpty(waylandDisplay);

        return arg1 || _config.IsWayland;
    }

// Try pasting using wl-paste on Wayland
    static bool TryWaylandPaste()
    {
        try
        {
            // First check if wl-clipboard is installed
            using (Process checkProcess = new Process())
            {
                checkProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "wl-paste",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                checkProcess.Start();
                string output = checkProcess.StandardOutput.ReadToEnd();
                checkProcess.WaitForExit();

                if (string.IsNullOrEmpty(output))
                {
                    Log("wl-clipboard not found. Please install it with: sudo apt install wl-clipboard");
                    return false;
                }
            }

            // Use ydotool for Wayland (if installed)
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ydotool",
                    Arguments = "key ctrl+v",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(1000);
                Log("Used ydotool to paste in Wayland");
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Log($"Wayland paste attempt failed: {ex.Message}");
            return false;
        }
    }

// Try pasting using xdotool (X11)
    static bool TryXdotoolPaste()
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = "key --clearmodifiers ctrl+v",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(1000);
                Log("Used xdotool to paste in X11");
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Log($"X11 paste attempt failed: {ex.Message}");
            return false;
        }
    }

// Try pasting using xclip (works on both X11 and sometimes Wayland with XWayland)
    static bool TryXclipPaste(string text)
    {
        try
        {
            // Check if xclip is installed
            using (Process checkProcess = new Process())
            {
                checkProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "xclip",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                checkProcess.Start();
                string output = checkProcess.StandardOutput.ReadToEnd();
                checkProcess.WaitForExit();

                if (string.IsNullOrEmpty(output))
                {
                    Log("xclip not found. Please install it with: sudo apt install xclip");
                    return false;
                }
            }

            // Use xclip to set selection and then simulate paste
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments =
                        $"-c \"echo -n '{text.Replace("'", "'\\''")}' | xclip -selection clipboard && xdotool key ctrl+v\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(1000);
                Log("Used xclip direct method to paste");
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Log($"xclip paste attempt failed: {ex.Message}");
            return false;
        }
    }

// Alternative method that doesn't rely on clipboard or key simulation
// This uses xdotool to type the text directly
//
    static void TypeTextDirectly(string text)
    {
        try
        {
            // Escape special characters for shell
            string escapedText = text.Replace("\"", "\\\"").Replace("$", "\\$");

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = $"type \"{escapedText}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit();
            }

            LogPasted(text);
        }
        catch (Exception ex)
        {
            Log($"Direct typing error: {ex}");
        }
    }

    static void OutputText(string txt)
    {
        if (typeOut) TypeTextDirectly(txt);
        else PasteText(txt);
    }

    #endregion

    #region Logging

    public static void Log(string msg, bool skipLine = false)
    {
        Console.WriteLine(msg);
        LogToFile(msg, skipLine);
    }

    public static void LogPasted(string msg)
    {
        Log($"Pasted: {msg}");
    }

    public static void LogToFile(string message, bool skipLine)
    {
        try
        {
            // Get the directory of the executable
            string exeDirectory = AppContext.BaseDirectory;

            // Define the log file path
            string logFilePath = Path.Combine(exeDirectory, "log.txt");

            string skipLineChar = skipLine ? "\n" : "";

            // Append the message to the log file (creates the file if it doesn't exist)
            using (StreamWriter writer = new StreamWriter(logFilePath, append: true))
            {
                writer.WriteLine($"{skipLineChar}{DateTime.Now:dd/MM/yyyy HH:mm} - {message}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error logging message: {ex.Message}");
        }
    }

    static void LogSourceMessage(string? source)
    {
        try
        {
            if (String.IsNullOrEmpty(source))
            {
                Log("Source string is null or empty.");
                return;
            }

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string loggerPath = Path.Combine(exeDirectory, "EmailViewer3", "EmailViewerLinux");

            if (!File.Exists(loggerPath))
            {
                Log("EmailViewer3/EmailViewerLinux does not exist at the specified path.");
                return;
            }

            // Write the source to a temporary file
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, source);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = loggerPath,
                Arguments = $"\"{tempFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
            Log("Logged source to EmailViewerLinux");
        }
        catch (Exception ex)
        {
            Log("ERROR logging source message: " + ex.Message);
        }
    }


    public static string ExtractTextFromHtml(string rawHtml)
    {
        if (string.IsNullOrEmpty(rawHtml))
        {
            Log("No HTML found");
            return string.Empty;
        }

        // Load the HTML into an HtmlDocument
        HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);

        // Use the HtmlDocument's DocumentNode to extract all text content
        string plainText = htmlDoc.DocumentNode.InnerText;

        // Optionally, you can also clean up extra whitespace or newlines
        plainText = System.Web.HttpUtility.HtmlDecode(plainText); // Decode HTML entities
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+",
            " "); // Replace multiple spaces/newlines with a single space
        plainText = plainText.Trim();

        return plainText;
    }

    public static string GetNewlineCompatible(string text)
    {
        if (String.IsNullOrEmpty(text) || String.IsNullOrWhiteSpace(text)) return "";
        return text.Replace("\n", newline);
    }

    public static string[] ExtractVerificationCodes(string emailText, int proximity = 50, string fancyAssText = "")
    {
        // List to store codes along with their proximity
        List<(string Code, int Proximity)> codesWithProximity = new List<(string Code, int Proximity)>();

        // Convert the email text to lowercase for case-insensitive matching
        emailText = emailText.ToLower();

        // Keywords to search for around the verification code
        string[] keywords = { "verification", "verify", "code", "authentication", "authenticate", "verif", "authen" };

        // Regular expression to find 4 or 6 digit numbers
        string codePattern = @"\b\d{4,6}\b";

        bool keywordFound = false;

        foreach (string keyword in keywords)
        {
            // Regex pattern to search for the keyword and a 4-6 digit number within the proximity range
            string pattern = $@"\b{keyword}\b.{{0,{proximity}}}?" + codePattern + $@"|" + codePattern +
                             $@".{{0,{proximity}}}?\b{keyword}\b";
            MatchCollection matches = Regex.Matches(emailText, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                keywordFound = true;
                // Extract the code from the matched string
                Match codeMatch = Regex.Match(match.Value, codePattern);
                if (codeMatch.Success)
                {
                    // Calculate the proximity (distance between keyword and code)
                    int keywordIndex = match.Value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                    int codeIndex = match.Value.IndexOf(codeMatch.Value, StringComparison.OrdinalIgnoreCase);
                    int distance = Math.Abs(keywordIndex - codeIndex);

                    // Add the code and its proximity to the list
                    codesWithProximity.Add((codeMatch.Value, distance));
                }
            }
        }

        if (!keywordFound)
        {
            if (!String.IsNullOrEmpty(fancyAssText) && fancyAssText != "" && fancyAssText != empty)
            {
                emailText = fancyAssText.ToLower();
            }

            // If no keywords are found, fallback to finding the code closest to the center of the text
            int centerPosition = emailText.Length / 2;
            MatchCollection matches = Regex.Matches(emailText, codePattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                // Calculate the proximity (distance to the center of the text)
                int codeIndex = match.Index;
                int distanceToCenter = Math.Abs(centerPosition - codeIndex);

                // Add the code and its distance to the center to the list
                codesWithProximity.Add((match.Value, distanceToCenter));
            }
        }

        // Sort the codes by proximity (smallest proximity first)
        var sortedCodes = codesWithProximity.OrderBy(c => c.Proximity).Select(c => c.Code).ToArray();

        // Return the sorted codes, ensuring no duplicates
        return sortedCodes.Distinct().ToArray();
    }

    public static string GetLogo()
    {
        return "LOGO";
    }

    #endregion

    static async Task<MailClient> GenerateMailClient()
    {
        Random rand = new Random();
        MailClient client = new();

        var domain = await client.GetFirstAvailableDomainName();
        string customUsername = await GetCoolUsername();
        string customEmailAddress = $"{customUsername}@{domain}";
        string password = GeneratePassword();

        try
        {
            await client.Register(customEmailAddress, password);
        }
        catch (Exception ex)
        {
            Log($"ERROR creating new account: {ex.Message}");
            Log("Postherr stopped");
            Log($"Bad Disposable Email Address: {customEmailAddress}");
            Log($"Bad Password: {password}");
            Log("Retry in 5 seconds...");

            Thread.Sleep(5000);
            await GenerateMailClient();
            return currentClient;
        }

        var account = await client.GetAccountInfo();

        Log($"Disposable Email Address: {account.Address}");
        Log($"Password: {password}");

        currentClient = client;
        currentPassword = password;
        currentUsername = customUsername;
        formattedEmailAddress = customEmailAddress;

        return client;
    }


    static async Task SetupTempMailsNShit()
    {
        await GenerateMailClient();
    }

    static string empty = "EMPTY";

    static async void CheckNewMessages(MailClient currentClient)
    {
        bool running = true;
        bool runningExitThread = false;
        while (running)
        {
            var messages = await currentClient.GetAllMessages();

            foreach (var message in messages)
            {
                var messageDetails = await currentClient.GetMessage(message.Id);

                MessageSource source = await currentClient.GetMessageSource(message.Id);

                string plainText = ExtractTextFromHtml(source.Data);
                string? BodyText;

                if (messageDetails != null && messageDetails.BodyText != null &&
                    messageDetails.BodyText.ToString() != null)
                {
                    BodyText = messageDetails.BodyText.ToString();
                    if (String.IsNullOrEmpty(BodyText))
                    {
                        BodyText = empty;
                    }
                }
                else
                {
                    BodyText = empty;
                }

                currentVerificationCodes =
                    ExtractVerificationCodes(plainText, 50, message.Subject.ToString() + BodyText);
                verificationCodeCounter = 0;
                string verificationCode = currentVerificationCodes.Length > 0 ? currentVerificationCodes[0] : "";


                string messageToLog = $"{newline} " +
                                      $"From: {message.From.Address}{newline} " +
                                      $"Subject: {message.Subject}{newline} " +
                                      $"Code: {verificationCode}{NewLine(2)} " +
                                      $"Body: {newline} " +
                                      $"---------------------------- {NewLine(2)} " +
                                      $"{GetNewlineCompatible(BodyText)}{newline} " +
                                      $"============================ {newline}" +
                                      $"{rawBody}: {GetNewlineCompatible(plainText)}";

                //LogMessage(messageToLog);
                //LogMessage(message.Subject, message.From.Address, "Me", BodyText, source.ToString()!, verificationCode);
                LogSourceMessage(source.Data);

                await currentClient.MarkMessageAsSeen(message.Id, true);
                await currentClient.DeleteMessage(message.Id);

                if (killAllMSGThreads)
                {
                    Thread.Sleep(4000); //4secs thats more than 3 duh!

                    killAllMSGThreads = false;
                    runningMSGThread = false;
                    running = false;
                }

                if (!runningExitThread)
                {
                    Log($"Exiting CheckThread in {threadAliveTime} Milliseconds");
                    runningExitThread = true;
                    //Disable Account 1.5 minutes after first message or smth i dunno probably changed it idk.
                    Thread timedThread = new Thread(async () =>
                    {
                        Thread.Sleep(generateNewAccountTime);

                        //await GenerateMailClient();  NO Auto regenation after time

                        runningMSGThread = false;

                        Thread.Sleep(threadAliveTime - generateNewAccountTime);

                        running = false;

                        if (deleteAccountWhenAbandoned)
                        {
                            await currentClient.DeleteAccount();
                        }

                        Log("Exited a CheckThread");
                    });
                    timedThread.Start();
                }
            }

            await Task.Delay(3000); //sleep sum time!
        }
    }

    private static ManualResetEventSlim _exitEvent = new ManualResetEventSlim(false);

    [STAThread]
    static async Task Main()
    {
        Log($"Postherr {versionIdentifier} started", true);


        if (!InitializeFacts())
        {
            Thread timedThread = new Thread(async () =>
            {
                Thread.Sleep(retryOnFailTime);
                Log($"Fact file initialization failed, retry successful: {InitializeFacts()}");
            });
            timedThread.Start();
        }

        using (var hotkeyManager = new GlobalHotkeyManager())
        {
            // Store it in your static field for reference elsewhere if needed
            _hotkeyManager = hotkeyManager;

            // Register hotkeys
            RegisterAllHotkeysOnStart();

            // Setup other components
            await SetupTempMailsNShit();

            // Wait for exit signal - this keeps the main thread alive
            _exitEvent.Wait();
        }

        Log("Postherr ended");
    }

    public static void SignalExit()
    {
        OnStop();
        _exitEvent.Set();
    }

    #region Cooldown because my code is as unstable as my mental condition

    private static DateTime lastHotKeyPress = DateTime.MinValue;
    private static readonly object cooldownLock = new object();
    private static readonly TimeSpan cooldown = TimeSpan.FromSeconds(0.5 + (pasteWaitTime / 1000) * 2);

    private static bool CanExecute()
    {
        lock (cooldownLock)
        {
            if (DateTime.Now - lastHotKeyPress < cooldown)
            {
                Log($"Cooldown in effect, try again in: {lastHotKeyPress + cooldown}");
                return false;
            }

            lastHotKeyPress = DateTime.Now;
            return true;
        }
    }

    #endregion

    #region HotKey Functions

    [STAThread]
    private static void OnHotKeyEmailAddrPressed()
    {
        //if (!CanExecute()) return;
        //if (currentClient == null) await GenerateMailClient();

        string emailaddress = formattedEmailAddress; //currentClient.Email;

        OutputText(emailaddress);

        if (runningMSGThread)
        {
            Log("Message Thread already running");
            return;
        }

        runningMSGThread = true;

        Thread messageCheckerThread = new Thread(() => CheckNewMessages(currentClient));
        messageCheckerThread.Start();
    }

    [STAThread]
    private static void OnHotKeyPSWDPressed()
    {
        if (!CanExecute()) return;

        OutputText(currentPassword!);
    }

    [STAThread]
    private static void OnHotKeyUsernamePressed()
    {
        if (!CanExecute()) return;

        OutputText(currentUsername!);
    }

    public static int verificationCodeCounter = 0;

    [STAThread]
    private static void OnHotKeyVerificationCodePressed()
    {
        if (!CanExecute() || currentVerificationCodes == null) return;

        if (!(verificationCodeCounter + 1 <= currentVerificationCodes!.Length))
        {
            verificationCodeCounter = 0;
        }

        OutputText(currentVerificationCodes[verificationCodeCounter]);
        verificationCodeCounter++;
    }

    [STAThread]
    private static async void OnAccountRegenerate()
    {
        if (!CanExecute()) return;
        Log("Hotkey Account Regenerate pressed!");

        await GenerateMailClient();
        runningMSGThread = false;
    }

    [STAThread]
    private static async void OnEmailLoopEliminate()
    {
        if (!CanExecute()) return;
        Log("Hotkey Email Loop big red button or something idk what im doing pls help pressed!");

        runningMSGThread = false;
        killAllMSGThreads = true;
    }

    [STAThread]
    private static async void OnRandomFactPressed()
    {
        if (!CanExecute()) return;

        string fact = await GetRandomContent();
        OutputText(fact);
    }

    #endregion

    static void OnStop()
    {
        Log("Postherr stopped");
    }
}


public class HotkeyConfig
{
    public int ClipboardProcessingDelay { get; set; } = 300;
    public bool IsWayland { get; set; } = true;
    public string EmailShortcut { get; set; } = "Alt+Q";
    public string PasswordShortcut { get; set; } = "Alt+W";
    public string UsernameShortcut { get; set; } = "Alt+E";
    public string VerificationCodeShortcut { get; set; } = "Alt+S";
    public string RegenerateAccountShortcut { get; set; } = "Alt+1";
    public string KillLoopShortcut { get; set; } = "Alt+P";
    public string FactShortcut { get; set; } = "Alt+F";

    public static HotkeyConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var defaultConfig = new HotkeyConfig();
            SaveToFile(defaultConfig, filePath);
            return defaultConfig;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<HotkeyConfig>(json) ?? new HotkeyConfig();
    }

    public static void SaveToFile(HotkeyConfig config, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(filePath, json);
    }
}

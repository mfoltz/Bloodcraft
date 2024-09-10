using System.Text;
using System.Text.RegularExpressions;

namespace Bloodcraft;

internal static class GenerateREADME
{
    // Paths set by the user or build script
    static string CommandsPath { get; set; }
    static string ReadMePath { get; set; }

    // Static regex patterns for parsing commands
    static readonly Regex CommandGroupRegex = new(@"\[CommandGroup\((?:name:\s*""(?<name>[^""]+)""\s*,\s*)?""(?<group>[^""]+)""(?:\s*,\s*""(?<short>[^""]+)"")?\)\]");
    static readonly Regex CommandAttributeRegex = new(@"\[Command\((?:name:\s*""(?<name>[^""]+)"")?(?:,\s*shortHand:\s*""(?<shortHand>[^""]+)"")?(?:,\s*adminOnly:\s*(?<adminOnly>\w+))?(?:,\s*usage:\s*""(?<usage>[^""]+)"")?(?:,\s*description:\s*""(?<description>[^""]+)"")?\)\]");
    static readonly Regex CommandSectionPattern = new(@"^(?!.*using\s+static).*?\b[A-Z][a-zA-Z]*Commands\b");

    // Entry point for post-build invocation
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: GenerateREADME <CommandsPath> <ReadMePath>");
            return;
        }

        // Set the paths from the command-line arguments
        CommandsPath = args[0];
        ReadMePath = args[1];

        Generate();
    }

    // Main method to generate the README
    static void Generate()
    {
        try
        {
            // Call the command generation logic
            GenerateCommandsSection();
            Console.WriteLine("README generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating README: {ex.Message}");
        }
    }

    // Method to generate the commands section of the README
    static void GenerateCommandsSection()
    {
        // Get all C# files from the CommandsPath
        string[] files = Directory.GetFiles(CommandsPath, "*.cs");

        // StringBuilder to construct new Commands section
        StringBuilder commandsSection = new();
        commandsSection.AppendLine("## Commands");

        foreach (string file in files)
        {
            // Load the file content
            string[] fileLines = File.ReadAllLines(file);

            // Extract command section
            string commandSection = Regex.Replace(fileLines.First(line => CommandSectionPattern.IsMatch(line)), "(?<!^)([A-Z])", " $1");
            commandSection = commandSection.Replace("internal static class  ", "");

            // Extract command group full and short
            string commandGroup = "";
            string commandGroupShort = "";

            var commandGroupLine = fileLines.FirstOrDefault(line => CommandGroupRegex.IsMatch(line));
            if (commandGroupLine != null)
            {
                var match = CommandGroupRegex.Match(commandGroupLine);
                commandGroup = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
                commandGroupShort = match.Groups[2].Success ? match.Groups[2].Value : "";
            }

            // Append section title
            commandsSection.AppendLine($"\n### {commandSection}");

            // Find methods with command attribute
            foreach (string line in fileLines)
            {
                var match = CommandAttributeRegex.Match(line);
                if (match.Success)
                {
                    string name = match.Groups["name"].Value;
                    string shortHand = match.Groups["shortHand"].Success ? match.Groups["shortHand"].Value : "";
                    string adminOnly = match.Groups["adminOnly"].Value;
                    string usage = match.Groups["usage"].Value;
                    string description = match.Groups["description"].Value;

                    // Formulate command prefix
                    string commandPrefix = $"- `.{commandGroup} {usage}`";

                    // Append information to the section
                    commandsSection.AppendLine(commandPrefix);
                    commandsSection.AppendLine($"  - {description}");
                    if (bool.Parse(adminOnly))
                    {
                        commandsSection.AppendLine($"  - Admin-only");
                    }
                }
            }
        }

        // Write the new Commands section to the README
        UpdateReadme(commandsSection.ToString());
    }

    // Method to update the README with the new Commands section
    static void UpdateReadme(string commandsSection)
    {
        // Load the existing README file
        string[] readmeLines = File.ReadAllLines(ReadMePath);

        // Find start and end of the Commands section
        int commandsStartIndex = Array.FindIndex(readmeLines, line => line.StartsWith("## Commands"));
        int commandsEndIndex = Array.FindIndex(readmeLines, commandsStartIndex + 1, line => line.StartsWith("## "));

        // Replace the old Commands section with the new one
        StringBuilder updatedReadme = new();
        updatedReadme.Append(string.Join(Environment.NewLine, readmeLines.Take(commandsStartIndex)));
        updatedReadme.AppendLine(commandsSection);
        updatedReadme.Append(string.Join(Environment.NewLine, readmeLines.Skip(commandsEndIndex)));

        // Write the updated content back to the README file
        File.WriteAllText(ReadMePath, updatedReadme.ToString());
    }
}
using Microsoft.Build.Framework;
using System.Text;
using System.Text.RegularExpressions;

namespace Bloodcraft;
public class GenerateREADME : ITask
{
    public IBuildEngine BuildEngine { get; set; }
    public ITaskHost HostObject { get; set; }

    // Required properties for our task
    [Required]
    public string CommandsPath { get; set; }

    [Required]
    public string ReadMePath { get; set; }

    static readonly Regex CommandGroupRegex = new(@"\[CommandGroup\((?:name:\s*""(?<name>[^""]+)""\s*,\s*)?""(?<group>[^""]+)""(?:\s*,\s*""(?<short>[^""]+)"")?\)\]");
    static readonly Regex CommandAttributeRegex = new(@"\[Command\((?:name:\s*""(?<name>[^""]+)"")?(?:,\s*shortHand:\s*""(?<shortHand>[^""]+)"")?(?:,\s*adminOnly:\s*(?<adminOnly>\w+))?(?:,\s*usage:\s*""(?<usage>[^""]+)"")?(?:,\s*description:\s*""(?<description>[^""]+)"")?\)\]");
    static readonly Regex CommandSectionPattern = new(@"^(?!.*using\s+static).*?\b[A-Z][a-zA-Z]*Commands\b");

    public bool Execute()
    {
        try
        {
            // Run the logic to generate the README section
            GenerateCommandsSection();
            return true;
        }
        catch (Exception ex)
        {
            BuildEngine.LogErrorEvent(new BuildErrorEventArgs("Error", "", "GenerateReadmeTask", 0, 0, 0, 0, ex.Message, "", "GenerateReadmeTask"));
            return false;
        }
    }

    private void GenerateCommandsSection()
    {
        // Get all text files in the directory
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

                    string commandPrefix = "";

                    if (!string.IsNullOrEmpty(commandGroupShort))
                    {
                        if (!string.IsNullOrEmpty(shortHand))
                        {
                            commandPrefix = $"- `.{commandGroup} {usage.Replace("." + shortHand, name)}`";
                        }
                        else
                        {
                            commandPrefix = $"- `.{commandGroup} {usage.Replace("." + commandGroupShort + " ", "")}`";
                        }
                    }
                    else if (!string.IsNullOrEmpty(commandGroup))
                    {
                        if (!string.IsNullOrEmpty(shortHand))
                        {
                            commandPrefix = $"- `.{commandGroup} {usage.Replace("." + shortHand, name)}`";
                        }
                        else
                        {
                            commandPrefix = $"- `.{usage}`";
                        }
                    }
                    else
                    {
                        commandPrefix = $"- `.{usage}`";
                    }

                    string commandDescription = $"  - {description}";
                    string commandShortcut = $"  - Shortcut: *{usage}*";

                    if (bool.Parse(adminOnly))
                    {
                        commandPrefix += " 🔒";
                    }

                    // Append the formatted command to the section
                    commandsSection.AppendLine(commandPrefix);
                    commandsSection.AppendLine(commandDescription);
                    commandsSection.AppendLine(commandShortcut);
                }
            }
        }

        // Load the existing README file
        string[] readmeLines = File.ReadAllLines(ReadMePath);

        // Find the start and end of the old Commands section
        int commandsStartIndex = Array.FindIndex(readmeLines, line => line.StartsWith("## Commands"));
        int commandsEndIndex = Array.FindIndex(readmeLines, commandsStartIndex + 1, line => line.StartsWith("## "));

        // Replace the old Commands section with the new one
        StringBuilder updatedReadme = new();
        updatedReadme.Append(string.Join(Environment.NewLine, readmeLines.Take(commandsStartIndex)));
        updatedReadme.AppendLine(commandsSection.ToString());
        updatedReadme.Append(string.Join(Environment.NewLine, readmeLines.Skip(commandsEndIndex)));

        // Write the updated content back to the README file
        File.WriteAllText(ReadMePath, updatedReadme.ToString());
    }
}
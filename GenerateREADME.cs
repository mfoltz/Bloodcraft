using System.Text;
using System.Text.RegularExpressions;
using static Bloodcraft.Services.ConfigService;

namespace Bloodcraft;
internal static class GenerateREADME
{
    // Paths set by the user or build script
    static string CommandsPath { get; set; }
    static string ReadMePath { get; set; }

    // Regex patterns for parsing commands
    // static readonly Regex _commandGroupRegex = new(@"\[CommandGroup\(name:\s*""(?<group>[^""]+)"",\s*""(?<short>[^""]+)""\)\]"); // the first and second one here should really just be one but this works and tired so leaving >_>
    // static readonly Regex _commandGroupAndShortRegex = new(@"\[CommandGroup\(name:\s*""(?<group>[^""]+)""(?:\s*,\s*short:\s*""(?<short>[^""]+)"")?\)\]");
    // static readonly Regex _commandAttributeRegex = new(@"\[Command\(name:\s*""(?<name>[^""]+)""(?:,\s*shortHand:\s*""(?<shortHand>[^""]+)"")?(?:,\s*adminOnly:\s*(?<adminOnly>\w+))?(?:,\s*usage:\s*""(?<usage>[^""]+)"")?(?:,\s*description:\s*""(?<description>[^""]+)"")?\)\]");

    // Order‑agnostic regex patterns for attributes (works across line breaks, any parameter order)
    /*
    static readonly Regex _commandGroupRegex =
        new(@"\[CommandGroup\((?<args>[^\)]*)\)\]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex _commandAttributeRegex =
        new(@"\[Command\((?<args>[^\)]*)\)\]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    */

    static readonly Regex _commandGroupRegex = 
        new("\\[CommandGroup\\((?<args>.*?)\\)\\]",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex _commandAttributeRegex = 
        new("\\[Command\\((?<args>.*?)\\)\\]",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex _argPairRegex = 
        new("\\b(?<key>\\w+)\\s*:\\s*(?<value>\\\"[^\\\"]*\\\"|[^,\\)\\r\\n]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Constants for README sections
    const string COMMANDS_HEADER = "## Commands";
    const string CONFIG_HEADER = "## Configuration";

    // We'll store all commands in this structure before outputting them
    static readonly Dictionary<(string groupName, string groupShort), List<(string name, string shortHand, bool adminOnly, string usage, string description)>> _commandsByGroup
        = [];

    // Entry point for post-build invocation
    public static void Main(string[] args)
    {
        // Check if we're running in a GitHub Actions environment and skip
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            Console.WriteLine("GenerateREADME skipped during GitHub Actions build.");
            return;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: GenerateREADME <CommandsPath> <ReadMePath>");
            return;
        }

        CommandsPath = args[0];
        ReadMePath = args[1];

        try
        {
            Generate();
            Console.WriteLine("README generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating README: {ex.Message}");
        }
    }

    // Main method to generate the README
    static void Generate()
    {
        CollectCommands();
        var commandsSection = BuildCommandsSection();
        var configSection = BuildConfigSection();
        UpdateReadme(commandsSection, configSection);
    }

    // Collect commands from all files into the dictionary
    static void CollectCommands()
    {
        var files = Directory.GetFiles(CommandsPath, "*.cs");
        // .Where(file => !Path.GetFileName(file).Equals("DevCommands.cs", StringComparison.OrdinalIgnoreCase));

        /*
        foreach (var file in files)
        {
            var fileContent = File.ReadAllText(file);
            var commandGroupMatch = _commandGroupRegex.Match(fileContent);

            // if (!commandGroupMatch.Success) commandGroupMatch = _commandGroupAndShortRegex.Match(fileContent);
            if (!commandGroupMatch.Success) commandGroupMatch = _commandGroupRegex.Match(fileContent);

            string groupName, groupShort;

            if (commandGroupMatch.Success)
            {
                // Extract group name and shorthand from regex match
                groupName = commandGroupMatch.Groups["group"].Value;
                groupShort = commandGroupMatch.Groups["short"].Value;
            }
            else
            {
                groupName = "misc";
                groupShort = string.Empty;
            }

            if (!commandsByGroup.TryGetValue((groupName, groupShort), out var cmdList))
            {
                cmdList = [];
                commandsByGroup[(groupName, groupShort)] = cmdList;
            }

            // Find all commands within the group
            foreach (Match commandMatch in _commandAttributeRegex.Matches(fileContent))
            {
                string name = commandMatch.Groups["name"].Value;

                // If optional attributes are missing, they won't have .Success = true
                string shortHand = commandMatch.Groups["shortHand"].Success ? commandMatch.Groups["shortHand"].Value : string.Empty;
                bool adminOnly = false;

                if (commandMatch.Groups["adminOnly"].Success)
                {
                    adminOnly = bool.TryParse(commandMatch.Groups["adminOnly"].Value, out adminOnly) && adminOnly;
                }

                string usage = commandMatch.Groups["usage"].Success ? commandMatch.Groups["usage"].Value : string.Empty;
                string description = commandMatch.Groups["description"].Success ? commandMatch.Groups["description"].Value : string.Empty;

                cmdList.Add((name, shortHand, adminOnly, usage, description));
            }
        }
        */

        foreach (string file in files)
        {
            string content = File.ReadAllText(file);

            // ——— Group ———
            Match grpMatch = _commandGroupRegex.Match(content);
            string groupArgs = grpMatch.Success ? grpMatch.Groups["args"].Value : string.Empty;
            string groupName = GetStringArg(groupArgs, "name");
            string groupShort = GetStringArg(groupArgs, "short");
            if (string.IsNullOrEmpty(groupShort))
                groupShort = GetStringArg(groupArgs, "shortHand");
            if (string.IsNullOrEmpty(groupName)) groupName = "misc";

            if (!_commandsByGroup.TryGetValue((groupName, groupShort), out var list))
            {
                list = [];
                _commandsByGroup[(groupName, groupShort)] = list;
            }

            // ——— Commands ———
            foreach (Match cmdMatch in _commandAttributeRegex.Matches(content))
            {
                string args = cmdMatch.Groups["args"].Value;

                string name = GetStringArg(args, "name");
                string shortHand = GetStringArg(args, "shortHand");
                bool adminOnly = GetBoolArg(args, "adminOnly");
                string usage = GetStringArg(args, "usage");
                string description = GetStringArg(args, "description");

                list.Add((name, shortHand, adminOnly, usage, description));
            }
        }

        // keep each list sorted for deterministic output
        foreach (var key in _commandsByGroup.Keys.ToList())
            _commandsByGroup[key] = [.._commandsByGroup[key].OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase)];
    }
    static string BuildCommandsSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine(COMMANDS_HEADER).AppendLine();

        foreach (var (groupName, groupShort) in _commandsByGroup.Keys.OrderBy(g => g.groupName))
        {
            sb.AppendLine($"### {Capitalize(groupName)} Commands");

            foreach (var (name, shortHand, adminOnly, usage, description) in _commandsByGroup[(groupName, groupShort)])
            {
                bool hasShortHand = !string.IsNullOrEmpty(shortHand);

                // derive a usable shortcut string if the attribute didn't provide one
                string effectiveUsage = string.IsNullOrEmpty(usage)
                    ? $".{(string.IsNullOrEmpty(groupShort) ? groupName : groupShort)} {(hasShortHand ? shortHand : name)}"
                    : usage;

                // strip the prefix (dot + two tokens) to get parameters only
                string parameters = string.Empty;
                var parts = effectiveUsage.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3) parameters = parts[2];
                else if (parts.Length == 2 && parts[1].StartsWith("[")) parameters = parts[1];

                string adminLock = adminOnly ? " 🔒" : string.Empty;
                string cmdLine = parameters.Length > 0
                    ? $"- `.{groupName} {name} {parameters}`{adminLock}"
                    : $"- `.{groupName} {name}`{adminLock}";

                sb.AppendLine(cmdLine);
                if (!string.IsNullOrEmpty(description)) sb.AppendLine($"  - {description}");
                sb.AppendLine($"  - Shortcut: *{effectiveUsage}*");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /*
    static string BuildCommandsSection()
    {
        StringBuilder sb = new();
        sb.AppendLine("## Commands");
        sb.AppendLine();

        var orderedGroups = _commandsByGroup.Keys.OrderBy(g => g.groupName).ToList();

        foreach (var group in orderedGroups)
        {
            var (groupName, groupShort) = group;
            sb.AppendLine($"### {Capitalize(groupName)} Commands");

            var cmdList = _commandsByGroup[group];
            foreach (var (name, shortHand, adminOnly, usage, description) in cmdList)
            {
                bool hasShorthand = !string.IsNullOrEmpty(shortHand);
                bool hasGroupShort = !string.IsNullOrEmpty(groupShort);

                // If has parameters and no shorthand replace 
                string commandUsage = string.IsNullOrEmpty(usage) ? name : usage;
                string nameReplacement = commandUsage.EndsWith(name) || !hasShorthand ? name : string.Empty;

                // Prebuild command line strings
                string adminLock = adminOnly ? " 🔒" : string.Empty;
                string commandParameters = string.Empty;

                if (hasGroupShort)
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupShort} {shortHand}", "") : commandUsage.Replace($".{groupShort} {nameReplacement}", "");
                }
                else
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupName} {shortHand}", "") : commandUsage.Replace($".{groupName} {nameReplacement}", "");
                }

                // Build main command line string
                var commandLine = $"- `.{groupName} {name}{commandParameters}`{adminLock}";
                sb.AppendLine(commandLine);

                // Description line if available
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"  - {description}");
                }

                sb.AppendLine($"  - Shortcut: *{commandUsage}*");
            }

            // Add spacing after each group, except the last one
            if (orderedGroups.IndexOf(group) < orderedGroups.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
    */
    static string BuildConfigSection()
    {
        StringBuilder sb = new();
        sb.AppendLine("## Configuration");
        sb.AppendLine();

        // Group config entries by their section
        var groupedConfigEntries = ConfigInitialization.ConfigEntries
            .GroupBy(entry => entry.Section)
            .OrderBy(group => ConfigInitialization.SectionOrder.IndexOf(group.Key)).ToList();

        foreach (var group in groupedConfigEntries)
        {
            sb.AppendLine($"### {group.Key}");

            foreach (var entry in group)
            {
                string defaultValue = entry.DefaultValue is string strValue ? $"\"{strValue}\"" : entry.DefaultValue.ToString();
                string typeName = entry.DefaultValue.GetType().Name.ToLower();

                // Adjust type naming for readability
                if (typeName == "boolean") typeName = "bool";
                else if (typeName == "single") typeName = "float";
                else if (typeName == "int32") typeName = "int";

                sb.AppendLine($"- **{AddSpacesToCamelCase(entry.Key)}**: `{entry.Key}` ({typeName}, default: {defaultValue})");
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    sb.AppendLine($"  {entry.Description}");
                }
            }

            // Add spacing after each group, except the last one
            if (groupedConfigEntries.IndexOf(group) < groupedConfigEntries.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
    static string AddSpacesToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder sb = new();
        for (int i = 0; i < input.Length; i++)
        {
            char current = input[i];

            // Check for capital letters but ignore consecutive ones (e.g., XP)
            bool isUpperCase = char.IsUpper(current);
            bool isNotFirstChar = i > 0;
            bool isPreviousCharLowerCase = isNotFirstChar && char.IsLower(input[i - 1]);
            bool isNextCharLowerCase = (i < input.Length - 1) && char.IsLower(input[i + 1]);

            if (isNotFirstChar && isUpperCase && (isPreviousCharLowerCase || isNextCharLowerCase))
            {
                sb.Append(' ');
            }

            sb.Append(current);
        }

        return sb.ToString();
    }
    static void UpdateReadme(string commandsSection, string configSection)
    {
        bool inCommandsSection = false;
        bool inConfigSection = false;
        bool commandsReplaced = false;
        bool configReplaced = false;

        List<string> newContent = [];

        try
        {
            foreach (string line in File.ReadLines(ReadMePath))
            {
                if (line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Start of "## Commands"
                    inCommandsSection = true;
                    commandsReplaced = true;

                    newContent.Add(commandsSection); // Add new commands

                    continue;
                }

                if (line.Trim().Equals(CONFIG_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Start of "## Configuration"
                    inConfigSection = true;
                    configReplaced = true;

                    newContent.Add(configSection); // Add new configuration

                    continue;
                }

                if (inCommandsSection && line.Trim().StartsWith("## ", StringComparison.OrdinalIgnoreCase) &&
                    !line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Reached the next section or a new header
                    inCommandsSection = false;
                }

                if (inConfigSection && line.Trim().StartsWith("## ", StringComparison.OrdinalIgnoreCase) &&
                    !line.Trim().Equals(CONFIG_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Reached the next section or a new header
                    inConfigSection = false;
                }

                if (!inCommandsSection && !inConfigSection)
                {
                    newContent.Add(line);
                }
            }

            if (inConfigSection)
            {
                newContent.Add(configSection);
                inConfigSection = false;
            }

            if (!commandsReplaced)
            {
                // Append new section if "## Commands" not found
                newContent.Add(COMMANDS_HEADER);
                newContent.Add(commandsSection);
            }

            if (!configReplaced)
            {
                // Append new config section if "## Configuration" not found
                newContent.Add(CONFIG_HEADER);
                newContent.Add(configSection);
            }

            File.WriteAllLines(ReadMePath, newContent);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error updating the readme: {ex.Message}");
            throw;
        }
    }
    static string GetStringArg(string args, string key)
    {
        foreach (Match m in _argPairRegex.Matches(args))
        {
            if (m.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                string raw = m.Groups["value"].Value.Trim();
                return raw.Length > 1 && raw[0] == '\"' && raw[^1] == '\"'
                       ? raw[1..^1]   // strip quotes
                       : raw;
            }
        }

        return string.Empty;
    }

    /*
    static string GetStringArg(string args, string key)
    {
        foreach (Match m in _argPairRegex.Matches(args))
            if (string.Equals(m.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return m.Groups["value"].Value;
        return string.Empty;
    }
    */
    static bool GetBoolArg(string args, string key)
    {
        foreach (Match m in _argPairRegex.Matches(args))
            if (string.Equals(m.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return bool.TryParse(m.Groups["value"].Value, out bool b) && b;

        return false;
    }
    static string Capitalize(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input[1..];
}

/*
internal static class GenerateREADME
{
    // Paths set by the user or build script
    static string CommandsPath { get; set; }
    static string ReadMePath { get; set; }

    // Order‑agnostic regex patterns for attributes (works across line breaks, any parameter order)
    static readonly Regex _commandGroupRegex =
        new(@"\[CommandGroup\((?<args>[^\)]*)\)\]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex _commandAttributeRegex =
        new(@"\[Command\((?<args>[^\)]*)\)\]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // generic  key : "value"  pair matcher used by the helpers below
    static readonly Regex _argPairRegex =
        new(@"\b(?<key>\w+)\s*:\s*""(?<value>[^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Constants for README sections
    const string COMMANDS_HEADER = "## Commands";
    const string CONFIG_HEADER = "## Configuration";

    // We'll store all commands in this structure before outputting them
    static readonly Dictionary<(string groupName, string groupShort), List<(string name, string shortHand, bool adminOnly, string usage, string description)>> commandsByGroup
        = [];

    // Entry point for post-build invocation
    public static void Main(string[] args)
    {
        // Skip on GitHub runners (handled by separate action)
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            Console.WriteLine("GenerateREADME skipped during GitHub Actions build.");
            return;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: GenerateREADME <CommandsPath> <ReadMePath>");
            return;
        }

        CommandsPath = args[0];
        ReadMePath = args[1];

        try
        {
            Generate();
            Console.WriteLine("README generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating README: {ex.Message}");
        }
    }
    #region Regex‑helpers
    static string GetStringArg(string args, string key)
    {
        foreach (Match m in _argPairRegex.Matches(args))
            if (string.Equals(m.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return m.Groups["value"].Value;
        return string.Empty;
    }
    static bool GetBoolArg(string args, string key)
    {
        foreach (Match m in _argPairRegex.Matches(args))
            if (string.Equals(m.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return bool.TryParse(m.Groups["value"].Value, out bool b) && b;
        return false;
    }

    #endregion
    // Main method to generate the README
    static void Generate()
    {
        CollectCommands();
        var commandsSection = BuildCommandsSection();
        var configSection = BuildConfigSection();
        UpdateReadme(commandsSection, configSection);
    }

    // Collect commands from all files into the dictionary
    static void CollectCommands()
    {
        var files = Directory.GetFiles(CommandsPath, "*.cs", SearchOption.AllDirectories)
                             .Where(file => !Path.GetFileName(file).Equals("DevCommands.cs", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            string fileContent = File.ReadAllText(file);

            // ---------- Parse group ----------
            string groupName = "misc";
            string groupShort = string.Empty;

            var groupMatch = _commandGroupRegex.Match(fileContent);
            if (groupMatch.Success)
            {
                string groupArgs = groupMatch.Groups["args"].Value;
                string nameArg = GetStringArg(groupArgs, "name");
                if (!string.IsNullOrEmpty(nameArg)) groupName = nameArg;

                string shortArg = GetStringArg(groupArgs, "short");
                if (!string.IsNullOrEmpty(shortArg)) groupShort = shortArg;
            }

            if (!commandsByGroup.TryGetValue((groupName, groupShort), out var cmdList))
            {
                cmdList = [];
                commandsByGroup[(groupName, groupShort)] = cmdList;
            }

            // ---------- Parse commands ----------
            foreach (Match commandMatch in _commandAttributeRegex.Matches(fileContent))
            {
                string args = commandMatch.Groups["args"].Value;

                string name = GetStringArg(args, "name");
                if (string.IsNullOrWhiteSpace(name)) continue; // malformed attribute

                string shortHand = GetStringArg(args, "shortHand");
                bool adminOnly = GetBoolArg(args, "adminOnly");
                string usage = GetStringArg(args, "usage");
                string description = GetStringArg(args, "description");

                cmdList.Add((name, shortHand, adminOnly, usage, description));
            }

            // Keep deterministic ordering inside each group for stable diffs
            cmdList.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ---------- README BUILDERS (unchanged below this point) ----------
    static string BuildCommandsSection()
    {
        StringBuilder sb = new();
        sb.AppendLine("## Commands");
        sb.AppendLine();

        var orderedGroups = commandsByGroup.Keys.OrderBy(g => g.groupName).ToList();

        foreach (var group in orderedGroups)
        {
            var (groupName, groupShort) = group;
            sb.AppendLine($"### {Capitalize(groupName)} Commands");

            var cmdList = commandsByGroup[group];
            foreach (var (name, shortHand, adminOnly, usage, description) in cmdList)
            {
                bool hasShorthand = !string.IsNullOrEmpty(shortHand);
                bool hasGroupShort = !string.IsNullOrEmpty(groupShort);

                string commandUsage = string.IsNullOrEmpty(usage) ? name : usage;
                string nameReplacement = commandUsage.EndsWith(name) || !hasShorthand ? name : string.Empty;

                string adminLock = adminOnly ? " 🔒" : string.Empty;
                string commandParameters;

                if (hasGroupShort)
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupShort} {shortHand}", "")
                                                      : commandUsage.Replace($".{groupShort} {nameReplacement}", "");
                }
                else
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupName} {shortHand}", "")
                                                      : commandUsage.Replace($".{groupName} {nameReplacement}", "");
                }

                var commandLine = $"- `.{groupName} {name}{commandParameters}`{adminLock}";
                sb.AppendLine(commandLine);

                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"  - {description}");
                }

                sb.AppendLine($"  - Shortcut: *{commandUsage}*");
            }

            if (orderedGroups.IndexOf(group) < orderedGroups.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
    static string BuildConfigSection()
    {
        StringBuilder sb = new();
        sb.AppendLine("## Configuration");
        sb.AppendLine();

        var groupedConfigEntries = ConfigInitialization.ConfigEntries
            .GroupBy(entry => entry.Section)
            .OrderBy(group => ConfigInitialization.SectionOrder.IndexOf(group.Key)).ToList();

        foreach (var group in groupedConfigEntries)
        {
            sb.AppendLine($"### {group.Key}");

            foreach (var entry in group)
            {
                string defaultValue = entry.DefaultValue is string strValue ? $"\"{strValue}\"" : entry.DefaultValue.ToString();
                string typeName = entry.DefaultValue.GetType().Name.ToLower();

                if (typeName == "boolean") typeName = "bool";
                else if (typeName == "single") typeName = "float";
                else if (typeName == "int32") typeName = "int";

                sb.AppendLine($"- **{AddSpacesToCamelCase(entry.Key)}**: `{entry.Key}` ({typeName}, default: {defaultValue})");
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    sb.AppendLine($"  {entry.Description}");
                }
            }

            if (groupedConfigEntries.IndexOf(group) < groupedConfigEntries.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
    static string AddSpacesToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        StringBuilder sb = new();
        for (int i = 0; i < input.Length; i++)
        {
            char current = input[i];

            bool isUpperCase = char.IsUpper(current);
            bool isNotFirstChar = i > 0;
            bool isPreviousCharLowerCase = isNotFirstChar && char.IsLower(input[i - 1]);
            bool isNextCharLowerCase = (i < input.Length - 1) && char.IsLower(input[i + 1]);

            if (isNotFirstChar && isUpperCase && (isPreviousCharLowerCase || isNextCharLowerCase))
            {
                sb.Append(' ');
            }

            sb.Append(current);
        }

        return sb.ToString();
    }
    static void UpdateReadme(string commandsSection, string configSection)
    {
        bool inCommandsSection = false;
        bool inConfigSection = false;
        bool commandsReplaced = false;
        bool configReplaced = false;

        List<string> newContent = [];

        try
        {
            foreach (string line in File.ReadLines(ReadMePath))
            {
                if (line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    inCommandsSection = true;
                    commandsReplaced = true;
                    newContent.Add(commandsSection);
                    continue;
                }

                if (line.Trim().Equals(CONFIG_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    inConfigSection = true;
                    configReplaced = true;
                    newContent.Add(configSection);
                    continue;
                }

                if (inCommandsSection && line.Trim().StartsWith("## ", StringComparison.OrdinalIgnoreCase) &&
                    !line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    inCommandsSection = false;
                }

                if (inConfigSection && line.Trim().StartsWith("## ", StringComparison.OrdinalIgnoreCase) &&
                    !line.Trim().Equals(CONFIG_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    inConfigSection = false;
                }

                if (!inCommandsSection && !inConfigSection)
                {
                    newContent.Add(line);
                }
            }

            if (inConfigSection) newContent.Add(configSection);

            if (!commandsReplaced)
            {
                newContent.Add(COMMANDS_HEADER);
                newContent.Add(commandsSection);
            }

            if (!configReplaced)
            {
                newContent.Add(CONFIG_HEADER);
                newContent.Add(configSection);
            }

            File.WriteAllLines(ReadMePath, newContent);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error updating the readme: {ex.Message}");
            throw;
        }
    }

    // Helper method to capitalize strings
    static string Capitalize(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input[1..];
}
*/
using BepInEx;
using BepInEx.Logging;

namespace Bloodcraft;
internal static class ChromaEcho
{
    static ManualLogSource Log => Plugin.LogInstance;
    static void Write(string prefix, string message, ConsoleColor color, bool echo = false)
    {
        if (echo)
        {
            Log.LogInfo($"{prefix} {message}");
        }

        ConsoleManager.ConsoleStream?.Write(Environment.NewLine);
        ConsoleManager.SetConsoleColor(color);
        ConsoleManager.ConsoleStream?.Write($"{prefix} ");
        ConsoleManager.SetConsoleColor(ConsoleColor.Gray);
        ConsoleManager.ConsoleStream?.Write(message);
    }
    public static void LogSuccess(string message)
    {
        Write("[SUCCESS]", message, ConsoleColor.Green);
    }
    public static void LogNotice(string message)
    {
        Write("[NOTICE]", message, ConsoleColor.Magenta);
    }
    public static void LogHighlight(string message)
    {
        Write("[HIGHLIGHT]", message, ConsoleColor.Cyan);
    }
    public static void LogSoftWarning(string message)
    {
        Write("[⚠️ Hint]", message, ConsoleColor.Yellow, true);
    }
    public static void LogCritical(string message)
    {
        Write("[CRITICAL]", message, ConsoleColor.Red);
    }
    public static void Info(string message)
    {
        Write("ℹ", message, ConsoleColor.Gray);  // Info symbol (U+2139)
    }
    public static void Success(string message)
    {
        Write("✔", message, ConsoleColor.Green);  // Checkmark (U+2714)
    }
    public static void Notice(string message)
    {
        Write("※", message, ConsoleColor.Cyan);  // Reference mark (U+203B)
    }
    public static void Warn(string message)
    {
        Write("‼", message, ConsoleColor.Yellow); // Double exclamation (U+203C)
    }
    public static void Critical(string message)
    {
        Write("✖", message, ConsoleColor.Red);   // Cross mark (U+2716)
    }
    public static class Unicode
    {
        // ✅ STATUS & OUTCOME
        public const string SUCCESS = "✔";       // U+2714
        public const string PASSED = "✓";        // U+2713
        public const string INFO = "ℹ";          // U+2139
        public const string WARNING = "‼";       // U+203C
        public const string CRITICAL = "⛔";      // U+26D4
        public const string ERROR = "✖";         // U+2716
        public const string NOTICE = "※";        // U+203B
        public const string FLAG = "⚑";          // U+2691

        // 🧪 SCIENCE & TECHNOLOGY
        public const string ATOM = "⚛";          // U+269B
        public const string BIOHAZARD = "☣";     // U+2623
        public const string RADIATION = "☢";     // U+2622
        public const string GEAR = "⚙";          // U+2699
        public const string CIRCUIT = "⌁";       // U+2301
        public const string OSCILLATION = "∿";   // U+223F
        public const string INFINITY = "∞";      // U+221E
        public const string LOGIC_AND = "⋀";     // U+22C0
        public const string LOGIC_OR = "⋁";      // U+22C1
        public const string THERMO = "♨";        // U+2668
        public const string CLOCK = "⏱";         // U+23F1
        public const string SATELLITE = "🛰";     // U+1F6F0 (may be risky!)
        public const string SCOPE = "🎯";         // U+1F3AF (may be risky!)

        // 🔁 FLOW & ACTION
        public const string EXECUTE = "▶";       // U+25B6
        public const string FORWARD = "→";       // U+2192
        public const string LOOP = "↻";          // U+21BB
        public const string RETRY = "⟳";         // U+27F3
        public const string FORK = "⤴";          // U+2934
        public const string SYNC = "☲";          // U+2632
        public const string DISPATCH = "⇨";      // U+21E8

        // ✨ VISUAL HIGHLIGHTS
        public const string SPARKLE = "✦";       // U+2726
        public const string SHINE = "✧";         // U+2727
        public const string DIAMOND = "❖";       // U+2756
        public const string PULSE = "◉";         // U+25C9
        public const string GLINT = "✴";         // U+2734
        public const string HOLLOW_STAR = "☆";   // U+2606
        public const string SOLID_STAR = "★";    // U+2605
        public const string LIGHT_BURST = "☀";   // U+2600

        // 🧱 BLOCKS & STRUCTURE
        public const string BULLET = "▪";        // U+25AA
        public const string HOLLOW_BULLET = "▫"; // U+25AB
        public const string BOX = "▣";           // U+25A3
        public const string LAYER = "☰";         // U+2630
        public const string CIRCUIT_NODE = "⊛";  // U+229B

        // 🧠 LOGIC / MATH / CLARITY
        public const string THEREFORE = "∴";     // U+2234
        public const string BECAUSE = "∵";       // U+2235
        public const string NOT_EQUAL = "≠";     // U+2260
        public const string EQUAL = "≡";         // U+2261
        public const string APPROX = "≈";        // U+2248
        public const string GREATER = "›";       // U+203A
        public const string LESS = "‹";          // U+2039
        public const string BRANCH = "☍";        // U+260D
    }
}

namespace VCreate.Core.Toolbox
{
    internal class FontColors
    {
        public static string Color(string hexColor, string text)
        {
            return $"<color={hexColor}>{text}</color>";
        }

        public static string Red(string text)
        {
            return Color("#E90000", text);
        }

        public static string Cyan(string text)
        {
            return Color("#00FFFF", text);
        }

        public static string Blue(string text)
        {
            return Color("#0000ff", text);
        }

        public static string Green(string text)
        {
            return Color("#7FE030", text);
        }

        public static string Yellow(string text)
        {
            return Color("#FBC01E", text);
        }

        public static string Orange(string text)
        {
            return Color("#FFA500", text);
        }

        public static string Purple(string text)
        {
            return Color("#800080", text);
        }

        public static string Pink(string text)
        {
            return Color("#FFC0CB", text);
        }

        public static string Brown(string text)
        {
            return Color("#A52A2A", text);
        }

        public static string White(string text)
        {
            return Color("#FFFFFF", text);
        }

        public static string Black(string text)
        {
            return Color("#000000", text);
        }

        public static string Gray(string text)
        {
            return Color("#808080", text);
        }

        public static string Grey(string text)
        {
            return Color("#808080", text);
        }
    }
}
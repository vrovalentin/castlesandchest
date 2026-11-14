using System.Collections.Generic;
using System.Text;

namespace Game.Core
{
    /// <summary>
    /// The game's texts in the player's language. The Russian text is its own key: Loc.T("Склад") gives "Storage" in
    /// English from the dictionary (Resources/Lang/en.txt: "russian TAB english" per line, \n for a line break).
    /// A text missing from the dictionary stays Russian, so nothing is ever blank.
    /// </summary>
    public static class Loc
    {
        /// <summary>"ru" or "en".</summary>
        public static string Lang { get; private set; } = "ru";
        public static bool Russian => Lang == "ru";

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>();

        /// <summary>Russian for players whose language is Russian or close to it (as Yandex does), English for the rest.</summary>
        public static void SetLanguage(string code)
        {
            code = (code ?? "ru").ToLowerInvariant();
            Lang = code.StartsWith("ru") || code.StartsWith("be") || code.StartsWith("uk") || code.StartsWith("kk") || code.StartsWith("uz")
                ? "ru" : "en";
        }

        public static void Load(string dictionary)
        {
            En.Clear();
            if (string.IsNullOrEmpty(dictionary)) return;
            foreach (var line in dictionary.Split('\n'))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0 || line.StartsWith("#")) continue;
                En[Unescape(line.Substring(0, tab))] = Unescape(line.Substring(tab + 1).TrimEnd('\r'));
            }
        }

        public static string T(string ru)
        {
            if (Russian || ru == null) return ru;
            return En.TryGetValue(ru, out var en) && en.Length > 0 ? en : ru;
        }

        public static string F(string ru, params object[] args) => string.Format(T(ru), args);

        /// <summary>The word for n: Russian one/few/many; English singular/plural from the entry "one|few|many".</summary>
        public static string Plural(int n, string one, string few, string many)
        {
            if (!Russian && En.TryGetValue(one + "|" + few + "|" + many, out var en))
            {
                int bar = en.IndexOf('|');
                return bar < 0 ? en : n == 1 ? en.Substring(0, bar) : en.Substring(bar + 1);
            }
            int m10 = n % 10, m100 = n % 100;
            if (m10 == 1 && m100 != 11) return one;
            if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return few;
            return many;
        }

        private static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char c = s[++i];
                    b.Append(c == 'n' ? '\n' : c == 't' ? '\t' : c);
                }
                else b.Append(s[i]);
            }
            return b.ToString();
        }
    }
}

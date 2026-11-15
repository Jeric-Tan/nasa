using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OutpostGale.UI
{
    // Colour-codes the important words in what BOLT and the crew say (on dark speech boxes):
    // meters in their bar colours, dangers in red, supplies, places you can go, and keys to press.
    // Text already inside a <color> tag (like tap-to-explain words) is left alone.
    public static class Keywords
    {
        const string Place = "#D7B8FF";
        const string Danger = "#FF8A80";
        const string Key = "#6FE7FF";

        static readonly Dictionary<string, string> Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // meters (same colours as the bars)
            { "power", "#FFC23D" },
            { "life support", "#6CC4FF" },
            { "crew health", "#7CE29A" },
            { "health", "#7CE29A" },
            // dangers
            { "solar storms", Danger }, { "solar storm", Danger }, { "storms", Danger }, { "storm", Danger },
            { "radiation", Danger }, { "dose", Danger },
            // supplies
            { "regolith", "#F2A26B" }, { "parts", "#C9D2E6" },
            { "rock samples", "#FFD27A" }, { "rock sample", "#FFD27A" }, { "samples", "#FFD27A" },
            // places you can walk to
            { "status board", Place }, { "directory", Place }, { "nasa news", Place }, { "radio", Place },
            { "storm shelter", Place }, { "shelter", Place }, { "airlock", Place }, { "command deck", Place },
            { "map view", Place }, { "greenhouse", Place }, { "hab", Place },
        };

        // One pass, longest phrases first, whole words only ("ice" never matches inside "price").
        static readonly Regex Words = new Regex(@"\b(" + string.Join("|", Colors.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape)) + @")\b",
                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Keys: "press E", "tap I", "A and D", "(2)".
        static readonly Regex PressKey = new Regex(@"\b(press|tap|key)\s+([A-Z1-9])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex KeyPair = new Regex(@"\b([A-Z]) and ([A-Z])\b", RegexOptions.Compiled);
        static readonly Regex KeyParen = new Regex(@"\(([1-9B])\)", RegexOptions.Compiled);
        static readonly Regex Tag = new Regex(@"(<[^>]+>)", RegexOptions.Compiled);

        static string K(string key) => "<b><color=" + Key + ">" + key + "</color></b>";

        public static string Highlight(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = OutsideColor(text, s =>
            {
                s = PressKey.Replace(s, m => m.Groups[1].Value + " " + K(m.Groups[2].Value));
                s = KeyPair.Replace(s, m => K(m.Groups[1].Value) + " and " + K(m.Groups[2].Value));
                return KeyParen.Replace(s, m => "(" + K(m.Groups[1].Value) + ")");
            });
            return OutsideColor(text, s => Words.Replace(s, m => "<b><color=" + Colors[m.Value] + ">" + m.Value + "</color></b>"));
        }

        // Apply f only to plain text that isn't already inside a <color> span.
        static string OutsideColor(string text, Func<string, string> f)
        {
            var sb = new StringBuilder();
            int depth = 0;
            foreach (var part in Tag.Split(text))
            {
                if (part.Length == 0) continue;
                if (part[0] == '<' && part[part.Length - 1] == '>')
                {
                    if (part.StartsWith("<color", StringComparison.OrdinalIgnoreCase)) depth++;
                    else if (part.StartsWith("</color", StringComparison.OrdinalIgnoreCase)) depth = Math.Max(0, depth - 1);
                    sb.Append(part);
                }
                else sb.Append(depth == 0 ? f(part) : part);
            }
            return sb.ToString();
        }
    }
}

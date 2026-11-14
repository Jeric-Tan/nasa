using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    [Serializable]
    public class StoryLine
    {
        public string speaker;   // bolt | mira | tomas | earth | you
        public string text;      // {name} = commander name; [[term]] = tap-to-explain word
    }

    [Serializable]
    public class StoryBeat
    {
        public string id;
        public string trigger;   // act:2, sol:3, first:storm, build:greenhouse, ending:thriving …
        public string mode;      // "" = both, or cadet / commander
        public StoryLine[] lines;
    }

    [Serializable]
    public class Term
    {
        public string id;
        public string word;
        public string explain;
    }

    [Serializable]
    public class Ending
    {
        public string id;
        public string title;
        public string message;
        public string tip;
        public string parallel;  // a real NASA parallel
    }

    [Serializable]
    public class StoryScript
    {
        public StoryBeat[] beats;
        public Term[] terms;
        public Ending[] endings;
        public string[] speakers; // id=Display Name pairs, e.g. "mira=Mira"

        public Ending EndingFor(string id)
        {
            foreach (var e in endings) if (e.id == id) return e;
            return null;
        }

        public Term TermFor(string id)
        {
            foreach (var t in terms) if (t.id == id) return t;
            return null;
        }

        public string SpeakerName(string id)
        {
            if (speakers != null)
                foreach (var s in speakers)
                {
                    int i = s.IndexOf('=');
                    if (i > 0 && s.Substring(0, i) == id) return s.Substring(i + 1);
                }
            return id;
        }
    }

    // Picks which story beats play for a set of triggers. Each beat plays once per mission.
    public static class StoryDirector
    {
        public static List<StoryBeat> BeatsFor(StoryScript script, MissionState s, IEnumerable<string> triggers)
        {
            var outList = new List<StoryBeat>();
            if (script?.beats == null) return outList;
            foreach (var t in triggers)
                foreach (var beat in script.beats)
                {
                    if (beat.trigger != t) continue;
                    if (!string.IsNullOrEmpty(beat.mode) && beat.mode != s.mode) continue;
                    if (s.seenBeats.Contains(beat.id)) continue;
                    s.seenBeats.Add(beat.id);
                    outList.Add(beat);
                }
            return outList;
        }

        // Fill in the commander's name. Returns the text and the term ids it mentions.
        public static string Format(string text, MissionState s, List<string> termsFound)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string name = string.IsNullOrWhiteSpace(s.commanderName) ? "Commander" : s.commanderName.Trim();
            text = name == "Commander" ? text.Replace("Commander {name}", "Commander").Replace("{name}", "Commander") : text.Replace("{name}", name);
            int start;
            while ((start = text.IndexOf("[[", StringComparison.Ordinal)) >= 0)
            {
                int end = text.IndexOf("]]", start, StringComparison.Ordinal);
                if (end < 0) break;
                string inner = text.Substring(start + 2, end - start - 2);
                string id = inner, shown = inner;
                int bar = inner.IndexOf('|');
                if (bar >= 0) { id = inner.Substring(0, bar); shown = inner.Substring(bar + 1); }
                termsFound?.Add(id);
                text = text.Substring(0, start) + shown + text.Substring(end + 2);
            }
            return text;
        }

        // Terms not explained yet this mission, marked as seen.
        public static List<Term> NewTerms(StoryScript script, MissionState s, List<string> termIds)
        {
            var list = new List<Term>();
            foreach (var id in termIds)
            {
                if (s.seenTerms.Contains(id)) continue;
                var t = script.TermFor(id);
                if (t == null) continue;
                s.seenTerms.Add(id);
                list.Add(t);
            }
            return list;
        }
    }
}

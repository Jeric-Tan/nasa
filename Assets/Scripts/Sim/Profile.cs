using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // What carries over between missions (saved in the browser).
    [Serializable]
    public class Profile
    {
        public List<string> blueprints = new List<string>();   // module ids unlocked once
        public List<string> logbook = new List<string>();      // fact and storm card ids found
        public List<string> badges = new List<string>();
        public List<string> autoDo = new List<string>();       // tasks played once, so Auto-do is offered
        public int missionsFinished;
        public bool jezeroUnlocked;
        public string lastEnding;
        public bool reducedMotion;
        public bool muted;

        public bool Add(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id)) return false;
            list.Add(id);
            return true;
        }
    }

    [Serializable]
    public class MissionSetup
    {
        public string mode = "cadet";
        public string site = "gale";
        public string commanderName = "Commander";
        public int suitColor;
        public string classroomCode;
    }

    [Serializable]
    public class BadgeDef
    {
        public string id;
        public string name;
        public string text;
    }

    public static class Badges
    {
        public static readonly BadgeDef[] All =
        {
            new BadgeDef { id = "stormy_season", name = "Storm Chaser", text = "Finished a mission in a real stormy solar season" },
            new BadgeDef { id = "thriving_commander", name = "Top Commander", text = "Thriving Outpost on Commander mode" },
            new BadgeDef { id = "thriving", name = "Home Builder", text = "Reached the Thriving Outpost ending" },
            new BadgeDef { id = "inside_5", name = "Homebody", text = "No one went outside for 5 sols in a row" },
            new BadgeDef { id = "all_sealed", name = "Safe Sleeper", text = "Sealed the shelter before every storm" },
            new BadgeDef { id = "scientist", name = "Rock Hound", text = "Collected 5 rock samples in one mission" },
            new BadgeDef { id = "x_class", name = "X-Class Survivor", text = "Survived a season with an X-class flare" },
        };

        public static BadgeDef Get(string id)
        {
            foreach (var b in All) if (b.id == id) return b;
            return null;
        }
    }
}

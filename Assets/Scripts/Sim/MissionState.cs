using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // Every number of a running mission. Plain fields so it saves as JSON.
    [Serializable]
    public class MissionState
    {
        public int version = 1;
        public int seed;
        public string classroomCode;
        public string mode;            // cadet | commander
        public string site;            // gale | jezero
        public string commanderName;
        public int suitColor;

        public int sol = 1;
        public int lastSol;
        public int quartersLeft;       // daylight left, in quarter hours
        public int quartersWorked;     // non-rest work today
        public int busyDaysInRow;

        public int power, life, health;
        public int healthCap = 100;    // radiation dose adds up: rest can't heal above this
        public int regolith, ice, parts, samples;

        public bool radioUsed;
        public int forecastTonight;    // 0 unknown, 1 calm, 2 storm
        public int forecastTomorrow;   // with the comms dish
        public bool sealedTonight;
        public bool exercisedToday;
        public bool systemsDownLastNight;

        public List<PlacedModule> modules = new List<PlacedModule>();
        public List<Cell> corridors = new List<Cell>();
        public int nextUid = 1;

        public List<ActiveEvent> events = new List<ActiveEvent>();
        public List<PlannedStorm> storms = new List<PlannedStorm>();
        public string seasonLabel;
        public int seasonYear;
        public bool seasonReal;
        public int dustSeasonStart, dustSeasonEnd;

        public List<string> unlocked = new List<string>();   // module ids the build menu shows
        public List<string> traits = new List<string>();     // crew traits this run
        public List<Decision> decisions = new List<Decision>();
        public List<string> seenBeats = new List<string>();
        public List<string> seenTerms = new List<string>();
        public List<string> factsFound = new List<string>();
        public List<string> triggers = new List<string>();   // story triggers waiting for the UI
        public List<string> log = new List<string>();

        public int stormsHit, stormsSealed, stormsUnsealed;
        public int tripsOutside, solsInsideStreak, bestInsideStreak;
        public int totalDose;

        public Trip trip;              // current expedition, or null
        public NightReport lastReport;

        public bool over;
        public string ending;          // thriving | ready | holding | early
        public string endingReason;

        public MissionRng rng = new MissionRng();

        public int Meter(string m) => m == "power" ? power : m == "life" ? life : health;

        public float HoursLeft => quartersLeft / 4f;
    }

    [Serializable]
    public struct Cell
    {
        public int x, y;
        public Cell(int x, int y) { this.x = x; this.y = y; }
        public override string ToString() => $"({x},{y})";
    }

    [Serializable]
    public class PlacedModule
    {
        public int uid;
        public string id;
        public int x, y, w, h;
        public bool built;
        public int progressQ;          // construction progress, quarter hours
        public int builtSol;           // sol it was finished; active from the next sol
        public bool far;               // open-ground module placed far from the base
        public int reservedRegolith, reservedIce, reservedParts;

        public bool Contains(int cx, int cy) => cx >= x && cx < x + w && cy >= y && cy < y + h;
    }

    [Serializable]
    public class ActiveEvent
    {
        public string id;
        public int since;
        public int expiresSol;         // 0 = until fixed
        public int targetUid;          // e.g. which panel is dusty
    }

    [Serializable]
    public class PlannedStorm
    {
        public int sol;                // storm hits on this sol's night
        public DonkiStorm record;      // the real event it is based on (null on a practice season)
    }

    [Serializable]
    public class Decision
    {
        public int sol;
        public int weight;
        public string text;
        public string kind;            // build | storm | trip | event
    }

    [Serializable]
    public class Trip
    {
        public string spotId;
        public bool warning;           // a hazard showed up while outside
        public string hazard;
        public bool pushed;
        public int gained;
        public int dose;
        public string material;
    }

    [Serializable]
    public class ReportLine
    {
        public string meter;           // power | life | health
        public int delta;
        public string reason;
    }

    [Serializable]
    public class NightReport
    {
        public int sol;
        public List<ReportLine> lines = new List<ReportLine>();
        public bool storm;
        public int stormDamage;
        public bool shelterSealed;
        public PlannedStorm stormInfo;
        public bool systemsDown;
        public List<string> morningNews = new List<string>();
        public string factId;
        public bool ended;
    }
}

using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // Every tunable number in the game. Loaded from Assets/Resources/Data/Balance.json.
    // Hours in JSON are in hours (0.25 steps); the sim works in quarter-hours.
    [Serializable]
    public class Balance
    {
        public StartValues start;
        public ModeDef[] modes;
        public PowerRules power;
        public LifeRules life;
        public HealthRules health;
        public EventRules events;
        public EventDef[] eventDefs;
        public ModuleDef[] modules;
        public SpotDef[] spots;
        public ActionCosts actions;
        public GridDef grid;
        public TraitDef[] traits;
        public EndingRules endings;
        public ActRules acts;
        public FallbackSeason[] fallbackSeasons;
        public SiteDef[] sites;

        public ModeDef Mode(string id)
        {
            foreach (var m in modes) if (m.id == id) return m;
            return modes[0];
        }

        public ModuleDef Module(string id)
        {
            foreach (var m in modules) if (m.id == id) return m;
            return null;
        }

        public EventDef Event(string id)
        {
            foreach (var e in eventDefs) if (e.id == id) return e;
            return null;
        }

        public SpotDef Spot(string id)
        {
            foreach (var s in spots) if (s.id == id) return s;
            return null;
        }

        public TraitDef Trait(string id)
        {
            foreach (var t in traits) if (t.id == id) return t;
            return null;
        }

        public SiteDef Site(string id)
        {
            foreach (var s in sites) if (s.id == id) return s;
            return sites[0];
        }

        public static int Q(float hours) => (int)Math.Round(hours * 4f);
    }

    [Serializable]
    public class StartValues
    {
        public int power = 70, life = 70, health = 80;
        public float hours = 8;
        public int regolith, ice, parts = 3, samples;
    }

    [Serializable]
    public class ModeDef
    {
        public string id;
        public string name;
        public int sols;
        public int maxEvents;
        public string[] seasonKinds;
        public int minStorms;
        public int maxStorms;
        public bool realUnits;
        public float eventChanceScale = 1f;
        public float lateWarningChance;   // from Act 3, chance the Radio can't tell (halved with the comms dish)
    }

    [Serializable]
    public class PowerRules
    {
        public int baseProduction = 24;   // the starting solar array
        public int perPanel = 8;
        public float dustMultiplier = 0.5f;
        public int baseUse = 14;
        public int shelterUse = 12;
        public int batteryReserve = 20;
    }

    [Serializable]
    public class LifeRules
    {
        public int crewUse = 12;
        public int systemsDownDrain = 8;
        public int greenhouseDelaySols = 2;
        public int adjacencyBonus = 2;
    }

    [Serializable]
    public class HealthRules
    {
        public int baseline = 2;
        public int baselineShielded = 1;
        public int stormUnsealed = 22;
        public int stormSealed = 6;
        public int shelterNeighbourMax = 3;
        public int lowLifeThreshold = 25;
        public int lowLifeDrain = 5;
        public int rest = 10;
        public int gym = 4;
        public int fatigueDrain = 3;
        public int dustCleanDose = 2;
        public float doseScar = 0.5f;     // share of storm damage that rest can't heal (lowers max health)
        public float fatigueHours = 7;
    }

    [Serializable]
    public class EventRules
    {
        public int firstSol = 2;
        public float chance = 0.45f;
        public float dustSeasonChance = 0.7f;
        public float dustSeasonDustWeight = 3f;
        public float farPanelDustWeight = 0.5f;
    }

    [Serializable]
    public class EventDef
    {
        public string id;
        public string name;
        public string hint;
        public string fixText;
        public float fixHours = 1;
        public string station;        // module id where it is fixed
        public float weight = 1;
        public string requires;       // module id that must be built, or ""
        public int lifeDrain;
        public int expiresAfter;      // sols; 0 = until fixed
        public string realBasis;
        public bool rolled = true;    // false for events triggered by rules (fatigue)
    }

    [Serializable]
    public class ModuleDef
    {
        public string id;
        public string name;
        public int w = 1, h = 1;
        public int regolith, ice, parts;
        public float buildHours = 2;
        public string placement;      // fixed | inside | connected | open | touching
        public int minDistance;       // open ground: tiles from the base
        public int farDistance;       // open ground: "far" bonus threshold
        public int powerUse;
        public int lifeGain;
        public int powerGain;
        public string plus;
        public string minus;
        public string why;
        public string unlock;         // start | sample | act3
        public int unlockOrder;
        public int maxCount = 1;
        public bool countsAsUpgrade = true;
        public string color;
    }

    [Serializable]
    public class SpotDef
    {
        public string id;
        public string name;
        public string material;       // regolith | ice (shown as "water": baked out of soil) | samples | parts
        public int amount;
        public float hours;
        public int dose;
        public int stormDose;
        public float partChance;
        public string onlyWithEvent;  // e.g. supply
        public string blurb;
    }

    [Serializable]
    public class ActionCosts
    {
        public float rest = 2, radio = 1, seal = 1, exercise = 1, work = 1;
        public float pushLuckHours = 1;
        public int pushLuckDose = 2;
        public float hazardChance = 0.3f;
        public int walkTilesPerQuarter = 8;
    }

    [Serializable]
    public class GridDef
    {
        public int width = 16, height = 10;
        public int maxCorridor = 3;
    }

    [Serializable]
    public class SiteDef
    {
        public string id;
        public string name;
        public string blurb;
        public int[] rocks;           // flattened x,y pairs
        public int iceBonus;
        public Placement[] startModules;
    }

    [Serializable]
    public class Placement
    {
        public string id;
        public int x, y;
        public bool rotated;
    }

    [Serializable]
    public class TraitDef
    {
        public string id;
        public string name;
        public string text;
    }

    [Serializable]
    public class EndingRules
    {
        public int healthyMeter = 40;
        public int thrivingUpgrades = 2;
    }

    [Serializable]
    public class ActRules
    {
        // Act start as a fraction of mission length (30-sol plan: sols 1, 6, 16, 26).
        public float[] starts = { 0f, 5f / 30f, 15f / 30f, 25f / 30f };
    }

    [Serializable]
    public class FallbackSeason
    {
        public int[] nights; // for a 10-sol mission; stretched to the mission length
    }
}

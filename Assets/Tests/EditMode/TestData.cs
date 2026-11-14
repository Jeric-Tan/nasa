using System.IO;
using OutpostGale.Sim;
using UnityEngine;

namespace OutpostGale.Tests
{
    // Loads the real JSON files from disk so tests use the shipped numbers.
    public static class TestData
    {
        static Balance balance;
        static GameData data;

        public static string Root => Application.dataPath;

        public static Balance Balance
        {
            get
            {
                if (balance == null) balance = JsonUtility.FromJson<Balance>(File.ReadAllText(Path.Combine(Root, "Resources/Data/Balance.json")));
                return balance;
            }
        }

        public static GameData Data
        {
            get
            {
                if (data != null) return data;
                data = new GameData
                {
                    donki = JsonUtility.FromJson<DonkiArchive>(File.ReadAllText(Path.Combine(Root, "StreamingAssets/data/donki_seasons.json"))),
                    osdr = JsonUtility.FromJson<FactArchive>(File.ReadAllText(Path.Combine(Root, "StreamingAssets/data/osdr_facts.json"))),
                    radiation = JsonUtility.FromJson<RadiationInfo>(File.ReadAllText(Path.Combine(Root, "StreamingAssets/data/radiation.json"))),
                    story = JsonUtility.FromJson<StoryScript>(File.ReadAllText(Path.Combine(Root, "Resources/Data/Story.json"))),
                };
                data.staticFacts.AddRange(JsonUtility.FromJson<StaticFactList>(File.ReadAllText(Path.Combine(Root, "Resources/Data/Facts.json"))).facts);
                return data;
            }
        }

        public static Mission NewMission(string mode = "cadet", int seed = 1234, string site = "gale")
        {
            var setup = new MissionSetup { mode = mode, site = site, commanderName = "Test" };
            return Mission.Start(Balance, Data, new Profile(), setup, seed);
        }

        // A mission with no storms and no random events, for exact formula checks.
        public static Mission Quiet(string mode = "cadet")
        {
            var m = NewMission(mode);
            m.S.storms.Clear();
            m.S.events.Clear();
            m.S.dustSeasonStart = m.S.dustSeasonEnd = -10;
            return m;
        }

        public static PlacedModule AddBuilt(Mission m, string id, int x, int y, int builtSol = 0)
        {
            var def = m.B.Module(id);
            var pm = new PlacedModule { uid = m.S.nextUid++, id = id, x = x, y = y, w = def.w, h = def.h, built = true, builtSol = builtSol };
            m.S.modules.Add(pm);
            return pm;
        }
    }
}

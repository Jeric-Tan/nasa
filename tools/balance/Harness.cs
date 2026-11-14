// Balance harness: runs the real Sim code and the scripted commander outside Unity.
// Usage: bash tools/balance/run.sh [seeds]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OutpostGale.Sim;
using OutpostGale.Tests;

static class Harness
{
    static T Load<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true });

    static void Main(string[] args)
    {
        int seeds = args.Length > 0 ? int.Parse(args[0]) : 100;
        var b = Load<Balance>("Assets/Resources/Data/Balance.json");
        var data = new GameData
        {
            donki = Load<DonkiArchive>("Assets/StreamingAssets/data/donki_seasons.json"),
            osdr = Load<FactArchive>("Assets/StreamingAssets/data/osdr_facts.json"),
            story = Load<StoryScript>("Assets/Resources/Data/Story.json"),
        };
        data.staticFacts.AddRange(Load<StaticFactList>("Assets/Resources/Data/Facts.json").facts);
        foreach (var mode in new[] { "cadet", "commander" })
        {
            var endings = new Dictionary<string, int>();
            var minHealth = new List<int>(); var finalP = new List<int>(); var finalL = new List<int>(); var finalH = new List<int>();
            var padSol = new List<int>(); var ups = new List<int>(); var downs = 0; var storms = new List<int>();
            for (int seed = 1; seed <= seeds; seed++)
            {
                var m = Mission.Start(b, data, new Profile(), new MissionSetup { mode = mode }, seed * 7919);
                var bot = new ScriptedCommander(m);
                int lo = 100;
                while (!m.S.over)
                {
                    bot.PlaySol();
                    var r = m.Sleep();
                    if (r.systemsDown) downs++;
                    lo = Math.Min(lo, m.S.health);
                }
                endings[m.S.ending] = endings.GetValueOrDefault(m.S.ending) + 1;
                minHealth.Add(lo); finalP.Add(m.S.power); finalL.Add(m.S.life); finalH.Add(m.S.health);
                var pad = m.S.modules.Find(x => x.id == "pad" && x.built); if (pad != null) padSol.Add(pad.builtSol);
                ups.Add(m.UpgradesBuilt(false)); storms.Add(m.S.stormsHit);
            }
            Console.WriteLine($"{mode}: " + string.Join(", ", endings.OrderBy(k => k.Key).Select(k => $"{k.Key} {100.0 * k.Value / seeds:0}%")));
            Console.WriteLine($"   final P/L/H avg {finalP.Average():0}/{finalL.Average():0}/{finalH.Average():0}  min health avg {minHealth.Average():0}  upgrades {ups.Average():0.0}  pad built {padSol.Count}/{seeds} (avg sol {(padSol.Count > 0 ? padSol.Average() : 0):0})  storms {storms.Average():0.0}  power-outs {downs}");
        }
    }
}

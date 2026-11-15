using System;
using System.Collections.Generic;
using System.Globalization;
using OutpostGale.Sim;
using OutpostGale.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OutpostGale
{
    // The command deck screens (status guide, base directory, NASA news) and the welcome tour.
    // Numbers come from Balance.json and the bundled NASA data; nothing here changes the mission.
    public partial class GameManager
    {
        public void OpenKiosk(string id)
        {
            if (!WorldInputOpen) return;
            AudioFx.Click();
            switch (id)
            {
                case "status": Queue(ShowStatusGuide); break;
                case "directory": Queue(ShowDirectory); break;
                case "news": Queue(ShowNews); break;
            }
        }

        RectTransform GuideCard(string title, string subtitle, out RectTransform list, bool dark = false)
        {
            var card = Modal(860, null, dark);
            card.GetComponent<ContentSizeFitter>().enabled = false;
            card.sizeDelta = new Vector2(860, 620);
            UIKit.Label(card, title, 32, dark ? Theme.Paper : Theme.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Label(card, subtitle, 16, dark ? Theme.Soft : Theme.Muted);
            list = UIKit.Scroll(card, out var scroll);
            UIKit.Size(scroll, -1, 420, -1, 420, 1);
            return card;
        }

        static RectTransform Topic(RectTransform list, string title, string text, Color accent, bool dark = false)
        {
            var p = UIKit.Panel(list, dark ? Theme.NightSoft : Color.white, "Topic", true, 14);
            UIKit.VStack(p.gameObject, 4, 14);
            UIKit.Label(p.transform, "<color=#" + ColorUtility.ToHtmlStringRGB(accent) + "><b>" + title + "</b></color>", 19, dark ? Theme.Paper : Theme.Ink);
            if (!string.IsNullOrEmpty(text)) UIKit.Label(p.transform, text, 15, dark ? Theme.Paper : Theme.Ink);
            return p.rectTransform;
        }

        static void SourceLine(RectTransform topic, string source, bool dark = false) =>
            UIKit.Label(topic, "<size=12>Source: " + source + "</size>", 12, dark ? Theme.Soft : Theme.Muted);

        // ---------- status guide ----------

        void ShowStatusGuide(Action done)
        {
            var s = Mission.S;
            var P = Balance.power;
            var L = Balance.life;
            var H = Balance.health;
            var card = GuideCard("How your base works", "Keep all three meters up until the next crew lands after sol " + Mission.Mode.sols + ".", out var list);

            int dustPct = Mathf.RoundToInt(P.dustMultiplier * 100);
            Topic(list, "Power  ·  " + s.power + " (" + MeterWord(s.power) + ")",
                "Runs every machine. The solar array makes " + P.baseProduction + " power a sol, and each extra solar panel adds " + P.perPanel +
                ". The base uses " + P.baseUse + " a sol. Dust on the panels cuts solar power to " + dustPct +
                "%. If power runs out, the machines stop for the night and life support drops by " + L.systemsDownDrain + " more.", Theme.MarsDeep);
            Topic(list, "Life support  ·  " + s.life + " (" + MeterWord(s.life) + ")",
                "Air, water and food. The crew use " + L.crewUse + " a sol. The oxygen maker, greenhouse and recycler add more. If it drops below " +
                H.lowLifeThreshold + ", the crew's health starts to fall too.", Theme.Life);
            Topic(list, "Crew health  ·  " + s.health + " (" + MeterWord(s.health) + ")",
                "Mars has a thin atmosphere and no magnetic shield, so there is always some radiation. A normal sol costs " + H.baseline +
                " health (" + H.baselineShielded + " behind a shield wall). A solar storm costs " + H.stormUnsealed + ", but only " + H.stormSealed +
                " if you seal the storm shelter. Resting in the hab gives back " + H.rest + "." +
                (H.doseScar > 0 ? " Some storm damage never heals, so seal the shelter when Earth warns you." : "") +
                " Radiation so far this mission: " + s.totalDose + ".", Theme.Plus);
            Topic(list, "What happens if a meter runs low",
                "If Life support or Crew health reaches 0, the crew goes home early. If Power reaches 0, the machines stop for the night. " +
                "Keep every meter at " + Balance.endings.healthyMeter + " or more when the next crew lands for the best ending.", Theme.Warn);

            Topic(list, "Supplies", "You bring these back from trips outside through the airlock, then spend them on building.", Theme.Ink);
            Topic(list, "Regolith  ·  you have " + s.regolith, "Loose Mars sand and soil. " + UsedFor(d => d.regolith > 0), Theme.MarsDeep);
            Topic(list, "Water  ·  you have " + s.ice, "Baked out of Mars soil. Curiosity measured about 2% water in the soil at Gale crater. " + UsedFor(d => d.ice > 0), Theme.Life);
            Topic(list, "Parts  ·  you have " + s.parts, "Spare machine parts. " + UsedFor(d => d.parts > 0), Theme.Muted);
            Topic(list, "Samples  ·  you have " + s.samples, "Rock samples for scientists back on Earth. Collect them on trips outside.", Theme.Ink);
            Topic(list, "Time", "Each sol has about " + Balance.start.hours.ToString("0", CultureInfo.InvariantCulture) +
                " hours of daylight. Every job takes time. When you're done, sleep to end the sol. A sol is one day on Mars.", Theme.Ink);

            UIKit.Button(card, "Got it", () => { CloseModal(card); done(); }, Theme.Power);
        }

        string UsedFor(Predicate<ModuleDef> uses)
        {
            var names = new List<string>();
            foreach (var d in Balance.modules) if (uses(d)) names.Add(d.name);
            return names.Count == 0 ? "" : "Used to build: " + string.Join(", ", names) + ".";
        }

        // ---------- directory ----------

        void ShowDirectory(Action done)
        {
            var card = GuideCard("Base directory", "Every room and machine.  + is what it gives you,  − is what it costs.", out var list);
            UIKit.Label(list, "In your base", 20, Theme.Ink, TextAnchor.MiddleLeft, true);
            var seen = new HashSet<string>();
            foreach (var m in Mission.S.modules)
            {
                if (!seen.Add(m.id)) continue;
                var def = Balance.Module(m.id);
                int count = Mission.S.modules.FindAll(x => x.id == m.id).Count;
                var t = Topic(list, def.name + (count > 1 ? "  ×" + count : "") + (m.built ? "" : "  (being built)"), PlusMinus(def), Theme.MarsDeep);
                var target = m;
                bool indoor = m.id == "hab" || m.id == "oxygen" || m.id == "recycler" || m.id == "greenhouse" || m.id == "battery" || m.id == "gym" || m.id == "shelter" || m.id == "airlock";
                var go = UIKit.Button(t, indoor ? "Walk there" : "Show on the map", () =>
                {
                    CloseModal(card);
                    done();
                    if (indoor) World.WalkTo(target);
                    else World.SetInside(false);
                }, Theme.Paper, 15);
                UIKit.Size(go, 180, 40, 180, 40);
            }
            var buildable = Array.FindAll(Balance.modules, d => Mission.S.unlocked.Contains(d.id));
            if (buildable.Length > 0)
            {
                UIKit.Label(list, "You can build (press B)", 20, Theme.Ink, TextAnchor.MiddleLeft, true);
                foreach (var d in buildable)
                    Topic(list, d.name + "  ·  " + Mission.CostText(d), PlusMinus(d), Theme.Plus);
            }
            var locked = Array.FindAll(Balance.modules, d => !Mission.S.unlocked.Contains(d.id) && !seen.Contains(d.id));
            if (locked.Length > 0)
            {
                var names = new List<string>();
                foreach (var d in locked) names.Add(d.name);
                Topic(list, "Coming later", string.Join(", ", names) + ". New plans unlock as the mission goes on.", Theme.Muted);
            }
            UIKit.Button(card, "Close", () => { CloseModal(card); done(); }, Theme.Paper);
        }

        static string PlusMinus(ModuleDef d) =>
            (string.IsNullOrEmpty(d.plus) ? "" : "<color=#1F8A44>+ " + d.plus + "</color>") +
            (string.IsNullOrEmpty(d.minus) ? "" : "\n<color=#D23A2E>− " + d.minus + "</color>");

        // ---------- NASA news ----------

        void ShowNews(Action done)
        {
            var card = GuideCard("NASA news", "Real data from NASA, never made up. It shapes the storms in your mission.", out var list, true);

            var sun = Topic(list, "Today's Sun" + (LiveSun.Live ? "  ·  LIVE" : ""), LiveSun.Headline + (string.IsNullOrEmpty(LiveSun.Detail) ? "" : " " + LiveSun.Detail), Theme.Gold, true);
            SourceLine(sun, LiveSun.Source + (LiveSun.Live ? ", asked just now for the last 30 days" : ", saved archive"), true);

            DonkiSeason season = null;
            if (Data.donki?.seasons != null)
                foreach (var se in Data.donki.seasons) if (se.year == Mission.S.seasonYear) season = se;
            if (season != null)
            {
                var t = Topic(list, "Your mission's storm year: " + season.year,
                    "NASA's space weather office recorded " + season.stormCount + " solar particle storms in " + season.year +
                    (string.IsNullOrEmpty(season.strongestFlare) ? "." : ". The biggest solar flare was class " + season.strongestFlare + ".") +
                    " Every storm night in your mission is one of them, squeezed into " + Mission.Mode.sols + " sols.", Theme.Warn, true);
                SourceLine(t, (season.source ?? "NASA CCMC DONKI") + (string.IsNullOrEmpty(season.retrieved_on) ? "" : ", retrieved " + season.retrieved_on), true);
            }

            var facts = Data.osdr?.facts;
            if (facts != null && facts.Length > 0)
            {
                var f = facts[Mathf.Abs(Mission.S.sol) % facts.Length];
                var t = Topic(list, "Space science of the sol: " + f.title, f.text, Theme.Health, true);
                SourceLine(t, f.source + ", study " + f.id + " (" + f.studyTitle + ")" + (string.IsNullOrEmpty(f.retrieved_on) ? "" : ", retrieved " + f.retrieved_on), true);
            }

            var rad = Data.radiation;
            if (rad != null && rad.chestXray_mSv > 0)
            {
                float xrays = rad.baselineDoseEquivalent_mSvPerSol / rad.chestXray_mSv;
                var t = Topic(list, "Radiation where you live",
                    "Curiosity's RAD instrument measured about " + rad.baselineDoseEquivalent_mSvPerSol.ToString("0.##", CultureInfo.InvariantCulture) +
                    " mSv a sol at Gale crater. That's like " + xrays.ToString("0", CultureInfo.InvariantCulture) + " chest X-rays every day.", Theme.Life, true);
                SourceLine(t, rad.citation, true);
            }

            UIKit.Button(card, "Close", () => { CloseModal(card); done(); }, Theme.Gold);
        }

        // ---------- welcome tour (new missions) ----------

        void QueueTour()
        {
            string[] lines =
            {
                "Welcome inside, {name}! This is the command deck. Walk with A and D, or click where you want to go.",
                "The Status board shows your three meters: Power, Life support and Crew health. Keep all three up until the next crew lands.",
                "The Directory lists every room and what it does. The NASA News screen shows real data, like what the Sun did this month.",
                "Stand next to a screen or a room and press E to use it. Press I for the map view, where you build and head outside.",
            };
            Queue(d => { skippingStory = false; d(); });
            foreach (var text in lines)
            {
                var line = new StoryLine { speaker = "bolt", text = text };
                Queue(d => { if (skippingStory) d(); else ShowDialogue(line, d); });
            }
        }
    }
}

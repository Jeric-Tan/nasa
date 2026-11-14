using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OutpostGale.Sim;
using UnityEngine;

namespace OutpostGale.Tests
{
    public class NightFormulaTests
    {
        [Test]
        public void StartValuesComeFromBalance()
        {
            var m = TestData.NewMission();
            Assert.AreEqual(70, m.S.power);
            Assert.AreEqual(70, m.S.life);
            Assert.AreEqual(80, m.S.health);
            Assert.AreEqual(32, m.S.quartersLeft);
            Assert.AreEqual(3, m.S.parts);
            Assert.AreEqual(20, m.S.lastSol);
            Assert.AreEqual(30, TestData.NewMission("commander").S.lastSol);
        }

        [Test]
        public void QuietNight()
        {
            var m = TestData.Quiet();
            var r = m.Sleep();
            Assert.AreEqual(80, m.S.power, "70 + 24 - 14");
            Assert.AreEqual(58, m.S.life, "70 - 12");
            Assert.AreEqual(78, m.S.health, "80 - 2");
            Assert.AreEqual(3, r.lines.Count);
            Assert.IsTrue(r.lines.All(l => !string.IsNullOrEmpty(l.reason)), "every line says why");
            Assert.AreEqual(2, m.S.sol);
            Assert.AreEqual(32, m.S.quartersLeft);
        }

        [Test]
        public void DustHalvesSolar()
        {
            var m = TestData.Quiet();
            m.AddEvent("dust", null);
            m.Sleep();
            Assert.AreEqual(70 + 12 - 14, m.S.power);
        }

        [Test]
        public void LeakDrainsLifeSupport()
        {
            var m = TestData.Quiet();
            m.AddEvent("leak", null);
            m.Sleep();
            Assert.AreEqual(70 - 12 - 6, m.S.life);
        }

        [Test]
        public void UnsealedStorm()
        {
            var m = TestData.Quiet();
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            var r = m.Sleep();
            Assert.IsTrue(r.storm);
            Assert.AreEqual(22, r.stormDamage);
            Assert.AreEqual(80 - 2 - 22, m.S.health);
        }

        [Test]
        public void SealedStormUsesPowerAndNeighboursBlock()
        {
            var m = TestData.Quiet();
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            Assert.IsTrue(m.Do("seal", m.S.modules.First(x => x.id == "shelter").uid).ok);
            var r = m.Sleep();
            Assert.AreEqual(5, r.stormDamage, "6 sealed, -1 because the hab touches the shelter");
            Assert.AreEqual(80 - 2 - 5, m.S.health);
            Assert.AreEqual(70 + 24 - 14 - 12, m.S.power);
        }

        [Test]
        public void SealingWithoutStormStillCostsPower()
        {
            var m = TestData.Quiet();
            m.Do("seal", m.S.modules.First(x => x.id == "shelter").uid);
            m.Sleep();
            Assert.AreEqual(70 + 24 - 14 - 12, m.S.power);
            Assert.AreEqual(78, m.S.health);
        }

        [Test]
        public void ShieldWallHalvesRadiationForTheRoomItTouches()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "wall", 3, 3); // touches the hab only
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            var r = m.Sleep();
            Assert.AreEqual(11, r.stormDamage);
            Assert.AreEqual(80 - 1 - 11, m.S.health);
        }

        [Test]
        public void ShieldedShelterAndWallNeighbour()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "wall", 2, 3); // touches the shelter
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            m.Do("seal", m.S.modules.First(x => x.id == "shelter").uid);
            var r = m.Sleep();
            Assert.AreEqual(2, r.stormDamage, "6 - 2 neighbours = 4, halved by the wall");
        }

        [Test]
        public void OxygenMakerTradeOff()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "oxygen", 3, 6);
            m.Sleep();
            Assert.AreEqual(70 + 24 - 14 - 8, m.S.power);
            Assert.AreEqual(70 - 12 + 10, m.S.life);
        }

        [Test]
        public void ModulesSwitchOnTheSolAfterBuilding()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "oxygen", 3, 6, builtSol: 1);
            m.Sleep();
            Assert.AreEqual(58, m.S.life, "not active on the sol it was built");
        }

        [Test]
        public void GreenhouseStartsTwoSolsLater()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "greenhouse", 3, 6, builtSol: 1);
            int life = m.S.life;
            m.Sleep(); m.S.events.Clear();
            Assert.AreEqual(life - 12, m.S.life, "sol 1: still growing");
            life = m.S.life;
            m.Sleep(); m.S.events.Clear();
            Assert.AreEqual(life - 12, m.S.life, "sol 2: still growing");
            life = m.S.life;
            m.Sleep();
            Assert.AreEqual(life - 12 + 7, m.S.life, "sol 3: food");
        }

        [Test]
        public void OxygenNextToGreenhouseBonus()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "greenhouse", 3, 6, builtSol: -5);
            TestData.AddBuilt(m, "oxygen", 5, 6);
            m.Sleep();
            Assert.AreEqual(70 - 12 + 10 + 7 + 2, m.S.life);
        }

        [Test]
        public void PowerOutStopsMachines()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "oxygen", 3, 6);
            m.S.power = 0;
            m.Do("seal", m.S.modules.First(x => x.id == "shelter").uid);
            var r = m.Sleep();
            Assert.IsTrue(r.systemsDown);
            Assert.AreEqual(0, m.S.power);
            Assert.AreEqual(70 - 12 - 8, m.S.life, "no oxygen and -8 for machines stopping");
            Assert.IsFalse(m.S.over, "running out of power never ends the game");
        }

        [Test]
        public void BatteryCoversADarkNight()
        {
            var m = TestData.Quiet();
            TestData.AddBuilt(m, "oxygen", 3, 6);
            TestData.AddBuilt(m, "battery", 6, 5);
            m.S.power = 0;
            m.Do("seal", m.S.modules.First(x => x.id == "shelter").uid);
            var r = m.Sleep();
            Assert.IsFalse(r.systemsDown);
            Assert.AreEqual(70 - 12 + 10, m.S.life);
        }

        [Test]
        public void LowLifeSupportHurtsHealth()
        {
            var m = TestData.Quiet();
            m.S.life = 30;
            m.Sleep();
            Assert.AreEqual(18, m.S.life);
            Assert.AreEqual(80 - 2 - 5, m.S.health);
        }

        [Test]
        public void RestAndGym()
        {
            var m = TestData.Quiet();
            m.S.health = 50;
            var gym = TestData.AddBuilt(m, "gym", 6, 5);
            Assert.IsTrue(m.Do("rest", m.S.modules.First(x => x.id == "hab").uid).ok);
            Assert.IsTrue(m.Do("exercise", gym.uid).ok);
            m.Sleep();
            int rest = m.HasTrait("light_sleeper") ? 15 : 10;
            Assert.AreEqual(50 + rest - 2 + 4, m.S.health);
        }
    }

    public class EndingTests
    {
        [Test]
        public void EarlyRideHomeWhenHealthRunsOut()
        {
            var m = TestData.Quiet();
            m.S.health = 10;
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            var r = m.Sleep();
            Assert.IsTrue(r.ended);
            Assert.AreEqual("early", m.S.ending);
            Assert.AreEqual(1, m.S.sol);
        }

        [Test]
        public void ThrivingNeedsPadHealthyMetersAndUpgrades()
        {
            var m = TestData.Quiet();
            m.S.sol = m.S.lastSol;
            TestData.AddBuilt(m, "pad", 12, 4);
            TestData.AddBuilt(m, "oxygen", 3, 6);
            TestData.AddBuilt(m, "greenhouse", 6, 5, builtSol: -5);
            TestData.AddBuilt(m, "recycler", 3, 3);
            m.Sleep();
            Assert.AreEqual("thriving", m.S.ending);
        }

        [Test]
        public void ReadyJustBarely()
        {
            var m = TestData.Quiet();
            m.S.sol = m.S.lastSol;
            m.S.life = 30;
            TestData.AddBuilt(m, "pad", 12, 4);
            m.Sleep();
            Assert.AreEqual("ready", m.S.ending);
        }

        [Test]
        public void HoldingOnWithoutPad()
        {
            var m = TestData.Quiet();
            m.S.sol = m.S.lastSol;
            m.Sleep();
            Assert.AreEqual("holding", m.S.ending);
        }

        [Test]
        public void TopDecisionsAreAtMostThree()
        {
            var m = TestData.NewMission(seed: 77);
            new ScriptedCommander(m).PlayMission();
            var top = m.TopDecisions();
            Assert.LessOrEqual(top.Count, 3);
            Assert.Greater(top.Count, 0);
        }
    }

    public class PlacementTests
    {
        [Test]
        public void PanelsNeedOpenGround()
        {
            var m = TestData.Quiet();
            m.S.parts = 10;
            Assert.IsTrue(m.CheckPlace("panel", 4, 1, false).ok, "3 tiles from the base");
            Assert.IsFalse(m.CheckPlace("panel", 4, 2, false).ok, "2 tiles is too close");
            var far = m.CheckPlace("panel", 12, 8, false);
            Assert.IsTrue(far.ok);
            Assert.IsTrue(far.far, "6+ tiles: far panel");
        }

        [Test]
        public void RocksBlockBuilding()
        {
            var m = TestData.Quiet();
            m.S.parts = 10;
            var r = m.CheckPlace("panel", 8, 1, false);
            Assert.IsFalse(r.ok);
            StringAssert.Contains("Rocks", r.reason);
        }

        [Test]
        public void ConnectedModulesNeedACorridor()
        {
            var m = TestData.Quiet();
            m.S.parts = 10; m.S.ice = 5;
            var touching = m.CheckPlace("oxygen", 3, 6, false);
            Assert.IsTrue(touching.ok);
            Assert.AreEqual(0, touching.corridor.Count);
            var gap = m.CheckPlace("oxygen", 3, 7, false);
            Assert.IsTrue(gap.ok);
            Assert.AreEqual(1, gap.corridor.Count);
            Assert.IsFalse(m.CheckPlace("oxygen", 11, 6, false).ok, "too far to connect");
        }

        [Test]
        public void PlaceReservesAndCancelRefunds()
        {
            var m = TestData.Quiet();
            m.S.ice = 1;
            Assert.IsTrue(m.Place("oxygen", 3, 7, false).ok);
            Assert.AreEqual(1, m.S.parts);
            Assert.AreEqual(0, m.S.ice);
            Assert.AreEqual(1, m.S.corridors.Count);
            var site = m.S.modules.Last();
            Assert.IsTrue(m.Do("cancel", site.uid).ok);
            Assert.AreEqual(3, m.S.parts);
            Assert.AreEqual(1, m.S.ice);
            Assert.AreEqual(0, m.S.corridors.Count);
        }

        [Test]
        public void ConstructionTakesHours()
        {
            var m = TestData.Quiet();
            m.S.ice = 1;
            m.Place("oxygen", 3, 6, false);
            var site = m.S.modules.Last();
            Assert.IsTrue(m.Do("work", site.uid).ok);
            Assert.IsFalse(site.built);
            Assert.IsTrue(m.Do("work", site.uid).ok);
            Assert.IsTrue(site.built);
            Assert.AreEqual(1, site.builtSol);
        }

        [Test]
        public void WallsMustTouchAModule()
        {
            var m = TestData.Quiet();
            m.S.regolith = 10;
            Assert.IsFalse(m.CheckPlace("wall", 10, 6, false).ok);
            Assert.IsTrue(m.CheckPlace("wall", 3, 3, false).ok);
        }

        [Test]
        public void LandingPadNeedsDistance()
        {
            var m = TestData.Quiet();
            m.S.unlocked.Add("pad");
            m.S.regolith = 10;
            Assert.IsTrue(m.CheckPlace("pad", 12, 4, false).ok);
            Assert.IsFalse(m.CheckPlace("pad", 9, 4, false).ok);
        }

        [Test]
        public void LockedModulesCantBePlaced()
        {
            var m = TestData.Quiet();
            m.S.ice = 5; m.S.regolith = 5;
            Assert.IsFalse(m.CheckPlace("greenhouse", 3, 6, false).ok);
        }

        [Test]
        public void WalkingCostsTimeToFarStations()
        {
            var m = TestData.Quiet();
            var grid = m.Layout();
            Assert.AreEqual(1, grid.WalkQuarters(grid.First("solar"), false), "5 tiles rounds to 0.25 h");
            Assert.AreEqual(0, grid.WalkQuarters(grid.First("airlock"), false));
            Assert.AreEqual(0, grid.WalkQuarters(grid.First("hab"), false));
        }

        [Test]
        public void PathfindingAvoidsRocks()
        {
            var m = TestData.Quiet();
            var grid = m.Layout();
            // Rocks at (8,1) and (9,1): going from (8,2) down to (8,0) has to step around them.
            var path = GridPath.Find(grid, new Cell(8, 2), new Cell(8, 0));
            Assert.IsNotNull(path);
            Assert.IsTrue(path.All(c => !grid.IsRock(c.x, c.y)));
            Assert.AreEqual(5, path.Count);
        }
    }

    public class ExpeditionTests
    {
        [Test]
        public void CraterTripGivesRegolithAndDose()
        {
            var m = TestData.Quiet();
            Assert.IsTrue(m.StartTrip("crater").ok);
            Assert.AreEqual(2, m.S.regolith);
            Assert.AreEqual(77, m.S.health);
            Assert.AreEqual(32 - 8, m.S.quartersLeft);
            m.EndTrip();
            Assert.IsFalse(m.TripActive);
        }

        [Test]
        public void StormSolDoseIsHigher()
        {
            var m = TestData.Quiet();
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            m.StartTrip("crater");
            Assert.AreEqual(75, m.S.health);
        }

        [Test]
        public void PushYourLuck()
        {
            var m = TestData.Quiet();
            m.StartTrip("crater");
            int dose = m.PushDose;
            Assert.IsTrue(m.PushLuck().ok);
            Assert.AreEqual(3, m.S.regolith);
            Assert.AreEqual(77 - dose, m.S.health);
            Assert.IsFalse(m.CanPush, "only once per trip");
        }

        [Test]
        public void SamplesUnlockBlueprints()
        {
            var m = TestData.Quiet();
            Assert.IsFalse(m.S.unlocked.Contains("greenhouse"));
            m.StartTrip("science");
            m.EndTrip();
            Assert.IsTrue(m.S.unlocked.Contains("greenhouse"));
            Assert.IsTrue(m.P.blueprints.Contains("greenhouse"), "carries over to the next mission");
        }

        [Test]
        public void SupplySpotOnlyWithEvent()
        {
            var m = TestData.Quiet();
            Assert.IsFalse(m.StartTrip("supply").ok);
            m.AddEvent("supply", null);
            Assert.IsTrue(m.StartTrip("supply").ok);
            Assert.AreEqual(5, m.S.parts);
            Assert.IsNull(m.EventActive("supply"));
        }

        [Test]
        public void NotEnoughDaylight()
        {
            var m = TestData.Quiet();
            m.S.quartersLeft = 4;
            Assert.IsFalse(m.StartTrip("ice").ok);
        }
    }

    public class SeasonAndDeckTests
    {
        [Test]
        public void RealSeasonsFromDonki()
        {
            foreach (var mode in new[] { "cadet", "commander" })
                for (int seed = 1; seed <= 40; seed++)
                {
                    var m = TestData.NewMission(mode, seed);
                    var md = m.Mode;
                    Assert.IsTrue(m.S.seasonReal, "bundled archive has enough seasons");
                    Assert.That(m.S.storms.Count, Is.InRange(md.minStorms - 1, md.maxStorms));
                    Assert.IsTrue(m.S.storms.All(st => st.sol >= 2 && st.sol <= m.S.lastSol));
                    Assert.IsTrue(m.S.storms.Any(st => st.sol >= m.S.lastSol - 1), "finale storm");
                    Assert.IsTrue(m.S.storms.All(st => !string.IsNullOrEmpty(st.record.date) && !string.IsNullOrEmpty(st.record.id)));
                    Assert.AreEqual(m.S.storms.Count, m.S.storms.Select(st => st.sol).Distinct().Count());
                    var kinds = md.seasonKinds;
                    var season = TestData.Data.donki.seasons.First(s => s.year == m.S.seasonYear);
                    CollectionAssert.Contains(kinds, season.kind);
                }
        }

        [Test]
        public void PracticeSeasonWithoutData()
        {
            var m = Mission.Start(TestData.Balance, new GameData(), new Profile(), new MissionSetup(), 5);
            Assert.IsFalse(m.S.seasonReal);
            Assert.Greater(m.S.storms.Count, 0);
        }

        [Test]
        public void EventsNeverDuplicateOrOverflow()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var m = TestData.NewMission(seed % 2 == 0 ? "cadet" : "commander", seed);
                var bot = new ScriptedCommander(m);
                while (!m.S.over)
                {
                    int rolled = m.S.events.Count(e => m.B.Event(e.id).rolled);
                    Assert.LessOrEqual(rolled, m.Mode.maxEvents);
                    Assert.AreEqual(m.S.events.Count, m.S.events.Select(e => e.id).Distinct().Count());
                    bot.PlaySol();
                    m.Sleep();
                }
            }
        }

        [Test]
        public void SameSeedSameMission()
        {
            string Run()
            {
                var m = TestData.NewMission("commander", 4242);
                new ScriptedCommander(m).PlayMission();
                return JsonUtility.ToJson(m.S);
            }
            Assert.AreEqual(Run(), Run());
        }

        [Test]
        public void ClassroomCodeGivesTheSameSeed()
        {
            Assert.AreEqual(MissionRng.SeedFromCode("abc234"), MissionRng.SeedFromCode("ABC234 "));
            Assert.AreNotEqual(MissionRng.SeedFromCode("ABC234"), MissionRng.SeedFromCode("ABC235"));
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var m = TestData.NewMission("cadet", 99);
            var bot = new ScriptedCommander(m);
            for (int i = 0; i < 5; i++) { bot.PlaySol(); m.Sleep(); }
            string json = JsonUtility.ToJson(m.S);
            var loaded = JsonUtility.FromJson<MissionState>(json);
            Assert.AreEqual(json, JsonUtility.ToJson(loaded));
            var m2 = new Mission(TestData.Balance, TestData.Data, loaded, new Profile());
            Assert.IsFalse(m2.TripActive);
            m2.Sleep();
            m.Sleep();
            Assert.AreEqual(JsonUtility.ToJson(m.S), JsonUtility.ToJson(m2.S));
        }
    }

    public class BalanceRun
    {
        [Test]
        public void HundredSeedsScriptedCommander()
        {
            foreach (var mode in new[] { "cadet", "commander" })
            {
                var endings = new Dictionary<string, int>();
                int totalSols = 0;
                for (int seed = 1; seed <= 100; seed++)
                {
                    var m = TestData.NewMission(mode, seed * 7919);
                    new ScriptedCommander(m).PlayMission();
                    endings.TryGetValue(m.S.ending, out int n);
                    endings[m.S.ending] = n + 1;
                    totalSols += m.S.sol;
                }
                string summary = mode + ": " + string.Join(", ", endings.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value + "%")) +
                                 " | avg sols " + (totalSols / 100f);
                Debug.Log("[Balance] " + summary);
                TestContext.WriteLine(summary);
                endings.TryGetValue("thriving", out int thriving);
                endings.TryGetValue("early", out int early);
                // The plan's target: a sensible scripted strategy wins roughly 50–80% of the time.
                Assert.That(thriving, Is.InRange(50, 80), mode + " win rate for a sensible strategy: " + summary);
                Assert.Less(early, 35, "a sensible strategy rarely loses: " + summary);
            }
        }
    }

    public class StoryAndDataTests
    {
        [Test]
        public void StoryScriptIsComplete()
        {
            var story = TestData.Data.story;
            foreach (var id in new[] { "thriving", "ready", "holding", "early" })
                Assert.IsNotNull(story.EndingFor(id), id);
            var speakers = story.speakers.Select(s => s.Split('=')[0]).ToList();
            foreach (var beat in story.beats)
            {
                Assert.LessOrEqual(beat.lines.Length, 3, beat.id + ": 3 lines at most per beat");
                foreach (var line in beat.lines)
                {
                    CollectionAssert.Contains(speakers, line.speaker, beat.id);
                    var terms = new List<string>();
                    StoryDirector.Format(line.text, new MissionState(), terms);
                    foreach (var t in terms) Assert.IsNotNull(story.TermFor(t), beat.id + " uses unknown term " + t);
                }
            }
        }

        [Test]
        public void BeatsPlayOnce()
        {
            var m = TestData.NewMission();
            var first = StoryDirector.BeatsFor(m.D.story, m.S, m.TakeTriggers());
            Assert.IsTrue(first.Any(b => b.id == "intro"));
            m.Trigger("act:1");
            Assert.AreEqual(0, StoryDirector.BeatsFor(m.D.story, m.S, m.TakeTriggers()).Count);
        }

        [Test]
        public void NasaDataKeepsSources()
        {
            var d = TestData.Data;
            Assert.GreaterOrEqual(d.donki.seasons.Length, 10);
            foreach (var s in d.donki.seasons)
            {
                Assert.IsFalse(string.IsNullOrEmpty(s.source));
                Assert.IsFalse(string.IsNullOrEmpty(s.retrieved_on));
            }
            foreach (var f in d.osdr.facts)
            {
                StringAssert.StartsWith("OSD-", f.id);
                Assert.IsFalse(string.IsNullOrEmpty(f.studyTitle), f.id + " title fetched from OSDR");
            }
            Assert.AreEqual(0.64f, d.radiation.baselineDoseEquivalent_mSvPerSol, 0.001f);
        }

        [Test]
        public void EveryUpgradeHasATradeOff()
        {
            foreach (var mod in TestData.Balance.modules.Where(x => x.unlock != "none"))
            {
                Assert.IsFalse(string.IsNullOrEmpty(mod.plus), mod.id);
                Assert.IsFalse(string.IsNullOrEmpty(mod.minus), mod.id + " needs a downside");
                Assert.IsFalse(string.IsNullOrEmpty(mod.why), mod.id + " needs a real-world reason");
            }
        }
    }
}

namespace OutpostGale.Tests
{
    public class LateWarningTests
    {
        [Test]
        public void CommanderRadioCanBeLateInAct3()
        {
            int late = 0;
            for (int seed = 1; seed <= 60; seed++)
            {
                var m = TestData.NewMission("commander", seed);
                m.S.sol = Mission.ActStartSol(m.B, m.S.lastSol, 3);
                m.Do("radio", m.S.modules.Find(x => x.id == "hab").uid);
                if (m.S.forecastTonight == 3) late++;
            }
            Assert.That(late, Is.InRange(15, 45), "about half the time");
        }

        [Test]
        public void CadetRadioIsNeverLate()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var m = TestData.NewMission("cadet", seed);
                m.S.sol = Mission.ActStartSol(m.B, m.S.lastSol, 3);
                m.Do("radio", m.S.modules.Find(x => x.id == "hab").uid);
                Assert.AreNotEqual(3, m.S.forecastTonight);
            }
        }
    }
}

namespace OutpostGale.Tests
{
    public class DoseTests
    {
        [Test]
        public void StormDoseLowersMaxHealthAndRestCantUndoIt()
        {
            var m = TestData.Quiet();
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            m.Sleep();
            Assert.AreEqual(100 - 22, m.S.healthCap);
            m.S.health = m.S.healthCap - 1;
            m.Do("rest", m.S.modules.Find(x => x.id == "hab").uid);
            Assert.AreEqual(m.S.healthCap, m.S.health, "rest stops at the cap");
        }

        [Test]
        public void SealedShelterKeepsMaxHealthHigh()
        {
            var m = TestData.Quiet();
            m.S.storms.Add(new PlannedStorm { sol = 1 });
            m.Do("seal", m.S.modules.Find(x => x.id == "shelter").uid);
            m.Sleep();
            Assert.AreEqual(95, m.S.healthCap);
        }
    }
}

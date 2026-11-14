using System;
using System.Collections;
using System.IO;
using OutpostGale.Sim;
using UnityEngine;
using UnityEngine.Networking;

namespace OutpostGale
{
    // Loads game JSON (Resources) and the bundled NASA archive (StreamingAssets, via UnityWebRequest so it works on WebGL).
    public static class DataLoader
    {
        public static Balance LoadBalance() => JsonUtility.FromJson<Balance>(Resources.Load<TextAsset>("Data/Balance").text);

        public static IEnumerator LoadAll(Action<GameData, string> done)
        {
            var data = new GameData
            {
                story = JsonUtility.FromJson<StoryScript>(Resources.Load<TextAsset>("Data/Story").text),
            };
            var facts = JsonUtility.FromJson<StaticFactList>(Resources.Load<TextAsset>("Data/Facts").text);
            if (facts?.facts != null) data.staticFacts.AddRange(facts.facts);

            string error = null;
            yield return Fetch("donki_seasons.json", t => data.donki = JsonUtility.FromJson<DonkiArchive>(t), e => error = e);
            yield return Fetch("osdr_facts.json", t => data.osdr = JsonUtility.FromJson<FactArchive>(t), e => error = e);
            yield return Fetch("radiation.json", t => data.radiation = JsonUtility.FromJson<RadiationInfo>(t), e => error = e);
            done(data, error);
        }

        static IEnumerator Fetch(string file, Action<string> ok, Action<string> fail)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "data", file);
            if (!path.Contains("://")) path = "file://" + path;
            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { ok(req.downloadHandler.text); }
                    catch (Exception e) { fail(file + ": " + e.Message); }
                }
                else fail(file + ": " + req.error);
            }
        }
    }

    // "Today's Sun": one optional live DONKI call at startup. If it fails (offline, CORS), the banner
    // keeps the archive and its label says REAL ARCHIVE instead of LIVE.
    public static class LiveSun
    {
        public static bool Live;
        public static bool Done;
        public static string Headline = "Checking today's Sun…";
        public static string Detail = "";
        public static string Source = "NASA CCMC DONKI";

        public static IEnumerator Fetch(GameData archive)
        {
            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-30);
            string url = "https://ccmc.gsfc.nasa.gov/DONKI/WS/get/FLR?startDate=" + start.ToString("yyyy-MM-dd") + "&endDate=" + end.ToString("yyyy-MM-dd");
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 8;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var flares = JsonUtility.FromJson<FlareList>("{\"items\":" + req.downloadHandler.text + "}");
                        Summarise(flares.items);
                        Live = true;
                    }
                    catch (Exception) { Live = false; }
                }
            }
            if (!Live) FromArchive(archive);
            Done = true;
        }

        [Serializable] class Flare { public string flrID; public string peakTime; public string beginTime; public string classType; }
        [Serializable] class FlareList { public Flare[] items; }

        static void Summarise(Flare[] items)
        {
            if (items == null || items.Length == 0)
            {
                Headline = "The Sun has been quiet: no big flares in the last 30 days.";
                Detail = "";
                return;
            }
            Flare best = null;
            int m = 0, x = 0;
            foreach (var f in items)
            {
                if (string.IsNullOrEmpty(f.classType)) continue;
                if (f.classType.StartsWith("X")) x++;
                if (f.classType.StartsWith("M")) m++;
                if (best == null || Rank(f.classType) > Rank(best.classType)) best = f;
            }
            string when = best != null ? StormPlanner.NiceDate((best.peakTime ?? best.beginTime ?? "").Length >= 10 ? (best.peakTime ?? best.beginTime).Substring(0, 10) : "") : "";
            Headline = items.Length + " solar flares in the last 30 days" + (x > 0 ? " (" + x + " X-class!)" : m > 0 ? " (" + m + " M-class)" : "") + ".";
            Detail = best != null ? "Biggest: class " + best.classType + " on " + when + "." : "";
        }

        static float Rank(string cls)
        {
            if (string.IsNullOrEmpty(cls)) return 0;
            int letter = "ABCMX".IndexOf(cls[0]);
            float.TryParse(cls.Substring(1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float n);
            return letter * 100 + n;
        }

        public static void FromArchive(GameData archive)
        {
            Live = false;
            if (archive?.donki?.seasons == null || archive.donki.seasons.Length == 0)
            {
                Headline = "Space weather archive not loaded.";
                return;
            }
            DonkiSeason latest = archive.donki.seasons[archive.donki.seasons.Length - 1];
            Headline = "In " + latest.year + " NASA logged " + StormPlanner.DistinctDays(latest) + " days with solar particle storms.";
            Detail = string.IsNullOrEmpty(latest.strongestFlare) ? "" : "Biggest flare that year: class " + latest.strongestFlare + ".";
        }
    }

    // Local saves in the browser (PlayerPrefs maps to IndexedDB on WebGL). No account.
    public static class SaveSystem
    {
        const string MissionKey = "og_mission_v1";
        const string ProfileKey = "og_profile_v1";

        public static Profile LoadProfile()
        {
            try
            {
                string json = PlayerPrefs.GetString(ProfileKey, "");
                if (!string.IsNullOrEmpty(json)) return JsonUtility.FromJson<Profile>(json) ?? new Profile();
            }
            catch (Exception) { }
            return new Profile();
        }

        public static void SaveProfile(Profile p)
        {
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(p));
            PlayerPrefs.Save();
        }

        public static MissionState LoadMission()
        {
            try
            {
                string json = PlayerPrefs.GetString(MissionKey, "");
                if (string.IsNullOrEmpty(json)) return null;
                var s = JsonUtility.FromJson<MissionState>(json);
                return s != null && !s.over && s.version == 1 ? s : null;
            }
            catch (Exception) { return null; }
        }

        public static void SaveMission(MissionState s)
        {
            if (s == null) return;
            PlayerPrefs.SetString(MissionKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        public static void ClearMission()
        {
            PlayerPrefs.DeleteKey(MissionKey);
            PlayerPrefs.Save();
        }
    }
}

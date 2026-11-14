using System.Collections.Generic;
using UnityEngine;

namespace OutpostGale
{
    // Sound effects. Uses imported clips from Resources/Audio/<name> when present,
    // otherwise small synthesized placeholders so the game always has feedback.
    public class AudioFx : MonoBehaviour
    {
        static AudioFx inst;
        AudioSource sfx, ambience, music;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static bool Muted;
        public static bool HasRealWind => Resources.Load<AudioClip>("Audio/mars_wind") != null;

        public static void Init(GameObject host)
        {
            if (inst) return;
            inst = host.AddComponent<AudioFx>();
            inst.sfx = host.AddComponent<AudioSource>();
            inst.ambience = host.AddComponent<AudioSource>();
            inst.ambience.loop = true;
            inst.ambience.volume = 0.35f;
            inst.music = host.AddComponent<AudioSource>();
            inst.music.loop = true;
            inst.music.volume = 0.25f;
        }

        AudioClip Get(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (!c) c = Synth(name);
            clips[name] = c;
            return c;
        }

        static void Play(string name, float volume = 0.6f)
        {
            if (!inst || Muted) return;
            var c = inst.Get(name);
            if (c) inst.sfx.PlayOneShot(c, volume);
        }

        public static void Click() => Play("click", 0.35f);
        public static void Clunk() => Play("build", 0.8f);
        public static void Siren() => Play("siren", 0.5f);
        public static void Chime() => Play("chime", 0.5f);
        public static void Step() => Play("step", 0.15f);
        public static void Error() => Play("error", 0.4f);
        public static void Trip() => Play("trip", 0.5f);
        public static void Radio() => Play("radio", 0.35f);
        public static void DustDevil() => Play("dust_devil", 0.8f);

        public static void Wind(bool on)
        {
            if (!inst) return;
            if (!on || Muted) { inst.ambience.Stop(); return; }
            if (inst.ambience.isPlaying) return;
            inst.ambience.clip = inst.Get("mars_wind");
            inst.ambience.Play();
        }

        // Optional music loops: Resources/Audio/music_calm and music_tense.
        public static void Music(string name)
        {
            if (!inst) return;
            var c = Resources.Load<AudioClip>("Audio/" + name);
            if (!c || Muted) { inst.music.Stop(); return; }
            if (inst.music.clip == c && inst.music.isPlaying) return;
            inst.music.clip = c;
            inst.music.Play();
        }

        public static void SetMuted(bool muted)
        {
            Muted = muted;
            if (!inst) return;
            if (muted) { inst.ambience.Stop(); inst.music.Stop(); }
        }

        static AudioClip Synth(string name)
        {
            const int rate = 22050;
            float len = name == "mars_wind" ? 6f : name == "siren" ? 1.2f : name == "build" ? 0.35f : name == "chime" ? 0.5f : 0.08f;
            int n = (int)(rate * len);
            var data = new float[n];
            var rnd = new System.Random(name.GetHashCode());
            float brown = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Clamp01(1 - t / len);
                switch (name)
                {
                    case "click": data[i] = Mathf.Sin(t * 2 * Mathf.PI * 900) * env * env * 0.4f; break;
                    case "step": data[i] = (float)(rnd.NextDouble() * 2 - 1) * env * env * 0.3f; break;
                    case "error": data[i] = Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * 160)) * env * 0.2f; break;
                    case "build":
                        data[i] = (Mathf.Sin(t * 2 * Mathf.PI * (110 - 60 * t)) + (float)(rnd.NextDouble() * 2 - 1) * 0.3f * env) * env * env * 0.7f;
                        break;
                    case "chime":
                        data[i] = (Mathf.Sin(t * 2 * Mathf.PI * 880) + 0.5f * Mathf.Sin(t * 2 * Mathf.PI * 1320)) * env * 0.3f;
                        break;
                    case "siren":
                        float f = (t % 0.6f) < 0.3f ? 660 : 880;
                        data[i] = Mathf.Sin(t * 2 * Mathf.PI * f) * 0.25f * Mathf.Clamp01(len - t);
                        break;
                    case "mars_wind":
                        // Brown noise with a slow swell (placeholder; drop the real recording in Resources/Audio/mars_wind).
                        brown = Mathf.Clamp(brown + (float)(rnd.NextDouble() * 2 - 1) * 0.02f, -1, 1);
                        float swell = 0.5f + 0.5f * Mathf.Sin(t * 2 * Mathf.PI / len);
                        float fade = Mathf.Min(1, Mathf.Min(t, len - t) * 4);
                        data[i] = brown * 0.6f * (0.4f + 0.6f * swell) * fade;
                        break;
                }
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using OutpostGale.Sim;
using OutpostGale.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OutpostGale
{
    // Boots the game, owns the Mission, and runs the screen flow. Rules live in OutpostGale.Sim.
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public Balance Balance;
        public GameData Data;
        public Profile Profile;
        public Mission Mission;
        public WorldView World;

        Canvas canvas;
        RectTransform screenLayer, hudLayer, modalLayer, toastLayer;
        enum Phase { Loading, Title, Setup, Outpost, Ending }
        Phase phase = Phase.Loading;

        readonly Queue<Action<Action>> queue = new Queue<Action<Action>>();
        bool queueBusy;
        bool nightRunning;
        bool firstInput;

        public bool WorldInputOpen => phase == Phase.Outpost && !queueBusy && !nightRunning && modalLayer.childCount == 0 && !Mission.S.over;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I) return;
            new GameObject("Outpost Gale").AddComponent<GameManager>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            AudioFx.Init(gameObject);
            Balance = DataLoader.LoadBalance();
            Profile = SaveSystem.LoadProfile();
            AudioFx.SetMuted(Profile.muted);
            BuildCanvas();
            EnsureEventSystem();
            var worldGo = new GameObject("World");
            World = worldGo.AddComponent<WorldView>();
            World.gm = this;
            ShowLoading();
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            string error = null;
            yield return DataLoader.LoadAll((d, e) => { Data = d; error = e; });
            if (error != null) Debug.LogWarning("Data: " + error);
            LiveSun.FromArchive(Data);
            ShowTitle();
            yield return LiveSun.Fetch(Data);
            if (phase == Phase.Title) ShowTitle();
        }

        void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            screenLayer = UIKit.Stretch(UIKit.Rect("Screens", go.transform));
            hudLayer = UIKit.Stretch(UIKit.Rect("HUD", go.transform));
            modalLayer = UIKit.Stretch(UIKit.Rect("Modals", go.transform));
            toastLayer = UIKit.Stretch(UIKit.Rect("Toasts", go.transform));
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>()) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        void Update()
        {
            // Browsers only allow audio after the first click or key.
            if (!firstInput && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
                                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)))
            {
                firstInput = true;
                AudioFx.Wind(true);
            }
            if (phase == Phase.Outpost) OutpostUpdate();
        }

        // ---------- queue of modal steps ----------

        public void Queue(Action<Action> step)
        {
            queue.Enqueue(step);
            Pump();
        }

        void Pump()
        {
            if (queueBusy || queue.Count == 0) return;
            queueBusy = true;
            var step = queue.Dequeue();
            bool finished = false;
            step(() =>
            {
                if (finished) return;
                finished = true;
                queueBusy = false;
                Pump();
            });
        }

        void ClearQueue()
        {
            queue.Clear();
            queueBusy = false;
            UIKit.Clear(modalLayer);
        }

        // A centred card on a dimmed backdrop. Close with CloseModal(card).
        public RectTransform Modal(float width, Color? color = null, bool dark = false)
        {
            var dim = UIKit.Fill(modalLayer, new Color(0.08f, 0.04f, 0.1f, 0.62f), "Modal");
            UIKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var card = UIKit.Panel(dim.transform, color ?? (dark ? Theme.Night : Theme.Paper), "Card");
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 100);
            UIKit.VStack(card.gameObject, 12, 24);
            UIKit.Fit(card.gameObject);
            return rt;
        }

        public void CloseModal(RectTransform card)
        {
            if (card) Destroy(card.parent.gameObject);
        }

        public void Toast(string text, Color? color = null)
        {
            var p = UIKit.Panel(toastLayer, color ?? Theme.Paper, "Toast");
            UIKit.Anchor(p.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(560, 56));
            var t = UIKit.Label(p.transform, text, 19, Theme.Ink, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform, 14, 4, 14, 4);
            StartCoroutine(FadeOut(p.gameObject, 2.6f));
        }

        IEnumerator FadeOut(GameObject go, float after)
        {
            yield return new WaitForSecondsRealtime(after);
            var cg = go.AddComponent<CanvasGroup>();
            for (float t = 0; t < 0.4f && go; t += Time.unscaledDeltaTime)
            {
                cg.alpha = 1 - t / 0.4f;
                yield return null;
            }
            if (go) Destroy(go);
        }

        void ClearScreens()
        {
            UIKit.Clear(screenLayer);
            UIKit.Clear(hudLayer);
            ClearQueue();
            hud = null;
        }

        void ShowLoading()
        {
            ClearScreens();
            var bg = UIKit.Fill(screenLayer, Theme.Mars, "Loading");
            UIKit.Stretch(bg.rectTransform);
            var t = UIKit.Label(bg.transform, "Loading Outpost Gale…", 34, Theme.Paper, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform);
        }

        // ---------- mission flow ----------

        public void StartMission(MissionSetup setup)
        {
            if (string.IsNullOrEmpty(setup.classroomCode))
                setup.classroomCode = MissionRng.NewCode(new System.Random());
            int seed = MissionRng.SeedFromCode(setup.classroomCode);
            Mission = Mission.Start(Balance, Data, Profile, setup, seed);
            SaveSystem.SaveMission(Mission.S);
            EnterOutpost(true);
        }

        public void ContinueMission()
        {
            var s = SaveSystem.LoadMission();
            if (s == null) { ShowTitle(); return; }
            Mission = new Mission(Balance, Data, s, Profile);
            EnterOutpost(false);
        }

        void EnterOutpost(bool fresh)
        {
            ClearScreens();
            phase = Phase.Outpost;
            World.gameObject.SetActive(true);
            World.Build();
            World.SetNight(false);
            BuildHud();
            AudioFx.Music("music_calm");
            if (fresh) Queue(done => ShowCodeCard(done));
            DrainTriggers();
            Queue(done => ShowBriefing(Mission.S.lastReport, done));
        }

        // Call after every change to the mission.
        public void AfterChange()
        {
            World.Refresh();
            RefreshHud();
            SaveSystem.SaveMission(Mission.S);
            SaveSystem.SaveProfile(Profile);
            DrainTriggers();
        }

        void DrainTriggers()
        {
            var triggers = Mission.TakeTriggers();
            if (triggers.Count == 0) return;
            var beats = StoryDirector.BeatsFor(Data.story, Mission.S, triggers);
            foreach (var beat in beats)
                foreach (var line in beat.lines)
                {
                    var l = line;
                    Queue(done => { if (skippingStory) done(); else ShowDialogue(l, done); });
                }
        }

        public void EndSol()
        {
            if (nightRunning || Mission.S.over) return;
            if (World.ghostModule != null) CancelBuild();
            StartCoroutine(NightSequence());
        }

        IEnumerator NightSequence()
        {
            nightRunning = true;
            bool storm = Mission.StormTonight;
            World.SetNight(true);
            AudioFx.Music(storm ? "music_tense" : "music_calm");
            yield return new WaitForSeconds(1.2f);
            if (storm)
            {
                World.stormFx = true;
                AudioFx.Siren();
                World.Shake();
                yield return new WaitForSeconds(2.2f);
                World.stormFx = false;
            }
            var report = Mission.Sleep();
            nightRunning = false;
            World.Refresh();
            RefreshHud();
            SaveSystem.SaveProfile(Profile);
            if (Mission.S.over)
            {
                SaveSystem.ClearMission();
                Queue(done => ShowNightReport(report, done));
                DrainTriggers();
                Queue(done => { ShowEnding(); done(); });
                yield break;
            }
            SaveSystem.SaveMission(Mission.S);
            Queue(done => ShowNightReport(report, done));
            Queue(done => { World.SetNight(false); AudioFx.Music("music_calm"); done(); });
            DrainTriggers();
            Queue(done => ShowBriefing(report, done));
        }

        public void QuitToTitle()
        {
            if (Mission != null && !Mission.S.over) SaveSystem.SaveMission(Mission.S);
            World.StopGhost();
            World.gameObject.SetActive(false);
            ShowTitle();
        }
    }
}

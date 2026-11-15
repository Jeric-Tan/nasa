using System;
using OutpostGale.Sim;
using OutpostGale.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OutpostGale
{
    // A short trip scene between leaving the airlock and the trip result: a real NASA photo of the
    // place (where we have one), one true fact about it, and three finds to tap. Purely a view: the haul,
    // hours and dose were already settled by Mission.StartTrip, and the push-your-luck choice comes next.
    public partial class GameManager
    {
        [Serializable]
        public class TripPlace
        {
            public string site, spot, place, photo, caption, credit, fact, action, source, retrieved_on;
        }

        [Serializable]
        class TripPlaceList { public TripPlace[] places; }

        static TripPlace[] tripPlaces;

        static TripPlace PlaceFor(string site, string spot)
        {
            if (tripPlaces == null)
            {
                var asset = Resources.Load<TextAsset>("Data/Trips");
                tripPlaces = asset ? JsonUtility.FromJson<TripPlaceList>(asset.text).places ?? new TripPlace[0] : new TripPlace[0];
            }
            return Array.Find(tripPlaces, p => p.site == site && p.spot == spot);
        }

        // The name to show for a spot: the real place at this site if we have one.
        public string SpotTitle(SpotDef spot)
        {
            var p = PlaceFor(Mission.S.site, spot.id);
            return p != null ? p.place : spot.name;
        }

        static string ActionFor(SpotDef spot) =>
            spot.material == "samples" ? "Drill" : spot.material == "parts" ? "Grab" : "Scoop";

        void ShowTripScene(SpotDef spot, Action done)
        {
            var place = PlaceFor(Mission.S.site, spot.id);
            var photo = place != null ? Theme.Art(place.photo) : null;
            string action = place?.action ?? ActionFor(spot);
            bool water = spot.material == "ice";

            var card = Modal(880, null, true);
            card.GetComponent<VerticalLayoutGroup>().spacing = 10;
            Origin(card, "TRIP OUTSIDE", "You went out through the airlock. Hours and radiation are already counted.", true);
            UIKit.Label(card, place != null ? place.place : spot.name, 30, Theme.Paper, TextAnchor.MiddleLeft, true);
            UIKit.Label(card, Balance.Site(Mission.S.site).name + "  ·  " + spot.name + (place != null ? "  ·  a real place Curiosity visited" : ""), 14, Theme.Soft);

            // ---- the photo banner (or a drawn Mars view when we have no photo of this place) ----
            var banner = UIKit.Panel(card, Theme.Hex("#2A1B2E"), "Banner", true, 16);
            UIKit.Size(banner, -1, 230, -1, 230);
            banner.gameObject.AddComponent<RectMask2D>();
            if (photo)
            {
                var pic = UIKit.Rect("Photo", banner.transform);
                var pi = pic.gameObject.AddComponent<Image>();
                pi.sprite = photo;
                pi.raycastTarget = false;
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = photo.rect.width / photo.rect.height;
                var capBg = UIKit.Fill(banner.transform, new Color(0.07f, 0.04f, 0.12f, 0.72f), "Caption");
                capBg.rectTransform.anchorMin = new Vector2(0, 0);
                capBg.rectTransform.anchorMax = new Vector2(1, 0);
                capBg.rectTransform.pivot = new Vector2(0.5f, 0);
                capBg.rectTransform.sizeDelta = new Vector2(0, 26);
                capBg.raycastTarget = false;
                var cap = UIKit.Label(capBg.transform, place.caption + "  " + place.credit, 11, Theme.Paper, TextAnchor.MiddleLeft);
                UIKit.Stretch(cap.rectTransform, 12, 0, 12, 0);
            }
            else
            {
                var sky = UIKit.Rect("Sky", banner.transform);
                UIKit.Stretch(sky);
                var si = sky.gameObject.AddComponent<Image>();
                si.sprite = Theme.Gradient(Theme.Hex("#E9925E"), Theme.Hex("#3A2448"));
                si.raycastTarget = false;
                var ground = UIKit.Rect("Ground", banner.transform);
                ground.anchorMin = Vector2.zero; ground.anchorMax = new Vector2(1, 0.4f);
                ground.offsetMin = ground.offsetMax = Vector2.zero;
                var gi = ground.gameObject.AddComponent<Image>();
                gi.color = Theme.Hex("#9A4E33");
                gi.raycastTarget = false;
                var note = UIKit.Label(banner.transform, "Drawn view: we don't have a rover photo of this spot.", 11, Theme.Paper, TextAnchor.LowerLeft);
                UIKit.Stretch(note.rectTransform, 12, 6, 12, 6);
            }

            if (place != null)
                UIKit.Label(card, "<color=#FFD27A><b>From NASA:</b></color>  " + place.fact, 15, Theme.Paper);
            else
                UIKit.Label(card, spot.blurb, 15, Theme.Paper);

            // ---- three finds to tap ----
            var field = UIKit.Panel(card, Theme.Hex("#7A3D2E"), "Field", true, 16);
            UIKit.Size(field, -1, 120, -1, 120);
            var status = UIKit.Label(card, action + " all three finds!", 15, Theme.Soft, TextAnchor.MiddleCenter);
            var row = UIKit.Rect("Buttons", card);
            UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(row, -1, 56, -1, 56);
            Button go = null;
            int left = 3;
            Action finish = () =>
            {
                status.text = water
                    ? "<color=#8FD3FF><b>Baking the soil…</b></color> the heat drives out the water trapped in it. Ready to head back."
                    : "<color=#7CE29A><b>All packed!</b></color> Ready to head back.";
                go.interactable = true;
                UIKit.SetLabel(go, "Continue");
            };
            float[] xs = { 0.2f, 0.5f, 0.8f };
            var rnd = new System.Random(Mission.S.seed + Mission.S.sol);
            for (int i = 0; i < 3; i++)
            {
                var find = UIKit.Button(field.transform, action, null, spot.material == "samples" ? Theme.Hex("#C9B8A8") : spot.material == "parts" ? Theme.Hex("#C9D2E6") : Theme.Hex("#E2A274"), 15);
                var rt = (RectTransform)find.transform;
                float y = 0.38f + (float)rnd.NextDouble() * 0.24f;
                rt.anchorMin = rt.anchorMax = new Vector2(xs[i], y);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(104, 52);
                find.gameObject.AddComponent<Pulse>().speed = 2f + i * 0.4f;
                find.onClick.AddListener(() =>
                {
                    AudioFx.Click();
                    find.gameObject.SetActive(false);
                    left--;
                    status.text = left > 0 ? "Got it! " + left + " to go." : status.text;
                    if (left == 0) finish();
                });
            }
            go = UIKit.Button(row, "Collect the finds first", () => { CloseModal(card); done(); }, Theme.Power, 17);
            go.interactable = false;
            UIKit.Button(row, "Skip", () => { CloseModal(card); done(); }, Theme.NightSoft, 14);
            UIKit.Size(row.GetChild(1).GetComponent<Button>(), 90, 52, 100, 56);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OutpostGale.UI
{
    // Small helpers for building uGUI from code (big buttons, readable text, rounded panels).
    public static class UIKit
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", bool outlined = true, int radius = 18)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Rounded(radius, 4, outlined);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            return img;
        }

        public static Image Fill(Transform parent, Color color, string name = "Fill")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.White();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 20, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, bool display = false, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? Theme.Display : Theme.Body;
            t.fontSize = size;
            t.color = color ?? Theme.Ink;
            t.alignment = align;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color? fill = null, int fontSize = 20, string name = "Button")
        {
            var img = Panel(parent, fill ?? Color.white, name, true, 14);
            var btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(1, 1, 1, 0.45f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => { AudioFx.Click(); onClick(); });
            var label = Label(img.transform, text, fontSize, Theme.Ink, TextAnchor.MiddleCenter, false, "Label");
            label.font = Theme.BodyBlack;
            Stretch(label.rectTransform, 10, 4, 10, 4);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 48;
            le.preferredHeight = 52;
            return btn;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t) t.text = text;
        }

        public static VerticalLayoutGroup VStack(GameObject go, float spacing = 8, int pad = 0, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(GameObject go, float spacing = 8, int pad = 0, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static ContentSizeFitter Fit(GameObject go, bool vertical = true, bool horizontal = false)
        {
            var f = go.AddComponent<ContentSizeFitter>();
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        public static LayoutElement Size(Component c, float minW = -1, float minH = -1, float prefW = -1, float prefH = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (!le) le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            return le;
        }

        // Scrollable vertical list; returns the content transform to add rows to.
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var view = Rect("Scroll", parent);
            scroll = view.gameObject.AddComponent<ScrollRect>();
            var mask = view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            VStack(content.gameObject, 10, 4);
            Fit(content.gameObject);
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    // A button you hold down (or hold Space) to fill a ring: the default "hold to fix" task.
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float seconds = 1.6f;
        public Image ring;
        public Action onDone;
        bool held;
        float t;
        bool done;

        public void OnPointerDown(PointerEventData e) => held = true;
        public void OnPointerUp(PointerEventData e) => held = false;
        public void OnPointerExit(PointerEventData e) => held = false;

        void Update()
        {
            if (done) return;
            bool key = UnityEngine.InputSystem.Keyboard.current != null &&
                       (UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed || UnityEngine.InputSystem.Keyboard.current.eKey.isPressed);
            t = held || key ? t + Time.unscaledDeltaTime : Mathf.Max(0, t - Time.unscaledDeltaTime * 2);
            if (ring) ring.fillAmount = Mathf.Clamp01(t / seconds);
            if (t >= seconds)
            {
                done = true;
                onDone?.Invoke();
            }
        }
    }
}

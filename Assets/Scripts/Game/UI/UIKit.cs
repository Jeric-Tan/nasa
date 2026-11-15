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

        // A rounded card. Outlined panels get a faint rim (light on dark, dark on light) and a soft drop shadow.
        public static Image Panel(Transform parent, Color color, string name = "Panel", bool outlined = true, int radius = 18)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Rounded(radius + 4, 0, false);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            if (outlined)
            {
                bool dark = Theme.Luma(color) < 0.5f;
                var rim = rt.gameObject.AddComponent<Outline>();
                rim.effectColor = dark ? new Color(1f, 1f, 1f, 0.13f) : new Color(0.14f, 0.08f, 0.2f, 0.09f);
                rim.effectDistance = new Vector2(1.5f, -1.5f);
                rim.useGraphicAlpha = false;
                var drop = rt.gameObject.AddComponent<Shadow>();
                drop.effectColor = new Color(0.06f, 0.02f, 0.1f, dark ? 0.35f : 0.18f);
                drop.effectDistance = new Vector2(0, -6);
                drop.useGraphicAlpha = false;
            }
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

        // A chunky, glossy "key" button with a 3D lip underneath: it lifts on hover and presses down on click.
        // The text colour follows the fill (white on dark keys, ink on light ones).
        public static Button Button(Transform parent, string text, Action onClick, Color? fill = null, int fontSize = 20, string name = "Button")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Key(16);
            img.type = Image.Type.Sliced;
            img.color = fill ?? Color.white;
            rt.gameObject.AddComponent<Lip>();
            var btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.98f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.88f, 0.88f, 0.9f);
            colors.disabledColor = new Color(0.8f, 0.78f, 0.84f, 0.5f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => { AudioFx.Click(); onClick(); });
            var label = Label(img.transform, text, fontSize, Theme.TextOn(img.color), TextAnchor.MiddleCenter, false, "Label");
            label.font = Theme.BodyBlack;
            Stretch(label.rectTransform, 12, 4, 12, 4);
            rt.gameObject.AddComponent<Juice>();
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 48;
            le.preferredHeight = 52;
            return btn;
        }

        // Recolour a button and keep its text readable.
        public static void Tint(Button b, Color fill)
        {
            if (!b) return;
            b.image.color = fill;
            var t = b.transform.Find("Label");
            if (t) t.GetComponent<Text>().color = Theme.TextOn(fill);
        }

        // A small icon (Resources/Art/<name>) on the left of a button; no-op when the art is missing.
        public static void Icon(Button b, string artName, Color? tint = null)
        {
            var sprite = Theme.Art(artName);
            if (sprite == null) return;
            var rt = Rect("Icon", b.transform);
            Anchor(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 1), new Vector2(22, 22));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            var label = b.transform.Find("Label")?.GetComponent<Text>();
            img.color = tint ?? (label ? label.color : Theme.Ink);
            img.raycastTarget = false;
            if (label) label.rectTransform.offsetMin = new Vector2(38, label.rectTransform.offsetMin.y);
        }

        // A round badge in a button's top-left corner (keyboard shortcut hint).
        public static void KeyBadge(Button b, string key)
        {
            var rt = Rect("Key", b.transform);
            Anchor(rt, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(12, -6), new Vector2(24, 24));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Circle(64);
            img.color = Theme.Night;
            img.raycastTarget = false;
            var t = Label(rt, key, 13, Theme.Gold, TextAnchor.MiddleCenter);
            t.font = Theme.BodyBlack;
            Stretch(t.rectTransform);
        }

        // Pop a card in with a little overshoot.
        public static void PopIn(Component c) => c.gameObject.AddComponent<PopIn>();

        public static void SetLabel(Button b, string text)
        {
            var t = b.transform.Find("Label")?.GetComponent<Text>();
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

    // Draws a darker copy of the graphic a few pixels lower: the side of a pressable key.
    // It uses the graphic's own colour, so recoloured buttons keep a matching lip.
    public class Lip : BaseMeshEffect
    {
        public float depth = 5f;
        public float pressed;
        readonly System.Collections.Generic.List<UIVertex> verts = new System.Collections.Generic.List<UIVertex>();
        readonly System.Collections.Generic.List<UIVertex> output = new System.Collections.Generic.List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            verts.Clear();
            output.Clear();
            vh.GetUIVertexStream(verts);
            int n = verts.Count;
            for (int i = 0; i < n; i++)
            {
                var v = verts[i];
                v.position.y -= depth;
                Color32 c = v.color;
                v.color = new Color32((byte)(c.r * 0.66f), (byte)(c.g * 0.6f), (byte)(c.b * 0.72f), c.a);
                output.Add(v);
            }
            for (int i = 0; i < n; i++)
            {
                var v = verts[i];
                v.position.y -= depth * pressed;
                output.Add(v);
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }

        public void SetPressed(float p)
        {
            if (Mathf.Abs(p - pressed) < 0.001f) return;
            pressed = p;
            if (graphic) graphic.SetVerticesDirty();
        }
    }

    // Hover lift and click squish for buttons.
    public class Juice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        bool over, down;
        float scale = 1, press;
        Selectable sel;
        Lip lip;
        RectTransform label;
        float baseBottom, baseTop;

        void Awake()
        {
            sel = GetComponent<Selectable>();
            lip = GetComponent<Lip>();
        }

        void Start()
        {
            var t = transform.Find("Label");
            if (t) { label = (RectTransform)t; baseBottom = label.offsetMin.y; baseTop = label.offsetMax.y; }
        }

        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) { over = false; down = false; }
        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;

        void OnDisable()
        {
            over = down = false;
            scale = 1;
            press = 0;
            transform.localScale = Vector3.one;
            if (lip) lip.SetPressed(0);
        }

        void Update()
        {
            bool live = sel == null || sel.IsInteractable();
            var gm = OutpostGale.GameManager.I;
            bool still = gm != null && gm.Profile != null && gm.Profile.reducedMotion;
            float targetScale = live && over && !down && !still ? 1.035f : 1f;
            float targetPress = live && down ? 1f : 0f;
            scale = Mathf.Lerp(scale, targetScale, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            press = Mathf.Lerp(press, targetPress, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 30f));
            if (Mathf.Abs(scale - 1) > 0.0005f || transform.localScale.x != 1) transform.localScale = Vector3.one * scale;
            if (lip) lip.SetPressed(press);
            if (label && lip)
            {
                float shift = -lip.depth * press;
                label.offsetMin = new Vector2(label.offsetMin.x, baseBottom + shift);
                label.offsetMax = new Vector2(label.offsetMax.x, baseTop + shift);
            }
        }
    }

    // Cards pop in: fade up from slightly small with a soft overshoot.
    public class PopIn : MonoBehaviour
    {
        float t;
        CanvasGroup group;

        void Awake()
        {
            group = gameObject.GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
            Apply(0);
        }

        void Update()
        {
            var gm = OutpostGale.GameManager.I;
            bool still = gm != null && gm.Profile != null && gm.Profile.reducedMotion;
            t += Time.unscaledDeltaTime / (still ? 0.01f : 0.28f);
            Apply(Mathf.Clamp01(t));
            if (t >= 1) Destroy(this);
        }

        void Apply(float x)
        {
            // easeOutBack
            const float c1 = 1.5f, c3 = c1 + 1;
            float e = 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, e);
            if (group) group.alpha = Mathf.Clamp01(x * 2.5f);
        }
    }

    // Gentle twinkle for stars on the title screen.
    public class Twinkle : MonoBehaviour
    {
        public float speed = 1, phase;
        Image img;
        float baseAlpha;

        void Start()
        {
            img = GetComponent<Image>();
            baseAlpha = img ? img.color.a : 1;
        }

        void Update()
        {
            if (!img) return;
            var c = img.color;
            c.a = baseAlpha * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * speed + phase));
            img.color = c;
        }
    }

    // A slow pulse (the LIVE dot, beacon lights).
    public class Pulse : MonoBehaviour
    {
        public float speed = 3;
        void Update() => transform.localScale = Vector3.one * (0.85f + 0.2f * Mathf.Sin(Time.unscaledTime * speed));
    }
}

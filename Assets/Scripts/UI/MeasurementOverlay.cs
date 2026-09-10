using CupPrototype.DrinkSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CupPrototype.UI
{
    public sealed class MeasurementOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private GameObject view;
        private DrinkContainer activeJigger;
        private readonly Image[] liquid = new Image[40];
        private readonly TextMeshProUGUI[] marks = new TextMeshProUGUI[5];
        private TextMeshProUGUI ingredientLabel;
        public bool IsVisible => view && view.activeSelf;
        public float DisplayedFraction { get; private set; }

        public void Show(DrinkContainer jigger, IngredientData ingredient)
        {
            Hide();
            if (!view) Build();
            activeJigger = jigger;
            activeJigger.StateChanged += Refresh;
            for (int i = 0; i < marks.Length; i++) marks[i].text = $"{jigger.MaxVolume * i / 4f:0.#}";
            ingredientLabel.text = ingredient.ingredientName.ToUpperInvariant();
            view.SetActive(true);
            Refresh(jigger);
        }
        public void Hide()
        {
            if (activeJigger) activeJigger.StateChanged -= Refresh;
            activeJigger = null;
            if (view) view.SetActive(false);
        }
        private void Refresh(DrinkContainer jigger)
        {
            DisplayedFraction = jigger.MaxVolume > 0 ? Mathf.Clamp01(jigger.CurrentVolume / jigger.MaxVolume) : 0;
            for (int i = 0; i < liquid.Length; i++)
            {
                liquid[i].enabled = (i + .5f) / liquid.Length <= DisplayedFraction;
                var color = jigger.CurrentColor;
                color.a = .95f;
                liquid[i].color = color;
            }
        }
        private RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private Image Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private TextMeshProUGUI Label(Transform parent, Vector2 size, Vector2 position, string text)
        {
            var label = Rect("Label", parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font ? font : TMP_Settings.defaultFontAsset;
            label.text = text; label.fontSize = 24; label.color = new Color(.78f, .94f, .92f);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private void Build()
        {
            view = new GameObject("Measurement View", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            view.transform.SetParent(transform, false);
            var canvas = view.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = view.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var scrim = Box("Scrim", view.transform, Vector2.zero, Vector2.zero, new Color(.01f,.025f,.03f,.42f)).rectTransform;
            scrim.anchorMin = Vector2.zero; scrim.anchorMax = Vector2.one; scrim.offsetMin = scrim.offsetMax = Vector2.zero;
            var body = Rect("Enlarged Jigger", view.transform, new Vector2(340,540), new Vector2(0,-100));
            // Stepped metal silhouette; the upper cup is a cutaway with volume-calibrated bands.
            for (int i = 0; i < 60; i++)
            {
                float y = i * 9 - 265.5f;
                float width = y >= -25 ? Mathf.Lerp(94,300,(y+25)/295) : Mathf.Lerp(94,220,(-y-25)/245);
                Box("Metal", body, new Vector2(width,9), new Vector2(0,y), new Color(.3f,.43f,.46f));
            }
            for (int i = 0; i < liquid.Length; i++)
            {
                float y = -15 + i * 6.75f;
                float width = Mathf.Lerp(78,276,i/39f);
                Box("Interior",body,new Vector2(width,6.75f),new Vector2(0,y),new Color(.025f,.06f,.07f));
                liquid[i] = Box("Liquid",body,new Vector2(width,6.75f),new Vector2(0,y),Color.clear);
            }
            for(int i=0;i<5;i++)
            {
                float y=-18.375f+270*i/4f;
                Box("Mark",body,new Vector2(36,3),new Vector2(90,y),new Color(.8f,1,1));
                marks[i]=Label(body,new Vector2(90,32),new Vector2(190,y),"");
            }
            Label(body,new Vector2(90,32),new Vector2(190,287),"ml");
            ingredientLabel = Label(view.transform,new Vector2(800,40),new Vector2(0,210),"");
            Label(view.transform,new Vector2(800,40),new Vector2(0,-420),"HOLD LMB TO POUR  /  RELEASE TO STOP");
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if(view) Destroy(view); }
    }
}

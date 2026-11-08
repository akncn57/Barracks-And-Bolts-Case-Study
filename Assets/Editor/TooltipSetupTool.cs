using System;
using BarracksAndBolts.UI.Tooltips;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.EditorTools
{
    /// <summary>
    /// One-time tool: adds the tooltip to the gameplay canvas. Delete this file once the tooltip is done.
    /// Safe to run more than once; it replaces the tooltip it made before.
    /// </summary>
    public static class TooltipSetupTool
    {
        private const string CanvasPath = "Assets/Prefabs/UI/Canvases/Prefab_UI_Canvas_Gameplay_Variant.prefab";
        private const string PaperPath = "Assets/Textures/UI/Papers/Texture_UI_Papers_Regular.png";
        private const string FontPath = "Assets/Materials/Font/MedievalSharp-Regular SDF.asset";
        private const string TooltipName = "Tooltip";

        [MenuItem("Tools/Barracks & Bolts/Add Tooltip")]
        private static void Run()
        {
            var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
            try
            {
                Build(FindDeep(canvas.transform, "DynamicCanvas"));
                PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canvas);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Tooltip is ready. You can delete Assets/Editor/TooltipSetupTool.cs now.");
        }

        private static void Build(Transform parent)
        {
            var old = parent.Find(TooltipName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var root = new GameObject(TooltipName, typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Last child, so it is drawn above the menu and the panel.
            rootRect.SetAsLastSibling();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(rootRect, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = Vector2.zero;
            panelRect.sizeDelta = new Vector2(340f, 160f);

            var image = panel.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.raycastTarget = false;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 24);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // The tooltip sits next to the mouse and must never catch it, or it would flicker.
            var group = panel.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var title = CreateText("Text_Title", panelRect, font, 30f);
            var body = CreateText("Text_Body", panelRect, font, 22f);

            var view = root.AddComponent<TooltipView>();
            SetReference(view, "panel", panelRect);
            SetReference(view, "titleLabel", title);
            SetReference(view, "bodyLabel", body);
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;

            text.fontSize = size;
            text.color = Color.black;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = name;
            return text;
        }

        private static void SetReference(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException($"{owner.GetType().Name} has no field {field}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            throw new InvalidOperationException($"{parent.name} has no child {name}.");
        }
    }
}

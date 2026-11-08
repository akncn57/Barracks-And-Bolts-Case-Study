using System;
using BarracksAndBolts.UI.Common;
using BarracksAndBolts.UI.Information;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.EditorTools
{
    /// <summary>
    /// One-time tool: finishes the information panel prefab setup. Delete this file once the panel is done.
    /// Safe to run more than once.
    /// </summary>
    public static class InformationPanelSetupTool
    {
        private const string PanelPath = "Assets/Prefabs/UI/Prefab_UI_Information_Panel.prefab";
        private const string CanvasPath = "Assets/Prefabs/UI/Canvases/Prefab_UI_Canvas_Gameplay_Variant.prefab";
        private const string HandleSpritePath = "Assets/Textures/UI/Buttons/Texture_UI_Buttons_Tiny_Blue_Square.png";
        private const string PanelName = "Prefab_UI_Information_Panel";

        private const float PanelWidth = 480f;
        private const float EdgeMargin = 25f;
        private const float PaperMargin = 30f;

        [MenuItem("Tools/Barracks & Bolts/Finish Information Panel")]
        private static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                SetupPanel(root);
                PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
            try
            {
                // The canvas keeps its own anchor values for the panel, so they are set here too.
                AnchorTopRight((RectTransform)FindDeep(canvas.transform, PanelName));
                PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canvas);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Information panel is ready. You can delete Assets/Editor/InformationPanelSetupTool.cs now.");
        }

        private static void SetupPanel(GameObject root)
        {
            var rootTransform = root.transform;
            var scrollArea = Find(rootTransform, "Content/Scroll View");
            var viewport = Find(scrollArea, "Viewport");
            var scrollContent = Find(viewport, "Content");
            var icon = Find(scrollContent, "Image_Icon");
            var nameText = Find(scrollContent, "Text_Name");
            var hpBar = Find(scrollContent, "HP_Bar");
            var production = Find(scrollContent, "Production");
            var title = Find(production, "Text_Title");
            var list = Find(production, "List");
            var scrollbar = Find(scrollArea, "Scrollbar Vertical");

            AnchorTopRight((RectTransform)rootTransform);
            Stretch((RectTransform)scrollArea, PaperMargin);

            SetupViewport(viewport);
            SetupScrollContent((RectTransform)scrollContent);

            var iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetPreferredHeight(icon, 200f);

            var nameLabel = nameText.GetComponent<TMP_Text>();
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.raycastTarget = false;

            SetupHpBar(hpBar);
            SetupProduction(production, title, list);
            SetupScrollbar(scrollbar);

            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.viewport = (RectTransform)viewport;
            scrollRect.content = (RectTransform)scrollContent;
            scrollRect.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 6f;

            var fit = root.GetComponent<ContentFitPanel>();
            if (fit == null)
                fit = root.AddComponent<ContentFitPanel>();

            SetReference(fit, "viewport", viewport);
            SetReference(fit, "content", scrollContent);

            var view = root.GetComponent<InformationPanelView>();
            SetReference(view, "panelFit", fit);
            SetReference(view, "scrollRect", scrollRect);
            SetReference(view, "picture", iconImage);
            SetReference(view, "nameLabel", nameLabel);
            SetReference(view, "hpLabel", Find(hpBar, "Text_HP").GetComponent<TMP_Text>());
            SetReference(view, "hpFill", Find(hpBar, "Image_Fill").GetComponent<Image>());
            SetReference(view, "productionSection", production.gameObject);
            SetReference(view, "producibleContainer", list);
        }

        private static void SetupViewport(Transform viewport)
        {
            // RectMask2D clips without the extra draw calls of a stencil Mask.
            var mask = viewport.GetComponent<Mask>();
            if (mask != null)
                Object.DestroyImmediate(mask);

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            // Invisible, but it still catches drags that start between the items.
            var image = viewport.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        private static void SetupScrollContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            SetupVerticalLayout(content.gameObject, 16f);

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void SetupHpBar(Transform hpBar)
        {
            SetPreferredHeight(hpBar, 44f);

            var fill = (RectTransform)Find(hpBar, "Image_Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(10f, 11f);
            fill.offsetMax = new Vector2(-10f, -11f);
            fill.GetComponent<Image>().raycastTarget = false;

            var text = (RectTransform)Find(hpBar, "Text_HP");
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = Vector2.zero;
            text.offsetMax = Vector2.zero;

            var label = text.GetComponent<TMP_Text>();
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private static void SetupProduction(Transform production, Transform title, Transform list)
        {
            SetupVerticalLayout(production.gameObject, 10f);

            var titleLabel = title.GetComponent<TMP_Text>();
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.raycastTarget = false;

            // The view creates the cells; leftovers from the editor would show up as extra units.
            for (var i = list.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(list.GetChild(i).gameObject);

            // Three cells have to fit next to the scrollbar too.
            var grid = list.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.UpperCenter;
        }

        private static void SetupScrollbar(Transform scrollbar)
        {
            var rect = (RectTransform)scrollbar;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(20f, 0f);

            var track = scrollbar.GetComponent<Image>();
            track.sprite = null;
            track.color = new Color(0f, 0f, 0f, 0.25f);

            var handleImage = Find(scrollbar, "Sliding Area/Handle").GetComponent<Image>();
            handleImage.sprite = LoadSprite(HandleSpritePath);
            handleImage.type = handleImage.sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            handleImage.color = Color.white;

            scrollbar.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
        }

        private static void SetupVerticalLayout(GameObject target, float spacing)
        {
            var layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = target.AddComponent<VerticalLayoutGroup>();

            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void SetPreferredHeight(Transform target, float height)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
                element = target.gameObject.AddComponent<LayoutElement>();

            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
        }

        private static void AnchorTopRight(RectTransform rect)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-EdgeMargin, -EdgeMargin);
            rect.sizeDelta = new Vector2(PanelWidth, 600f);
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetReference(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException($"{owner.GetType().Name} has no field {field}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                    return sprite;
            }

            throw new InvalidOperationException($"No sprite at {path}.");
        }

        private static Transform Find(Transform parent, string path)
        {
            return parent.Find(path) ?? throw new InvalidOperationException($"{parent.name} has no child {path}.");
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

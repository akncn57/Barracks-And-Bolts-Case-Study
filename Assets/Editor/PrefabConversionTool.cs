using System;
using BarracksAndBolts.Gameplay.Bootstrap;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI;
using BarracksAndBolts.UI.Feedback;
using BarracksAndBolts.UI.Hud;
using BarracksAndBolts.UI.Information;
using BarracksAndBolts.UI.MainMenu;
using BarracksAndBolts.UI.Sandbox;
using BarracksAndBolts.UI.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.EditorTools
{
    /// <summary>One-time tool: turns the plain objects of the canvases and scenes into named prefabs.</summary>
    public static class PrefabConversionTool
    {
        private const string GameplayCanvas = "Assets/Prefabs/UI/Canvases/Prefab_UI_Canvas_Gameplay_Variant.prefab";
        private const string MainMenuCanvas = "Assets/Prefabs/UI/Canvases/Prefab_UI_Canvas_MainMenu_Variant.prefab";
        private const string GameplayScene = "Assets/Scenes/Gameplay.unity";
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

        private const string Ui = "Assets/Prefabs/UI/";
        private const string Core = "Assets/Prefabs/Core/";
        private const string World = "Assets/Prefabs/Gameplay/";
        private const string CommonButton = Ui + "Prefab_UI_Common_Button.prefab";
        private const string ResourceEntry = Ui + "Prefab_UI_Resource_BarEntry.prefab";

        [MenuItem("Tools/Barracks & Bolts/Convert Objects To Prefabs")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play mode first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var openScene = SceneManager.GetActiveScene().path;
            EnsureFolder(World, "Placement");
            EnsureFolder(World, "Selection");
            EnsureFolder(World, "Commands");
            EnsureFolder(World, "Hover");

            EditCanvas(GameplayCanvas, ConvertGameplayCanvas);
            EditCanvas(MainMenuCanvas, ConvertMainMenuCanvas);
            ConvertGameplayScene();
            ConvertMainMenuScene();

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(openScene))
                EditorSceneManager.OpenScene(openScene);

            Debug.Log("Objects converted to prefabs.");
        }

        private static void ConvertGameplayCanvas(GameObject root)
        {
            var button = MakeShared(root, "Button_PlayAgain", CommonButton);
            Reuse(root, "Button_Menu", button);

            var entry = MakeShared(root, "Entry_Food", ResourceEntry);
            Reuse(root, "Entry_Wood", entry);
            Reuse(root, "Entry_Stone", entry);
            Reuse(root, "Entry_Gold", entry);
            Reuse(root, "Entry_Population", entry);

            MakePrefab(root.GetComponentInChildren<HoverLabelView>(true), Ui + "Prefab_UI_Hover_Label.prefab");
            MakePrefab(root.GetComponentInChildren<UnitSelectionCardView>(true), Ui + "Prefab_UI_Unit_SelectionCard.prefab");
            MakePrefab(root.GetComponentInChildren<TooltipView>(true), Ui + "Prefab_UI_Tooltip_Panel.prefab");
            MakePrefab(root.GetComponentInChildren<DayNightOverlayView>(true), Ui + "Prefab_UI_DayNight_Overlay.prefab");
            MakePrefab(root.GetComponentInChildren<ResourceBarView>(true), Ui + "Prefab_UI_Resource_Bar.prefab");
            MakePrefab(root.GetComponentInChildren<SandboxFactionToggleView>(true), Ui + "Prefab_UI_Sandbox_SideButton.prefab");
            MakePrefab(root.GetComponentInChildren<GameEndView>(true), Ui + "Prefab_UI_GameEnd_Screen.prefab");
            MakePrefab(root.GetComponentInChildren<NoticeBannerView>(true), Ui + "Prefab_UI_Notice_Banner.prefab");
        }

        private static void ConvertMainMenuCanvas(GameObject root)
        {
            var button = AssetDatabase.LoadAssetAtPath<GameObject>(CommonButton);
            Reuse(root, "Button_Play", button);
            Reuse(root, "Button_Sandbox", button);
            Reuse(root, "Button_Quit", button);
            MakePrefab(FindByName(root, "Panel_MainMenu").transform, Ui + "Prefab_UI_MainMenu_Panel.prefab");
        }

        private static void ConvertGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScene);

            MakePrefab(FindInScene<PlacementPreviewView>(), World + "Placement/Prefab_Gameplay_Placement_Preview.prefab");
            MakePrefab(FindInScene<BoxSelectionView>(), World + "Selection/Prefab_Gameplay_Selection_Box.prefab");
            MakePrefab(FindInScene<MoveMarkerView>(), World + "Commands/Prefab_Gameplay_Move_Marker.prefab");
            MakePrefab(FindInScene<HoverHighlightView>(), World + "Hover/Prefab_Gameplay_Hover_Highlight.prefab");
            MakePrefab(FindInScene<BuildingViewSpawner>(), World + "Buildings/Prefab_Gameplay_Building_Spawner.prefab");
            MakePrefab(FindInScene<UnitViewSpawner>(), World + "Units/Prefab_Gameplay_Unit_Spawner.prefab");
            MakePrefab(FindInScene<ResourceNodeViewSpawner>(), World + "ResourceNodes/Prefab_Gameplay_ResourceNode_Spawner.prefab");
            MakePrefab(FindInScene<ProjectileSpawner>(), World + "Combat/Prefab_Gameplay_Projectile_Spawner.prefab");
            MakePrefab(FindInScene<FloatingTextSpawner>(), Ui + "Prefab_UI_FloatingText_Spawner.prefab");
            MakePrefab(FindInScene<GameplayLifetimeScope>(), Core + "Prefab_Core_GameplayLifetimeScope.prefab");

            // Overrides of the scene that pointed at the old plain objects mean nothing now.
            var canvas = PrefabUtility.GetOutermostPrefabInstanceRoot(FindInScene<GameplayUiLifetimeScope>());
            PrefabUtility.RemoveUnusedOverrides(new[] { canvas }, InteractionMode.AutomatedAction);

            EditorSceneManager.SaveScene(scene);
        }

        private static void ConvertMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScene);
            MakePrefab(FindInScene<MainMenuLifetimeScope>(), Core + "Prefab_Core_MainMenuLifetimeScope.prefab");
            EditorSceneManager.SaveScene(scene);
        }

        private static void EditCanvas(string path, Action<GameObject> convert)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var before = CountReferences(root);
                convert(root);

                var after = CountReferences(root);
                if (before != after)
                    throw new InvalidOperationException($"{path}: references went from {before} to {after}. Nothing saved in this canvas.");

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void MakePrefab(Component component, string path)
        {
            var target = component.gameObject;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))
                return;

            target.name = System.IO.Path.GetFileNameWithoutExtension(path);
            PrefabUtility.SaveAsPrefabAssetAndConnect(target, path, InteractionMode.AutomatedAction);
        }

        /// <summary>The first of several alike objects becomes the prefab. It keeps its own name in the hierarchy.</summary>
        private static GameObject MakeShared(GameObject root, string objectName, string path)
        {
            var target = FindByName(root, objectName);
            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(target, path, InteractionMode.AutomatedAction);
            target.name = objectName;
            return prefab;
        }

        /// <summary>Connects an alike object to the prefab. Whatever differs, like its icon, stays as an override.</summary>
        private static void Reuse(GameObject root, string objectName, GameObject prefab)
        {
            var target = FindByName(root, objectName);
            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))
                return;

            var settings = new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                recordPropertyOverridesOfMatches = true,
                componentsNotMatchedBecomesOverride = true,
                gameObjectsNotMatchedBecomesOverride = true,
                changeRootNameToAssetName = false
            };
            PrefabUtility.ConvertToPrefabInstance(target, prefab, settings, InteractionMode.AutomatedAction);
        }

        private static GameObject FindByName(GameObject root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }

            throw new InvalidOperationException($"No object named {objectName}.");
        }

        private static T FindInScene<T>() where T : Component
        {
            var found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found == null)
                throw new InvalidOperationException($"No {typeof(T).Name} in {SceneManager.GetActiveScene().name}.");

            return found;
        }

        private static int CountReferences(GameObject root)
        {
            var count = 0;
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;

                var property = new SerializedObject(behaviour).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                        count++;
                }
            }

            return count;
        }

        private static void EnsureFolder(string parent, string name)
        {
            var trimmed = parent.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder($"{trimmed}/{name}"))
                AssetDatabase.CreateFolder(trimmed, name);
        }
    }
}

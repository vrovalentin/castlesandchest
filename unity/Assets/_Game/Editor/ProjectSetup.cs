using Game.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds the scenes and player settings from code, so they can be recreated at any time
    /// (menu Castles, or batchmode: -executeMethod Game.EditorTools.ProjectSetup.Run).
    /// </summary>
    public static class ProjectSetup
    {
        public const string MainScene = "Assets/_Game/Scenes/Main.unity";
        private const string OldMineScene = "Assets/_Game/Scenes/Mine.unity";

        [MenuItem("Castles/Setup project (scene + player settings)")]
        public static void Run()
        {
            if (!System.IO.Directory.Exists("Assets/_Game/Scenes")) AssetDatabase.CreateFolder("Assets/_Game", "Scenes");
            SetupPlayer();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] done");
        }

        /// <summary>The game scene: a camera and the AppController (build screen + mine).</summary>
        [MenuItem("Castles/Create Main scene")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("App").AddComponent<AppController>();

            EditorSceneManager.SaveScene(scene, MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            // The first version had a mine-only scene; the main scene replaces it.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldMineScene) != null) AssetDatabase.DeleteAsset(OldMineScene);
        }

        /// <summary>Portrait phone game; Yandex Games page 540×960.</summary>
        public static void SetupPlayer()
        {
            PlayerSettings.companyName = "vrovalentin";
            PlayerSettings.productName = "Castles and Chest";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            // No «Made with Unity» screen (allowed on Personal since Unity 6): the game starts 2-3 s sooner.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // A smaller download: unused code is cut harder and IL2CPP compiles for size. Our own assemblies are kept
            // whole (Assets/link.xml), so nothing the game reaches only by reflection or JsonUtility goes missing.
            foreach (var t in new[] { UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.NamedBuildTarget.Android })
            {
                PlayerSettings.SetManagedStrippingLevel(t, ManagedStrippingLevel.High);
            // Compiling for size is for the web download only: on Android a full IL2CPP rebuild for size needs more
            // memory than the build PC has, and the APK size matters less (Google Play).
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed);
            PlayerSettings.stripEngineCode = true;
        }
    }
}

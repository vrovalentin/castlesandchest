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
        public const string MineScene = "Assets/_Game/Scenes/Mine.unity";

        [MenuItem("Castles/Setup project (scene + player settings)")]
        public static void Run()
        {
            SetupPlayer();
            CreateMineScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] done");
        }

        [MenuItem("Castles/Create Mine scene")]
        public static void CreateMineScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("Mine").AddComponent<MineController>();

            EditorSceneManager.SaveScene(scene, MineScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MineScene, true) };
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
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Test APK for a phone (menu Castles, or batchmode:
    /// -executeMethod Game.EditorTools.BuildAndroid.Run). Signed with Unity's debug key,
    /// so it installs from a file but is not for Google Play (that needs a release key and an AAB).
    /// Output: Builds/castles.apk next to the Unity project.
    /// </summary>
    public static class BuildAndroid
    {
        public const string ApkPath = "../Builds/castles.apk";
        public const string PackageId = "com.vrovalentin.castlesandchest";

        [MenuItem("Castles/Build Android test APK")]
        public static void Run()
        {
            ProjectSetup.SetupPlayer();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScene },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[BuildAndroid] {s.result}: {s.totalSize / 1024 / 1024} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s, {Path.GetFullPath(ApkPath)}");
            if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}

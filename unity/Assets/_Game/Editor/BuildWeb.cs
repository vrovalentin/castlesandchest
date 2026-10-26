using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// WebGL build for phones and Yandex Games (menu Castles, or batchmode:
    /// -executeMethod Game.EditorTools.BuildWeb.Run). Output goes to Builds/WebGL next to the Unity
    /// project, plus Builds/castles-webgl.zip ready to upload as a Yandex Games draft.
    /// </summary>
    public static class BuildWeb
    {
        public const string OutDir = "../Builds/WebGL";
        public const string ZipPath = "../Builds/castles-webgl.zip";

        [MenuItem("Castles/Build WebGL (phone + Yandex)")]
        public static void Run()
        {
            ProjectSetup.SetupPlayer();
            PlayerSettings.WebGL.template = "PROJECT:Fullscreen";
            // Gzip with the JS fallback works on any static server, a phone over Wi-Fi included, and on Yandex.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.runInBackground = false;

            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScene },
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[BuildWeb] {s.result}: {s.totalSize / 1024 / 1024} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            if (s.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            ZipFile.CreateFromDirectory(OutDir, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);
            Debug.Log($"[BuildWeb] zip: {Path.GetFullPath(ZipPath)}");
        }
    }
}

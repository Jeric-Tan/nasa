using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OutpostGale.EditorTools
{
    // Batch builds:  Unity.exe -batchmode -quit -projectPath . -executeMethod OutpostGale.EditorTools.BuildScript.BuildWebGL
    public static class BuildScript
    {
        const string OutDir = "Builds/WebGL";

        [MenuItem("Outpost Gale/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigurePlayer();
            var scenes = new[] { "Assets/Scenes/Main.unity" };
            EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Build] {s.result}: {s.totalSize / (1024f * 1024f):0.0} MB in {s.totalTime.TotalSeconds:0}s -> {OutDir}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Outpost Gale/Apply Player Settings")]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Outpost Gale";
            PlayerSettings.productName = "Outpost Gale";
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works on hosts without gzip headers too
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "PROJECT:OutpostGale"; // full-window canvas, Mars loading bar
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL && !Application.isBatchMode)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }
    }

    // Import settings from the plan, applied to art and audio you drop into the project.
    public class ImportSettings : AssetPostprocessor
    {
        bool Ours => assetPath.StartsWith("Assets/ThirdParty/") || assetPath.StartsWith("Assets/Resources/Art/") || assetPath.StartsWith("Assets/Resources/Audio/");

        void OnPreprocessTexture()
        {
            if (!Ours) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 64;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 1024;
            ti.crunchedCompression = true;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }

        void OnPreprocessAudio()
        {
            if (!Ours) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            bool music = Path.GetFileName(assetPath).StartsWith("music") || Path.GetFileName(assetPath).StartsWith("mars_wind");
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = s;
        }
    }
}

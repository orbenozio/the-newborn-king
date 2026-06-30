using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Builds the NewbornKing Android APK from inside the already-open editor (the project is locked to it,
    // so a separate batchmode Unity can't be used). BuildPlayer is synchronous and blocks the bridge for
    // the whole build, so the result is ALSO written to Builds/last_build.json - poll that file from the
    // filesystem even if the bridge call times out. Sets a real package id + app label first.
    public static class build_android
    {
        [McpTool("build_android", "Build the NewbornKing Android APK to Builds/NewbornKing.apk from the open editor. Writes Builds/last_build.json with the result (poll it; the call blocks for the whole build).")]
        public static object Invoke(
            string scene = "Assets/Game/Scenes/Game.unity",
            string output = "Builds/NewbornKing.apk",
            bool development = false)
        {
            const string resultPath = "Builds/last_build.json";
            Directory.CreateDirectory("Builds");
            if (File.Exists(resultPath)) File.Delete(resultPath);   // so a poller only sees THIS build's result

            try
            {
                // A valid package id + a friendly launcher label (the app the user installs is the game).
                // Set unconditionally so the real id wins over Unity's non-empty com.DefaultCompany.* default.
                PlayerSettings.companyName = "Crossroads";
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.crossroads.newbornking");
                PlayerSettings.productName = "Newborn King";
                if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1")
                    PlayerSettings.bundleVersion = "0.1.0";   // shown on the Settings screen (Application.version)

                // Reload the scene from disk so transient edit-mode preview staging (SwipeHint, staged
                // menu/end panels) is discarded and never baked into the build. Dirtiness was already
                // cleared by the preview tools, so this does not prompt to save.
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                var s = report.summary;
                long size = File.Exists(output) ? new FileInfo(output).Length : 0;
                bool ok = s.result == BuildResult.Succeeded;

                string json = "{"
                    + "\"ok\":" + (ok ? "true" : "false")
                    + ",\"result\":\"" + s.result + "\""
                    + ",\"errors\":" + s.totalErrors
                    + ",\"warnings\":" + s.totalWarnings
                    + ",\"output\":\"" + output + "\""
                    + ",\"sizeBytes\":" + size.ToString(CultureInfo.InvariantCulture)
                    + ",\"package\":\"" + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) + "\""
                    + "}";
                File.WriteAllText(resultPath, json);
                return new { ok, result = s.result.ToString(), errors = s.totalErrors, output, sizeBytes = size };
            }
            catch (Exception e)
            {
                string json = "{\"ok\":false,\"result\":\"Exception\",\"error\":\""
                    + e.Message.Replace("\\", "/").Replace("\"", "'") + "\"}";
                File.WriteAllText(resultPath, json);
                return new { ok = false, error = e.Message };
            }
        }
    }
}

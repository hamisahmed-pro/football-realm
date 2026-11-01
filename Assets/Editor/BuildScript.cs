// Football Mogul — Headless build script.
// Usage (Linux editor, from the project root):
//   Unity -batchmode -nographics -quit -projectPath . \
//     -executeMethod FootballMogul.Editor.BuildScript.BuildAndroid
//   Unity -batchmode -nographics -quit -projectPath . \
//     -executeMethod FootballMogul.Editor.BuildScript.BuildWindows
//
// This script creates the bootstrap scene at build time (a GameObject
// with GameManager + MainMenuUI, found by type, not by GUID), so a
// fresh checkout with no .unity scene file still builds. It does NOT
// bypass Unity licensing: the editor must be activated with a valid
// (free Personal) licence before a build will run. See README.md.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FootballMogul.Editor
{
    public static class BuildScript
    {
        private static string EnsureScene()
        {
            string dir = "Assets/Scenes";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = dir + "/FootballMogul.unity";
            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("GameManager");
                go.AddComponent<FootballMogul.Unity.GameManager>();
                go.AddComponent<FootballMogul.Unity.MainMenuUI>();
                EditorSceneManager.SaveScene(scene, path);
            }
            return path;
        }

        private static void Build(string scenePath, BuildTarget target, string output)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("Football Mogul build FAILED: " + report.summary.result);
                EditorApplication.Exit(1);
            }
            Debug.Log("Football Mogul build SUCCEEDED: " + output);
        }

        public static void BuildAndroid()
        {
            Build(EnsureScene(), BuildTarget.Android, "Builds/Android/FootballMogul.apk");
        }

        public static void BuildWindows()
        {
            Build(EnsureScene(), BuildTarget.StandaloneWindows64, "Builds/Windows/FootballMogul.exe");
        }

        public static void BuildLinux()
        {
            Build(EnsureScene(), BuildTarget.StandaloneLinux64, "Builds/Linux/FootballMogul");
        }
    }
}
#endif

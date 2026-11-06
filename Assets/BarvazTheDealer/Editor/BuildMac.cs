using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BarvazTheDealer.EditorTools
{
    /// <summary>
    /// Menu: Barvaz > Build Mac App  ->  Builds/Caribi Barvaz.app
    /// Command line (Unity closed):
    ///   Unity -batchmode -quit -projectPath . -executeMethod BarvazTheDealer.EditorTools.BuildMac.BuildBatch
    /// </summary>
    public static class BuildMac
    {
        private const string ScenePath = "Assets/BarvazTheDealer/Scenes/Main.unity";
        private const string OutputPath = "Builds/Caribi Barvaz.app";

        [MenuItem("Barvaz/Build Mac App")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (Build()) EditorUtility.RevealInFinder(OutputPath);
            else EditorUtility.DisplayDialog(AppIdentity.ProductName, "The build failed. Check the Console for details.", "OK");
        }

        public static void BuildBatch()
        {
            EditorApplication.Exit(Build() ? 0 : 1);
        }

        [MenuItem("Barvaz/Open Main Scene")]
        public static void OpenMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static bool Build()
        {
            EnsureScene();

            AppIdentity.Apply();
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[Barvaz] Mac build " + report.summary.result + ": " + Path.GetFullPath(OutputPath));
            return ok;
        }

        /// <summary>The game builds itself at runtime, so the scene only needs to exist.</summary>
        private static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}

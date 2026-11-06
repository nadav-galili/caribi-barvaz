using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BarvazTheDealer.EditorTools
{
    /// <summary>
    /// Menu: Barvaz > Build Android APK  ->  Builds/BarvazTheDealer.apk
    /// Needs the "Android Build Support" module (with OpenJDK and SDK/NDK tools) installed from Unity Hub.
    /// Command line (Unity closed):
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod BarvazTheDealer.EditorTools.BuildAndroid.BuildBatch
    /// </summary>
    public static class BuildAndroid
    {
        private const string ScenePath = "Assets/BarvazTheDealer/Scenes/Main.unity";
        private const string OutputPath = "Builds/BarvazTheDealer.apk";

        [MenuItem("Barvaz/Build Android APK")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (Build()) EditorUtility.RevealInFinder(OutputPath);
            else EditorUtility.DisplayDialog("Barvaz The Dealer", "The Android build failed. Check the Console for details.", "OK");
        }

        public static void BuildBatch()
        {
            EditorApplication.Exit(Build() ? 0 : 1);
        }

        private static bool Build()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }

            PlayerSettings.productName = "Barvaz The Dealer";
            PlayerSettings.companyName = "Barvaz";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.barvaz.thedealer");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // landscape only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // 64-bit ARM (every current phone), IL2CPP
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            EditorUserBuildSettings.buildAppBundle = false; // a plain .apk you can install directly

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[Barvaz] Android build " + report.summary.result + ": " + Path.GetFullPath(OutputPath));
            return ok;
        }
    }
}

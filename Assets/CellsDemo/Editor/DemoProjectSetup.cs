using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Cells.Demo;

namespace Cells.Editor
{
    public static class DemoProjectSetup
    {
        public const string ScenePath = "Assets/CellsDemo/Scenes/ChemistryDemo.unity";
        public const string CultureScenePath = "Assets/CellsDemo/Scenes/MicroscopeCulture.unity";

        [MenuItem("Cells/Build Windows player")]
        public static void BuildWindowsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildWindows();
        }

        // Callable in batch mode with -executeMethod Cells.Editor.DemoProjectSetup.BuildWindows.
        public static void BuildWindows()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new System.InvalidOperationException("Install Windows Build Support (Mono) for this Unity Editor.");
            foreach (string scene in new[] { CultureScenePath, ScenePath })
                if (!File.Exists(scene)) throw new FileNotFoundException("Required build scene is missing", scene);

            string output = System.Environment.GetEnvironmentVariable("CELLS_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = "Build/Windows/Cells.exe";
            output = Path.GetFullPath(output);
            if (!string.Equals(Path.GetExtension(output), ".exe", System.StringComparison.OrdinalIgnoreCase))
                throw new System.ArgumentException("CELLS_BUILD_OUTPUT must end in .exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            var oldBackend = PlayerSettings.GetScriptingBackend(target);
            try
            {
                // Mono supports producing a Windows player from the Linux editor.
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { CultureScenePath, ScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.Exception("Cells build failed: " + report.summary.result);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "BUILD-INFO.txt"),
                    "cells Windows x64\nUnity " + Application.unityVersion +
                    "\nBackend: Mono\nEntry scene: " + CultureScenePath +
                    "\nBuilt UTC: " + System.DateTime.UtcNow.ToString("O") +
                    "\nKeep the entire build folder together. Launch " + Path.GetFileName(output) + ".\n");
                Debug.Log("CELLS_BUILD_OK: " + output + " | " + report.summary.totalSize + " bytes");
            }
            finally { PlayerSettings.SetScriptingBackend(target, oldBackend); }
        }

        [MenuItem("Cells/Create or open chemistry demo")]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            Directory.CreateDirectory("Assets/CellsDemo/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Cells chemistry demo").AddComponent<ChemistryDemoBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("CELLS_SETUP_OK: " + Application.unityVersion);
        }

        [MenuItem("Cells/Create or open microscope culture")]
        public static void CreateCultureScene()
        {
            if (File.Exists(CultureScenePath)) EditorSceneManager.OpenScene(CultureScenePath);
            else
            {
                Directory.CreateDirectory("Assets/CellsDemo/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var demo = new GameObject("Cells microscope culture").AddComponent<ChemistryDemoBootstrap>();
                demo.cultureView = true;
                demo.initialToxin = 0f;
                EditorSceneManager.SaveScene(scene, CultureScenePath);
            }
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(CultureScenePath, true), new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }
    }
}

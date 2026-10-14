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

        public static void BuildWindows()
        {
            CreateScene();
            CreateCultureScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { CultureScenePath, ScenePath },
                locationPathName = "Build/Cells.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Cells build failed: " + report.summary.result);
            Debug.Log("CELLS_BUILD_OK: " + report.summary.totalSize + " bytes");
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

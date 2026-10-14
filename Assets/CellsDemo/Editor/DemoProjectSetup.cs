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

        public static void BuildWindows()
        {
            CreateScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
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
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cells.Demo;

namespace Cells.EditorTools
{
    /// <summary>Creates a repeatable scene asset rather than requiring manual runtime wiring.</summary>
    public static class CellsSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ChemistryLab.unity";

        [MenuItem("Cells/Create Chemistry Lab Scene")]
        public static void CreateChemistryLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("cells / Chemistry Lab");
            root.AddComponent<ChemistryDemoBootstrap>();
            root.AddComponent<ChemistryDemoView>();

            var cameraObject = new GameObject("Microscope Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .035f, .08f);
            cameraObject.transform.position = new Vector3(0, 0, -10);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("cells scene created: " + ScenePath + ". Press Play to run the two-cell chemistry fixture.");
        }

        // Run with -batchmode -executeMethod Cells.EditorTools.CellsSceneBuilder.CreateBatchScene.
        public static void CreateBatchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("cells / Chemistry Lab");
            root.AddComponent<ChemistryDemoBootstrap>();
            root.AddComponent<ChemistryDemoView>();
            var cameraObject = new GameObject("Microscope Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .035f, .08f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("cells batch scene generation completed: " + ScenePath);
        }
    }
}
#endif

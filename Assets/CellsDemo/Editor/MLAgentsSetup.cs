using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Cells.Editor
{
    public static class MLAgentsSetup
    {
        static AddRequest request;

        [MenuItem("Cells/ML Agents/1 Install ML Agents package")]
        public static void Install()
        {
            if (request != null) return;
            request = Client.Add("com.unity.ml-agents@4.0.0");
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (request == null || !request.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
                Debug.Log("ML Agents installed. After compilation, use Cells/ML Agents/2 Enable adapter source.");
            else Debug.LogError(request.Error?.message);
            request = null;
        }

        [MenuItem("Cells/ML Agents/2 Enable adapter source")]
        public static void EnableAdapter()
        {
            if (!UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Any(p => p.name == "com.unity.ml-agents"))
                throw new System.InvalidOperationException("Install ML Agents and wait for resolution first.");
            string root = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(root, "Integrations/MLAgents/CellForagingAgent.cs.txt");
            string folder = "Assets/CellsMLAgents";
            Directory.CreateDirectory(folder);
            File.Copy(source, folder + "/CellForagingAgent.cs", true);
            File.WriteAllText(folder + "/Cells.MLAgents.asmdef",
                "{\"name\":\"Cells.MLAgents\",\"references\":[\"Cells.Runtime\",\"Unity.ML-Agents\"]}");
            Directory.CreateDirectory(folder + "/Editor");
            File.Copy(Path.Combine(root, "Integrations/MLAgents/MLTrainingSceneSetup.cs.txt"),
                folder + "/Editor/MLTrainingSceneSetup.cs", true);
            File.WriteAllText(folder + "/Editor/Cells.MLAgents.Editor.asmdef",
                "{\"name\":\"Cells.MLAgents.Editor\",\"references\":[\"Cells.MLAgents\",\"Cells.Runtime\",\"Unity.ML-Agents\"],\"includePlatforms\":[\"Editor\"]}");
            AssetDatabase.Refresh();
            Debug.Log("Adapter enabled. After compilation use Cells/ML Agents/3 Create or open training scene.");
        }
    }
}

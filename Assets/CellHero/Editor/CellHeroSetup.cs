using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Cells.Hero;

namespace Cells.Hero.Editor
{
    public static class CellHeroSetup
    {
        private const string Root = "Assets/CellHero";
        [Serializable] private sealed class Palette { public Pigment[] materials; }
        [Serializable] private sealed class Pigment
        {
            public string name;
            public float[] color;
            public float alpha, roughness, metallic;
            public bool membrane;
        }

        [InitializeOnLoadMethod]
        private static void OnLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(Root + "/Models/CellHero_LOD0.fbx") && !File.Exists(Root + "/Prefabs/CellHero.prefab"))
                {
                    try { BuildPrefab(); }
                    catch (Exception ex) { Debug.LogError("Cell Hero setup: " + ex.Message); }
                }
            };
        }

        [MenuItem("Tools/Cells/3D Cell/Build Hero Prefab")]
        public static void BuildPrefab()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            ImportTextures();
            var palette = JsonUtility.FromJson<Palette>(File.ReadAllText(Root + "/Documentation/materials.json"));
            if (palette == null || palette.materials == null) throw new InvalidDataException("Missing pigment palette.");
            var mats = new System.Collections.Generic.Dictionary<string, Material>();
            var pipeline = GraphicsSettings.currentRenderPipeline;
            bool hdrp = pipeline != null && pipeline.GetType().Name.Contains("HDRenderPipeline");
            bool urp = pipeline != null && pipeline.GetType().Name.Contains("Universal");
            string litName = hdrp ? "HDRP/Lit" : urp ? "Universal Render Pipeline/Lit" : "Standard";
            var lit = Shader.Find(litName);
            if (lit == null) throw new InvalidOperationException("The active pipeline's Lit shader is unavailable: " + litName);
            foreach (var p in palette.materials)
            {
                // The membrane shader is an art-directed transparency approximation.
                var selectedShader = p.membrane ? Shader.Find("Cells/Hero/TranslucentMembrane") : lit;
                if (selectedShader == null) selectedShader = lit;
                string path = Root + "/Materials/" + p.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(selectedShader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = selectedShader;
                var color = new Color(p.color[0], p.color[1], p.color[2], p.alpha);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", new Color(color.r, color.g, color.b, 1));
                var albedo = LoadTexture("Cell_SurfaceTint.png");
                var normal = LoadTexture("Cell_DetailNormal.png");
                var mask = LoadTexture("Cell_Mask.png");
                SetTexture(mat, "_MainTex", albedo);
                SetTexture(mat, "_BaseMap", albedo);
                SetTexture(mat, "_BaseColorMap", albedo);
                SetTexture(mat, "_BumpMap", normal);
                SetTexture(mat, "_NormalMap", normal);
                SetTexture(mat, "_MaskMap", mask);
                SetTexture(mat, "_MetallicGlossMap", mask);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", p.metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - p.roughness);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - p.roughness);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.3f);
                if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", 0.3f);
                mat.EnableKeyword("_NORMALMAP");
                if (hdrp)
                {
                    mat.EnableKeyword("_MASKMAP");
                    mat.SetFloat("_MetallicRemapMin", 0f);
                    mat.SetFloat("_MetallicRemapMax", p.metallic);
                    mat.SetFloat("_SmoothnessRemapMin", Mathf.Max(0.1f, 1f - p.roughness - 0.18f));
                    mat.SetFloat("_SmoothnessRemapMax", Mathf.Min(0.95f, 1f - p.roughness + 0.18f));
                }
                else
                {
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                }
                if (p.alpha < 1f && !p.membrane) ConfigureTransparency(mat, urp, hdrp);
                if (p.membrane) { mat.SetFloat("_Opacity", 0.045f); mat.SetFloat("_RimOpacity", 0.48f); mat.renderQueue = 3040; }
                else if (p.alpha < 1f) mat.renderQueue = p.name.Contains("Nucleus") ? 3020 : 3000;
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                mats.Add(p.name, mat);
            }
            var root = new GameObject("CellHero");
            try
            {
                var group = root.AddComponent<LODGroup>();
                var lods = new LOD[3];
                for (int i = 0; i < 3; i++)
                {
                    string path = Root + "/Models/CellHero_LOD" + i + ".fbx";
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) throw new FileNotFoundException("Cell mesh is missing", path);
                    importer.importAnimation = false;
                    importer.importBlendShapes = true;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importTangents = ModelImporterTangents.CalculateMikk;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var node = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    node.name = "LOD" + i;
                    node.transform.SetParent(root.transform, false);
                    var renderers = node.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        r.sharedMaterials = r.sharedMaterials.Select(m =>
                        {
                            if (m == null) throw new InvalidDataException("Empty FBX material slot.");
                            string name = m.name.Split('.')[0];
                            if (!mats.TryGetValue(name, out var replacement))
                                throw new InvalidDataException("Unknown FBX material: " + m.name);
                            return replacement;
                        }).ToArray();
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        r.receiveShadows = true;
                        if (r is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = false;
                    }
                    lods[i] = new LOD(new[] { 0.50f, 0.20f, 0.055f }[i], renderers);
                }
                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.None;
                group.RecalculateBounds();
                root.AddComponent<CellHeroAnimation>();
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/CellHero.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("Cell Hero: prefab and materials created. Use Tools/Cells/3D Cell/Open Preview Scene.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ConfigureTransparency(Material mat, bool urp, bool hdrp)
        {
            mat.SetOverrideTag("RenderType", "Transparent");
            if (hdrp)
            {
                mat.SetFloat("_SurfaceType", 1);
                mat.SetFloat("_BlendMode", 0);
                mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_TransparentZWrite", 0);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
                mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
            }
            else
            {
                if (urp) { mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
                else { mat.SetFloat("_Mode", 2); mat.EnableKeyword("_ALPHABLEND_ON"); }
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
            }
        }

        private static Texture2D LoadTexture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + name);
        private static void SetTexture(Material m, string property, Texture texture)
        {
            if (m.HasProperty(property)) m.SetTexture(property, texture);
        }
        private static void ImportTextures()
        {
            foreach (string name in new[] { "Cell_SurfaceTint.png", "Cell_DetailNormal.png", "Cell_Mask.png" })
            {
                string path = Root + "/Textures/" + name;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("Missing texture", path);
                importer.textureType = name.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = name.Contains("Tint");
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Tools/Cells/3D Cell/Open Preview Scene")]
        public static void OpenPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cell = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/CellHero.prefab"));
            var cameraObject = new GameObject("Microscope Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.047f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(3f, 8f, -7f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, -0.7f));
            cameraObject.AddComponent<CellHeroOrbit>().target = cell.transform;
            var key = new GameObject("Key");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(0.85f, 0.94f, 1f);
            key.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var fill = new GameObject("Warm Fill");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.5f;
            fillLight.color = new Color(1f, 0.72f, 0.46f);
            fill.transform.rotation = Quaternion.Euler(30f, 130f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.23f, 0.28f, 0.31f);
            Directory.CreateDirectory(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/CellHeroPreview.unity");
            Selection.activeGameObject = cell;
        }
    }
}

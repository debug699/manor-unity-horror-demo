#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>Forces every material slot under a selected pink Rhino model to a safe URP/Lit material.</summary>
    public static class RhinoPinkMaterialRepair
    {
        private const string Menu = "庄园/模型修复/1. 修复选中模型的粉色材质";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/RhinoRepairs";

        [MenuItem(Menu, true)]
        private static bool CanRun() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

        [MenuItem(Menu)]
        public static void RepairSelected()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("修复粉色模型", "项目没有找到 URP/Lit，无法修复。", "知道了");
                return;
            }

            EnsureFolder(MaterialFolder);
            var replacements = new Dictionary<Material, Material>();
            Material nullSlotReplacement = null;
            int slots = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] original = renderer.sharedMaterials;
                    if (original == null || original.Length == 0) continue;
                    Material[] updated = (Material[])original.Clone();
                    for (int index = 0; index < original.Length; index++)
                    {
                        Material source = original[index];
                        Material replacement;
                        // Rhino FBX import can leave a material slot completely empty. A null
                        // key is invalid in Dictionary, and previously aborted the whole repair.
                        if (source == null)
                        {
                            if (nullSlotReplacement == null)
                                nullSlotReplacement = CreateSafeUrpMaterial(null, shader);
                            replacement = nullSlotReplacement;
                        }
                        else if (!replacements.TryGetValue(source, out replacement))
                        {
                            replacement = CreateSafeUrpMaterial(source, shader);
                            replacements.Add(source, replacement);
                        }
                        updated[index] = replacement;
                        slots++;
                    }
                    Undo.RecordObject(renderer, "Repair Rhino pink material");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (slots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("修复粉色模型", slots > 0
                ? $"已强制替换 {slots} 个材质槽为可用的 URP 材质，并开启双面显示。模型位置、缩放和网格没有改变。\n\n若变成白色，说明 Rhino 原贴图没有随 FBX 导入；继续使用第 2 个工具套用旧木效果。"
                : "选中物体下面没有可显示的模型部件。", "完成");
        }

        private static bool IsPinkOrBroken(Material material)
        {
            if (material == null || material.shader == null) return true;
            if (material.shader.name.Contains("InternalError")) return true;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            return color.r > .82f && color.b > .82f && color.g < .28f;
        }

        private static Material CreateSafeUrpMaterial(Material source, Shader shader)
        {
            var material = new Material(shader) { name = "MAT_Rhino_PinkRepaired" };
            if (source != null)
            {
                Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                if (texture != null) material.SetTexture("_BaseMap", texture);
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
            string path = AssetDatabase.GenerateUniqueAssetPath(MaterialFolder + "/" + material.name + ".mat");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
#endif

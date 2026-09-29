#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>Applies the existing wet-dark wood look to a selected Rhino model that imported plain white.</summary>
    public static class RhinoWhiteModelStyle
    {
        private const string Menu = "庄园/模型修复/2. 给选中白色模型套用旧木效果";
        private const string SourceMaterialPath = "Assets/_Project/Art/Materials/MAT_Wood_WetDark.mat";

        [MenuItem(Menu, true)]
        private static bool CanRun() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

        [MenuItem(Menu)]
        public static void ApplyToSelected()
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            if (source == null)
            {
                EditorUtility.DisplayDialog("套用旧木效果", "没有找到项目的旧木材质，无法继续。", "知道了");
                return;
            }

            // A dedicated copy prevents any later adjustment on this fallback from changing the gate or bridge.
            Material wood = new Material(source) { name = "MAT_Rhino_WhiteModel_OldWood" };
            if (wood.HasProperty("_Cull")) wood.SetFloat("_Cull", (float)CullMode.Off);
            wood.doubleSidedGI = true;
            int renderers = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] slots = renderer.sharedMaterials;
                    if (slots == null || slots.Length == 0) continue;
                    Material[] replacement = new Material[slots.Length];
                    for (int index = 0; index < replacement.Length; index++) replacement[index] = wood;
                    Undo.RecordObject(renderer, "Apply Rhino old wood style");
                    renderer.sharedMaterials = replacement;
                    EditorUtility.SetDirty(renderer);
                    renderers++;
                }
            }

            if (renderers > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("套用旧木效果", renderers > 0
                ? $"已给 {renderers} 个模型部件套用潮湿旧木效果，并开启双面显示。\n\n请只选中需要处理的模型最外层；它会覆盖该模型原有的所有材质。"
                : "选中物体下没有能显示的模型部件。", "完成");
        }
    }
}
#endif

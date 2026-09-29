#if UNITY_EDITOR
using System.Linq;
using Manor.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>Creates centre hinges and links two independently selectable leaves to the selected tripo9 keypad.</summary>
    public static class DoubleGateKeypadSetup
    {
        private const string Menu = "庄园/大门/安装双扇门密码器（选左右门扇和 tripo9）";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => Selection.gameObjects != null && Selection.gameObjects.Length == 3;

        [MenuItem(Menu)]
        public static void Install()
        {
            GameObject[] selected = Selection.gameObjects.Where(item => item != null).ToArray();
            GameObject keypad = selected.FirstOrDefault(item => item.name.ToLowerInvariant().Contains("tripo9") || item.name.Contains("密码"));
            GameObject[] leaves = selected.Where(item => item != keypad).OrderBy(item => GetBounds(item).center.x).ToArray();
            if (keypad == null || leaves.Length != 2 || !HasRenderer(leaves[0]) || !HasRenderer(leaves[1]))
            {
                EditorUtility.DisplayDialog("安装双扇大门", "请在 Hierarchy 按住 Ctrl 同时选中：左门扇、右门扇、名字含 tripo9 的密码器。\n\n左右门扇必须是两个能独立选中的物体；如果选中一个后整扇大门都亮，说明它还没有拆成左右两扇，无法分别打开。", "知道了");
                return;
            }

            Transform parent = leaves[0].transform.parent;
            Transform leftHinge = CreateHinge("GateLeftHinge_左门中央铰链", parent, leaves[0], true);
            Transform rightHinge = CreateHinge("GateRightHinge_右门中央铰链", parent, leaves[1], false);
            Undo.SetTransformParent(leaves[0].transform, leftHinge, "Parent left gate leaf to hinge");
            Undo.SetTransformParent(leaves[1].transform, rightHinge, "Parent right gate leaf to hinge");

            DoubleGateKeypad controller = keypad.GetComponent<DoubleGateKeypad>();
            if (controller == null) controller = Undo.AddComponent<DoubleGateKeypad>(keypad);
            controller.Configure(leftHinge, rightHinge, "1016", 92f);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("安装完成", "密码器已安装。\n\n进入 Play 模式后，靠近 tripo9 按 E，输入 1016 并按 Enter；两扇门会从中间打开。\n\n如果门朝外侧开，请 Ctrl+Z 后告诉我，我会把旋转方向反过来。", "完成");
        }

        private static Transform CreateHinge(string name, Transform parent, GameObject leaf, bool isLeft)
        {
            Bounds bounds = GetBounds(leaf);
            GameObject hinge = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(hinge, "Create gate centre hinge");
            hinge.transform.SetParent(parent, true);
            hinge.transform.position = new Vector3(isLeft ? bounds.max.x : bounds.min.x, bounds.min.y, bounds.center.z);
            hinge.transform.rotation = leaf.transform.rotation;
            return hinge.transform;
        }

        private static bool HasRenderer(GameObject item) => item.GetComponentInChildren<Renderer>(true) != null;
        private static Bounds GetBounds(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
#endif

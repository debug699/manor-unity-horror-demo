#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>
    /// 将 Rhino FBX 的内嵌材质复制成工程内独立材质，再关闭 URP 背面剔除。
    /// 不修改 FBX 网格；材质副本会完整保留原 Shader、贴图和所有已序列化参数。
    /// </summary>
    public static class RhinoFbxDoubleSidedMaterials
    {
        private const string MenuRoot = "庄园/美术/Rhino FBX 双面材质/";
        private const string GeneratedMaterialsRoot = "Assets/_Project/Art/Materials/RhinoDoubleSided";
        private const string ImportedTexturesRoot = "Assets/_Project/Art/Textures/RhinoImported";
        private const string AutoImportRoot = "Assets/_Project/Art/Environment/SourceModels";
        private const string RhinoTextureLibraryRoot = "D:/游戏制作软件/管理/参考图/建模";
        private const string MarkerPrefix = "ManorRhinoDoubleSided:";
        private static bool runtimePinkProbeQueued;
        private static bool runtimePinkProbeFinished;

        // Runs once after scripts reload so a beginner does not need to locate a diagnostic
        // menu item just to identify visual artefacts in the open scene.
        [InitializeOnLoadMethod]
        private static void AutoScanPinkMarkerCandidatesAfterCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (!string.IsNullOrEmpty(EditorApplication.currentScene))
                {
                    DisableEditorOnlyRouteMarkers();
                    ClearSceneViewObjectIconOverlays();
                    DisableSceneViewGizmoOverlay();
                    WritePinkMarkerCandidateReport();
                }
            };
        }

        [InitializeOnLoadMethod]
        private static void InstallRuntimePinkProbe()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForPinkProbe;
            EditorApplication.playModeStateChanged += OnPlayModeStateChangedForPinkProbe;
        }

        private static void OnPlayModeStateChangedForPinkProbe(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                runtimePinkProbeFinished = false;
                runtimePinkProbeQueued = true;
                EditorApplication.update -= RunRuntimePinkProbeWhenReady;
                EditorApplication.update += RunRuntimePinkProbeWhenReady;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= RunRuntimePinkProbeWhenReady;
                runtimePinkProbeQueued = false;
            }
        }

        private static void RunRuntimePinkProbeWhenReady()
        {
            if (!runtimePinkProbeQueued || runtimePinkProbeFinished || !EditorApplication.isPlaying) return;
            // Let scene bootstrap finish and runtime-only objects appear.
            if (Time.realtimeSinceStartup < 2f) return;
            runtimePinkProbeFinished = true;
            EditorApplication.update -= RunRuntimePinkProbeWhenReady;

            List<string> hits = new List<string>();
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (!IsRuntimePinkMaterial(material)) continue;
                    hits.Add(GetScenePath(renderer.transform) + " [" + renderer.GetType().Name + " / " + (material == null ? "<null>" : material.name) + "]");
                }
            }
            Debug.Log("[Manor][RuntimePinkProbe] hits=" + hits.Count + "\n" + string.Join("\n", hits.Distinct()));
        }

        private static bool IsRuntimePinkMaterial(Material material)
        {
            if (material == null || material.shader == null) return false;
            string shaderName = material.shader.name ?? string.Empty;
            if (shaderName.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            return color.r > 0.82f && color.b > 0.82f && color.g < 0.25f;
        }

        /// <summary>
        /// Route marker transforms were created as empty editor helpers; SceneView draws an
        /// icon for them (the large pink arrow and small pink dots).  They have no Renderer,
        /// Collider or gameplay component, so disabling only these exact helpers cannot alter
        /// the Rhino building, interiors, bridge, gate, NavMesh or placed transforms.
        /// </summary>
        private static void DisableEditorOnlyRouteMarkers()
        {
            int disabled = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == null || !transform.gameObject.activeSelf) continue;
                    bool isRouteMarker = transform.name.IndexOf("_Marker_", StringComparison.OrdinalIgnoreCase) >= 0
                        || transform.name.IndexOf("RouteComplete_", StringComparison.OrdinalIgnoreCase) >= 0
                        || transform.name.IndexOf("RouteStart_", StringComparison.OrdinalIgnoreCase) >= 0
                        || transform.name.IndexOf("BridgeMechanism_", StringComparison.OrdinalIgnoreCase) >= 0
                        || transform.name.IndexOf("Gate_庄园大门", StringComparison.OrdinalIgnoreCase) >= 0;
                    // The identified marker objects can contain Unity's harmless editor
                    // helper component.  Their exact reserved names are the safe boundary;
                    // do not require a Transform-only object here.
                    if (!isRouteMarker) continue;
                    transform.gameObject.SetActive(false);
                    disabled++;
                }
            }
            if (disabled <= 0) return;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Manor][PinkMarkerFix] 已自动关闭 " + disabled + " 个空路线标识；建筑、室内、桥、大门及其位置和缩放没有修改。");
        }

        public static void DisablePinkRouteMarkersMenu()
        {
            int before = CountActiveRouteMarkers();
            DisableEditorOnlyRouteMarkers();
            int after = CountActiveRouteMarkers();
            AssetDatabase.SaveAssets();
            string message = before > after
                ? $"已关闭 {before - after} 个粉色路线/区域标识。\n\n房屋、室内、木桥、大门、碰撞与导航均没有修改。"
                : "没有找到仍开启的粉色路线标识。";
            EditorUtility.DisplayDialog("关闭粉色路线标记", message, "完成");
        }

        private static int CountActiveRouteMarkers()
        {
            int count = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform != null && transform.gameObject.activeInHierarchy && IsRouteMarkerName(transform.name)) count++;
                }
            }
            return count;
        }

        private static bool IsEmptyRouteMarker(Transform transform)
        {
            if (transform == null) return false;
            return IsRouteMarkerName(transform.name);
        }

        private static bool IsRouteMarkerName(string name)
        {
            return name.IndexOf("_Marker_", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("RouteComplete_", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("RouteStart_", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("BridgeMechanism_", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Gate_庄园大门", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasNonTransformComponent(GameObject gameObject)
        {
            return gameObject.GetComponents<Component>().Any(component => component != null && !(component is Transform));
        }

        /// <summary>
        /// Clears only SceneView icon overlays (including the pink arrow/dots).  Icons are an
        /// editor-only presentation setting: no GameObject, component, mesh, transform,
        /// material or runtime/build data is changed.
        /// </summary>
        private static void ClearSceneViewObjectIconOverlays()
        {
            int cleared = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == null) continue;
                    EditorGUIUtility.SetIconForObject(transform.gameObject, null);
                    cleared++;
                }
            }
            Debug.Log("[Manor][PinkMarkerFix] 已清除 " + cleared + " 个 Scene 视图编辑图标覆盖层；没有修改游戏内容。");
            SceneView.RepaintAll();
        }

        private static void DisableSceneViewGizmoOverlay()
        {
            // This is a Scene-view-only display preference.  It does not affect Game view,
            // builds, objects, materials, transforms, or any gameplay system.
            foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
            {
                sceneView.drawGizmos = false;
                sceneView.Repaint();
            }
            Debug.Log("[Manor][PinkMarkerFix] 已关闭 Scene 视图 Gizmos 图标层；运行游戏不受影响。");
        }

        private static bool CanProcessSelectedHierarchy()
        {
            return Selection.gameObjects.Length > 0;
        }

        /// <summary>用于场景对象和 Prefab 根节点：替换所有子 MeshRenderer/SkinnedMeshRenderer 的共享材质。</summary>
        public static void ProcessSelectedHierarchies()
        {
            int rendererCount = 0;
            int slotCount = 0;
            int createdCount = 0;
            HashSet<Material> processedMaterials = new HashSet<Material>();
            HashSet<string> sourceFbxPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (GameObject root in Selection.gameObjects)
                {
                    if (root == null) continue;
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] sourceMaterials = renderer.sharedMaterials;
                        if (sourceMaterials == null || sourceMaterials.Length == 0) continue;

                        bool changed = false;
                        Material[] replacementMaterials = new Material[sourceMaterials.Length];
                        for (int index = 0; index < sourceMaterials.Length; index++)
                        {
                            Material source = sourceMaterials[index];
                            string sourcePath = source == null ? null : AssetDatabase.GetAssetPath(source);
                            if (IsFbx(sourcePath)) sourceFbxPaths.Add(sourcePath);
                            Material replacement = GetOrCreateDoubleSidedMaterial(source, ref createdCount);
                            replacementMaterials[index] = replacement;
                            processedMaterials.Add(replacement);
                            if (replacement != source)
                            {
                                changed = true;
                                slotCount++;
                            }
                        }

                        if (!changed) continue;
                        Undo.RecordObject(renderer, "Assign Rhino double-sided materials");
                        renderer.sharedMaterials = replacementMaterials;
                        EditorUtility.SetDirty(renderer);
                        rendererCount++;
                    }
                }

                // 同时写回 ModelImporter remap；之后 FBX 重导入仍指向外置材质，场景覆盖不会丢失。
                ProcessFbxAssets(sourceFbxPaths.ToArray(), false);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Manor][RhinoMaterials] 已处理 {rendererCount} 个 Renderer、替换 {slotCount} 个材质槽位、新建 {createdCount} 个独立双面材质。 / Processed hierarchy materials.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool CanProcessSelectedFbxOrFolders()
        {
            return GetSelectedFbxPaths().Any() || GetSelectedFolderPaths().Any();
        }

        /// <summary>Selected FBX/folders: restore source appearance where available and make faces visible from both sides.</summary>
        [MenuItem(MenuRoot + "一键修复所选 FBX 或文件夹（贴图 + 双面）", true)]
        private static bool CanRepairSelectedFbxOrFolders()
        {
            return CanProcessSelectedFbxOrFolders();
        }

        [MenuItem(MenuRoot + "一键修复所选 FBX 或文件夹（贴图 + 双面）")]
        public static void RepairSelectedFbxOrFolders()
        {
            string[] paths = GetSelectedFbxPaths()
                .Concat(GetSelectedFolderPaths().SelectMany(FindFbxPathsInFolder))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (paths.Length == 0) return;
            // Material remapping must happen first; only then are the copied materials writable.
            ProcessFbxAssets(paths, false);
            RestoreBaseColorTextures(paths);
        }

        public static void RestoreCurrentSceneTextures()
        {
            HashSet<Material> materials = new HashSet<Material>();
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                CollectRendererMaterials(root, materials);
            }
            HashSet<string> fbxPaths = FindFbxPathsUsingMaterials(materials);
            if (fbxPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("恢复 Rhino 原贴图", "当前场景没有找到可以追溯的 Rhino 外置材质。", "知道了");
                return;
            }
            RestoreBaseColorTextures(fbxPaths.ToArray());
        }

        /// <summary>
        /// 一次性处理项目正式 Rhino 来源目录。先确保每个 FBX 已建立外置材质重映射，
        /// 再恢复可找到的 BaseColor；适用于用户不想逐个选择模型的首次整理。
        /// </summary>
        [MenuItem(MenuRoot + "一键修复全部 Rhino 建筑与室内（推荐首次使用）")]
        public static void RepairAllRhinoEnvironment()
        {
            string[] roots =
            {
                "Assets/_Project/Art/Environment/SourceModels",
                "Assets/_Project/Art/Environment/DerivedModels"
            };
            string[] paths = roots.Where(AssetDatabase.IsValidFolder)
                .SelectMany(FindFbxPathsInFolder)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Rhino 一键修复", "没有在正式 Rhino 模型目录找到 FBX。", "知道了");
                return;
            }

            // 顺序不能反过来：尚未外置材质的 FBX 没有可写的材质可供贴图恢复。
            ProcessFbxAssets(paths, false);
            RestoreBaseColorTextures(paths);
        }

        /// <summary>
        /// 当前关卡大门的定向补救：大门的 Tripo 材质名与贴图名不完全相同，
        /// 因此提供给用户一个不必寻找 FBX 的安全入口。
        /// </summary>
        public static void RepairCurrentManorGate()
        {
            string[] gatePaths = new[]
            {
                "Assets/_Project/Art/Environment/SourceModels/UserLibrary/大门/已拆开.fbx",
                "Assets/_Project/Art/Environment/SourceModels/UserLibrary/大门部件/已拆开大门部件.fbx",
                "Assets/_Project/Art/Environment/SourceModels/UserLibrary/可用大门栅栏/已拆开.fbx"
            }
            .Where(path => AssetImporter.GetAtPath(path) is ModelImporter)
            .ToArray();

            if (gatePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("修复大门", "没有找到当前项目的大门 FBX。", "知道了");
                return;
            }

            ProcessFbxAssets(gatePaths, false);
            RestoreBaseColorTextures(gatePaths);
        }

        public static void HideMagentaDebugMarkers()
        {
            int hidden = 0;
            List<string> names = new List<string>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    // Pink is usually Unity's error-shader fallback, not an actual magenta
                    // _BaseColor; test both forms so old diagnostic/placeholder meshes are found.
                    if (renderer == null || !IsPinkOrErrorMarker(renderer)) continue;
                    Undo.RecordObject(renderer, "Hide magenta debug marker");
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                    hidden++;
                    names.Add(GetScenePath(renderer.transform));
                }
            }
            if (hidden > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            string message = hidden > 0
                ? $"已隐藏 {hidden} 个粉色标记。它们没有被删除，随时可以用 Ctrl+Z 恢复。\n\n命中对象：\n" + string.Join("\n", names.Take(6))
                : "没有检测到粉色/失效 Shader 标记。若画面仍有粉色，请发此弹窗截图；它可能是运行时生成的对象。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("隐藏粉色标记", message, "完成");
        }

        /// <summary>
        /// Safe diagnostic for visual markers which are created by components rather than a
        /// material.  It never changes the scene: the detailed, reproducible candidate list
        /// is written to Editor.log for the assistant to inspect.
        /// </summary>
        public static void DiagnosePinkVisualMarkers()
        {
            List<string> candidates = WritePinkMarkerCandidateReport();
            candidates.AddRange(FindSceneRenderersUsingMagentaTexture());
            candidates = candidates.Distinct().OrderBy(value => value).ToList();
            const string tag = "[Manor][PinkMarkerScan]";
            Debug.Log(tag + " count=" + candidates.Count + "\n" + string.Join("\n", candidates));
            string summary = candidates.Count == 0
                ? "扫描完成：没有找到粒子、线条、导航或名称含“标记/箭头/路径”的候选组件。\n\n我会改用下一层运行时检查；本次没有修改场景。"
                : $"扫描完成：找到 {candidates.Count} 个可能产生箭头/粉点的组件。\n\n本次没有修改任何物体。请点“完成”，结果已记录，我会根据结果精确处理。";
            EditorUtility.DisplayDialog("扫描粉色箭头和粉点", summary, "完成");
        }

        private static List<string> WritePinkMarkerCandidateReport()
        {
            List<string> candidates = new List<string>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    string typeName = component.GetType().FullName ?? component.GetType().Name;
                    string objectName = component.gameObject.name;
                    if (!LooksLikeVisualMarkerComponent(typeName, objectName)) continue;
                    candidates.Add(GetScenePath(component.transform) + "  [" + typeName + "]");
                }
                // A magenta mesh can have a valid custom shader and an unhelpful name.
                // Include all unusually small standalone renderers as a second, deterministic
                // candidate set; this is a report only and changes nothing.
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || renderer is ParticleSystemRenderer) continue;
                    Bounds bounds = renderer.bounds;
                    float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                    if (longest <= 0.01f || longest > 4f) continue;
                    string materialNames = string.Join(", ", renderer.sharedMaterials.Where(m => m != null).Select(m => m.name));
                    candidates.Add(GetScenePath(renderer.transform) + "  [Renderer small=" + longest.ToString("0.00") + ", mats=" + materialNames + "]");
                }
            }
            candidates = candidates.Distinct().OrderBy(value => value).ToList();
            Debug.Log("[Manor][PinkMarkerScan] count=" + candidates.Count + "\n" + string.Join("\n", candidates));
            return candidates;
        }

        // The normal material scan only sees a colour tint.  A pink arrow can instead come
        // from a valid material whose BaseMap contains a magenta pixel.  Inspect just the
        // textures currently used by Scene renderers, so this remains narrow and read-only.
        private static List<string> FindSceneRenderersUsingMagentaTexture()
        {
            List<string> findings = new List<string>();
            HashSet<Texture> checkedTextures = new HashSet<Texture>();
            HashSet<Texture> magentaTextures = new HashSet<Texture>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null) continue;
                        Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
                        if (texture == null) continue;
                        if (!checkedTextures.Contains(texture))
                        {
                            checkedTextures.Add(texture);
                            if (TextureContainsStrongMagenta(texture)) magentaTextures.Add(texture);
                        }
                        if (magentaTextures.Contains(texture))
                            findings.Add("[MagentaTexture] " + GetScenePath(renderer.transform) + "  [" + material.name + " -> " + texture.name + "]");
                    }
                }
            }
            Debug.Log("[Manor][PinkTextureScan] checked=" + checkedTextures.Count + ", magenta=" + magentaTextures.Count + "\n" + string.Join("\n", findings));
            return findings;
        }

        private static bool TextureContainsStrongMagenta(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return false;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            bool originalReadable = importer.isReadable;
            try
            {
                if (!originalReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
                Texture2D readable = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (readable == null) return false;
                Color[] pixels = readable.GetPixels();
                int step = Mathf.Max(1, pixels.Length / 32768);
                for (int index = 0; index < pixels.Length; index += step)
                {
                    Color color = pixels[index];
                    if (color.r > 0.85f && color.b > 0.85f && color.g < 0.20f) return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Manor][PinkTextureScan] 无法读取贴图 " + path + "：" + exception.Message);
            }
            finally
            {
                if (!originalReadable)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
            return false;
        }

        public static void RepairSelectedGateUntexturedParts()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("修复大门灰白面", "请先在 Hierarchy 选中大门最外层物体，再执行本项。", "知道了");
                return;
            }

            Texture2D fallbackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/_Project/Art/Textures/RhinoImported/已拆开/大门1_tripo_part_4_basecolor.JPEG");
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (fallbackTexture == null || urpLit == null)
            {
                EditorUtility.DisplayDialog("修复大门灰白面", "大门木纹贴图或 URP/Lit Shader 尚未准备好。请先执行“大门白膜”修复。", "知道了");
                return;
            }

            EnsureFolder("Assets/_Project/Art/Materials/RhinoDoubleSided/GateFallback");
            const string fallbackPath = "Assets/_Project/Art/Materials/RhinoDoubleSided/GateFallback/MAT_Gate_GrayPanel_WoodFallback.mat";
            Material fallback = AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
            if (fallback == null)
            {
                fallback = new Material(urpLit) { name = "MAT_Gate_GrayPanel_WoodFallback" };
                fallback.SetTexture("_BaseMap", fallbackTexture);
                fallback.SetTexture("_MainTex", fallbackTexture);
                fallback.SetFloat("_Cull", (float)CullMode.Off);
                AssetDatabase.CreateAsset(fallback, fallbackPath);
            }

            int fixedSlots = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0) continue;
                    Material[] updated = null;
                    for (int index = 0; index < materials.Length; index++)
                    {
                        Material material = materials[index];
                        if (!NeedsGateFallback(material)) continue;
                        if (updated == null) updated = (Material[])materials.Clone();
                        updated[index] = fallback;
                        fixedSlots++;
                    }
                    if (updated == null) continue;
                    Undo.RecordObject(renderer, "Fix gate untextured panel");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (fixedSlots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = fixedSlots > 0
                ? $"已给 {fixedSlots} 个没有颜色贴图的大门子面补上旧木纹。\n\n已带贴图的木材、铁件、锁和桥没有修改。"
                : "当前选中的大门没有检测到“没有颜色贴图”的子面。灰白区域可能来自另一个独立物体；请保留选中状态并发截图。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("修复大门灰白面", message, "完成");
        }

        /// <summary>
        /// 大门可交互功能会在 Rhino 大门前叠加一个可转动的门扇；该门扇来自通用门库，
        /// 不是 FBX 的材质槽，必须单独指定与 Rhino 大门一致的木纹，才能在正反两面都一致。
        /// </summary>
        public static void RepairGateInteractiveLeaf()
        {
            Transform pivot = FindSceneTransformByName("UserGateDoorPivot_用户大门铰链");
            if (pivot == null)
            {
                EditorUtility.DisplayDialog("修复大门开关门扇", "当前场景没有找到大门开关门扇。请确认打开的是庄园 Demo 场景。", "知道了");
                return;
            }

            Material woodMaterial = GetOrCreateGateWoodFallback();
            int slots = 0;
            foreach (Renderer renderer in pivot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] current = renderer.sharedMaterials;
                if (current == null || current.Length == 0) continue;
                Material[] replacement = Enumerable.Repeat(woodMaterial, current.Length).ToArray();
                Undo.RecordObject(renderer, "Apply Rhino gate wood to interactive leaf");
                renderer.sharedMaterials = replacement;
                EditorUtility.SetDirty(renderer);
                slots += current.Length;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = slots > 0
                ? $"已给大门的开关门扇回填 {slots} 个 Rhino 木纹材质槽位。\n\n灰白遮挡面将变为旧木门纹理；大门仍可正常开关。"
                : "找到了大门铰链，但下面没有可见门扇材质。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("修复大门开关门扇", message, "完成");
        }

        /// <summary>
        /// 用实际的 Renderer 与材质数据定位大门上残留的灰白覆盖面。
        /// 仅处理位于 PF_UserGate 根节点下、材质颜色为灰且没有可见木纹的平面网格；
        /// Rhino 已贴图的门框/铁件/锁不会满足该条件。
        /// </summary>
        public static void RepairGateGrayOverlayByScan()
        {
            Transform gateRoot = FindSceneTransformByName("PF_UserGate_用户大门");
            if (gateRoot == null)
            {
                EditorUtility.DisplayDialog("扫描大门灰白面", "没有找到 PF_UserGate_用户大门。请确认当前打开的是庄园 Demo 场景。", "知道了");
                return;
            }

            Material fallback = GetOrCreateGateWoodFallback();
            int fixedSlots = 0;
            List<string> inspected = new List<string>();
            foreach (Renderer renderer in gateRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0) continue;
                Material[] updated = null;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (!LooksLikeGrayGateOverlay(renderer, material)) continue;
                    if (updated == null) updated = (Material[])materials.Clone();
                    updated[index] = fallback;
                    fixedSlots++;
                    inspected.Add(renderer.gameObject.name + " / " + (material == null ? "<null>" : material.name));
                }
                if (updated == null) continue;
                Undo.RecordObject(renderer, "Repair Rhino gate gray overlay");
                renderer.sharedMaterials = updated;
                EditorUtility.SetDirty(renderer);
            }

            if (fixedSlots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = fixedSlots > 0
                ? $"已扫描并修复 {fixedSlots} 个大门灰白面材质槽位，已替换为 Rhino 大门木纹。\n\n命中对象：\n" + string.Join("\n", inspected.Take(5))
                : "已扫描大门的全部可见网格，但没有找到“灰色且无可见木纹”的材质。\n\n这表示灰白块可能不是材质，而是 Rhino 网格本身没有对应 UV/贴图区域；下一步需要根据扫描到的网格名单生成针对性补面。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("扫描大门灰白面", message, "完成");
        }

        private static Transform FindSceneTransformByName(string objectName)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindTransformByName(root.transform, objectName);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindTransformByName(Transform parent, string objectName)
        {
            if (parent.name == objectName) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindTransformByName(parent.GetChild(index), objectName);
                if (found != null) return found;
            }
            return null;
        }

        private static Material GetOrCreateGateWoodFallback()
        {
            const string materialFolder = "Assets/_Project/Art/Materials/RhinoDoubleSided/GateFallback";
            const string fallbackPath = materialFolder + "/MAT_Gate_GrayPanel_WoodFallback.mat";
            // The three repaired Rhino sheets have no valid UVs.  A normal BaseMap would
            // therefore sample a random (grey) area of the original atlas.  This shader
            // projects grain from world space and remains correct after Transform scaling.
            Shader worldWood = Shader.Find("Manor/Rhino Gate World Wood");
            if (worldWood == null)
                throw new InvalidOperationException("大门世界坐标木纹 Shader 尚未准备好。");

            EnsureFolder(materialFolder);
            Material fallback = AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
            if (fallback == null)
            {
                fallback = new Material(worldWood) { name = "MAT_Gate_GrayPanel_WoodFallback" };
                AssetDatabase.CreateAsset(fallback, fallbackPath);
            }
            fallback.shader = worldWood;
            fallback.SetColor("_BaseColor", new Color(0.18f, 0.078f, 0.035f, 1f));
            fallback.SetColor("_GrainColor", new Color(0.045f, 0.018f, 0.009f, 1f));
            fallback.SetFloat("_Scale", 2.1f);
            EditorUtility.SetDirty(fallback);
            return fallback;
        }

        private static bool LooksLikeGrayGateOverlay(Renderer renderer, Material material)
        {
            if (renderer == null) return false;
            Bounds bounds = renderer.bounds;
            // 灰白问题面是门洞中央的高而窄平板；排除细小锁、铆钉和桥梁部件。
            if (bounds.size.y < 1.2f || Mathf.Max(bounds.size.x, bounds.size.z) < 0.35f) return false;
            if (material == null) return true;

            Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            if (texture == null) return true;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            // 只有无色相的中灰覆盖材质才替换；有明显棕色的原门木材保持不变。
            return max - min < 0.06f && max > 0.35f;
        }

        private static bool NeedsGateFallback(Material material)
        {
            if (material == null) return true;
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) return false;
            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null) return false;
            return true;
        }

        /// <summary>
        /// General counterpart to the gate repair. Use after the normal Rhino double-sided and
        /// BaseColor recovery steps for a selected model that still has blank white/grey faces.
        /// Only untextured neutral-colour slots are changed; normal textured wood, metal, glass
        /// and deliberately coloured materials remain untouched.
        /// </summary>
        private static bool CanRepairSelectedUntexturedWhiteParts()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        public static void RepairSelectedUntexturedWhiteParts()
        {
            Material fallback = GetOrCreateGenericWorldWoodFallback();
            int repairedSlots = 0;
            List<string> repairedObjects = new List<string>();

            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] current = renderer.sharedMaterials;
                    if (current == null || current.Length == 0) continue;
                    Material[] updated = null;
                    for (int index = 0; index < current.Length; index++)
                    {
                        if (!NeedsGenericWhiteWoodFallback(current[index])) continue;
                        if (updated == null) updated = (Material[])current.Clone();
                        updated[index] = fallback;
                        repairedSlots++;
                        repairedObjects.Add(renderer.gameObject.name);
                    }
                    if (updated == null) continue;
                    Undo.RecordObject(renderer, "Repair Rhino untextured white material");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (repairedSlots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = repairedSlots > 0
                ? $"已把 {repairedSlots} 个白/灰且无贴图的材质槽补为双面旧木风格。\n\n已处理对象：{string.Join("、", repairedObjects.Distinct().Take(6))}\n\n正常贴图、铁件、玻璃、模型网格、位置和缩放均未修改。"
                : "没有找到同时满足“白/灰色且无贴图”的材质槽。请先运行“恢复原贴图”，或只选中仍发白的具体子物体再试。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("补齐 Rhino 白色无贴图面", message, "完成");
        }

        private static Material GetOrCreateGenericWorldWoodFallback()
        {
            const string folder = "Assets/_Project/Art/Materials/RhinoDoubleSided/WorldWoodFallback";
            const string path = folder + "/MAT_Rhino_Untextured_WorldWood.mat";
            Shader shader = Shader.Find("Manor/Rhino World Wood");
            if (shader == null) throw new InvalidOperationException("通用 Rhino 世界坐标木纹 Shader 尚未完成导入。");
            EnsureFolder(folder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "MAT_Rhino_Untextured_WorldWood" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.18f, 0.078f, 0.035f, 1f));
            material.SetColor("_GrainColor", new Color(0.045f, 0.018f, 0.009f, 1f));
            material.SetFloat("_Scale", 2.1f);
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool NeedsGenericWhiteWoodFallback(Material material)
        {
            if (material == null) return true;
            Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            if (texture != null) return false;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            float maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            // Explicitly restrict the automatic replacement to neutral white/grey fallback
            // materials. This avoids painting intended red clues or dark metal as wood.
            return maximum > 0.45f && maximum - minimum < 0.10f;
        }

        /// <summary>
        /// Deliberate manual override for Rhino meshes whose material technically has a texture
        /// but whose UV coordinates point at a blank atlas region. The caller must select the
        /// visibly white child object itself; every slot below that object is then replaced.
        /// </summary>
        private static bool CanForceApplyWorldWoodToSelectedParts()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        public static void ForceApplyWorldWoodToSelectedParts()
        {
            Material fallback = GetOrCreateGenericWorldWoodFallback();
            int rendererCount = 0;
            int slotCount = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                // Include the selected object and its descendants, but never its parent/siblings.
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] current = renderer.sharedMaterials;
                    if (current == null || current.Length == 0) continue;
                    Material[] updated = Enumerable.Repeat(fallback, current.Length).ToArray();
                    Undo.RecordObject(renderer, "Force Rhino white part to world wood");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                    rendererCount++;
                    slotCount += current.Length;
                }
            }

            if (rendererCount > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = rendererCount > 0
                ? $"已将手动选中的部分内 {rendererCount} 个 Renderer、{slotCount} 个材质槽强制替换为双面旧木。\n\n只影响你当前选中的子物体及其子级；大门其他部分、木桥、房屋和 Transform 均未修改。若选错可立刻 Ctrl+Z。"
                : "当前选中物体下面没有 Renderer；请在 Hierarchy 展开模型，选中实际显示白色的 Mesh 子物体。";
            Debug.Log("[Manor][RhinoMaterials] " + message);
            EditorUtility.DisplayDialog("强制修复已选 Rhino 白色子物体", message, "完成");
        }

        private static bool HasStrongMagentaMaterial(IEnumerable<Material> materials)
        {
            if (materials == null) return false;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                if (color.r > 0.75f && color.b > 0.75f && color.g < 0.30f) return true;
            }
            return false;
        }

        private static bool IsPinkOrErrorMarker(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return false;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                string shaderName = material.shader == null ? string.Empty : material.shader.name;
                if (string.IsNullOrEmpty(shaderName)
                    || shaderName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0
                    || shaderName.IndexOf("Hidden/InternalError", StringComparison.OrdinalIgnoreCase) >= 0
                    || material.name.IndexOf("magenta", StringComparison.OrdinalIgnoreCase) >= 0
                    || material.name.IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return HasStrongMagentaMaterial(materials);
        }

        private static bool LooksLikeVisualMarkerComponent(string typeName, string objectName)
        {
            string combined = (typeName + " " + objectName).ToLowerInvariant();
            return combined.Contains("particlesystem")
                   || combined.Contains("linerenderer")
                   || combined.Contains("trailrenderer")
                   || combined.Contains("navmesh")
                   || combined.Contains("gizmo")
                   || combined.Contains("debug")
                   || combined.Contains("marker")
                   || combined.Contains("waypoint")
                   || combined.Contains("arrow")
                   || combined.Contains("标记")
                   || combined.Contains("箭头")
                   || combined.Contains("路径");
        }

        private static string GetScenePath(Transform transform)
        {
            if (transform == null) return "<unknown>";
            List<string> segments = new List<string>();
            while (transform != null)
            {
                segments.Add(transform.name);
                transform = transform.parent;
            }
            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void CollectRendererMaterials(GameObject root, ISet<Material> materials)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null) materials.Add(material);
                }
            }
        }

        private static HashSet<string> FindFbxPathsUsingMaterials(ISet<Material> materials)
        {
            HashSet<string> fbxPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (materials == null || materials.Count == 0) return fbxPaths;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsFbx(assetPath)) continue;
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null) continue;
                foreach (UnityEngine.Object mapped in importer.GetExternalObjectMap().Values)
                {
                    if (mapped is Material mappedMaterial && materials.Contains(mappedMaterial))
                    {
                        fbxPaths.Add(assetPath);
                        break;
                    }
                }
            }
            return fbxPaths;
        }

        [MenuItem(MenuRoot + "打开使用说明")]
        public static void OpenUsageGuide()
        {
            EditorUtility.DisplayDialog(
                "Rhino FBX 双面材质",
                "你只需要使用两个操作：\n\n1. 新导入的单个 Rhino FBX：在 Project 窗口选中 FBX 或它所在的文件夹，执行“一键修复所选 FBX 或文件夹（贴图 + 双面）”。\n\n2. 以前已经导入、整个场景需要整理：执行“一键修复全部 Rhino 建筑与室内”。\n\n它会先尽力找回原 BaseColor 贴图，再把材质设为双面显示，解决“一面正常、另一面能透过去”的问题。\n\n如果模型本身没有导出贴图或 UV 信息，Unity 无法自动还原原来的花纹；这时需要从 Rhino 重新导出时保留贴图/UV。",
                "知道了");
        }

        internal static bool ShouldAutoProcess(string assetPath)
        {
            return IsFbx(assetPath) && assetPath.StartsWith(AutoImportRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        internal static void ProcessImportedFbx(string assetPath)
        {
            ProcessFbxAssets(new[] { assetPath }, false);
        }

        internal static void RestoreKnownManorHouseTexturesOnce()
        {
            const string knownHouseFbx = "Assets/_Project/Art/Environment/DerivedModels/新版主楼/新版主楼_UnityCompatible.fbx";
            if (AssetImporter.GetAtPath(knownHouseFbx) == null) return;
            RestoreBaseColorTextures(new[] { knownHouseFbx });
        }

        private static void ProcessFbxAssets(IReadOnlyList<string> fbxPaths, bool showDialog)
        {
            if (fbxPaths == null || fbxPaths.Count == 0) return;
            int createdCount = 0;
            int remapCount = 0;
            List<string> errors = new List<string>();

            try
            {
                for (int itemIndex = 0; itemIndex < fbxPaths.Count; itemIndex++)
                {
                    string fbxPath = fbxPaths[itemIndex];
                    EditorUtility.DisplayProgressBar("Rhino FBX 双面材质", "处理 " + fbxPath, (float)itemIndex / fbxPaths.Count);
                    ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (importer == null)
                    {
                        errors.Add("不是可用的 ModelImporter: " + fbxPath);
                        continue;
                    }

                    Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                        .OfType<Material>()
                        .Where(material => material != null)
                        .ToArray();

                    bool importerChanged = false;
                    Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> existingRemaps = importer.GetExternalObjectMap();
                    foreach (Material embedded in embeddedMaterials)
                    {
                        Material external = GetOrCreateDoubleSidedMaterial(embedded, ref createdCount);
                        AssetImporter.SourceAssetIdentifier identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name);
                        if (!existingRemaps.TryGetValue(identifier, out UnityEngine.Object mappedMaterial) || mappedMaterial != external)
                        {
                            importer.AddRemap(identifier, external);
                            importerChanged = true;
                            remapCount++;
                        }
                    }

                    if (importerChanged)
                    {
                        importer.SaveAndReimport();
                    }
                }

                AssetDatabase.SaveAssets();
                string message = $"[Manor][RhinoMaterials] 已处理 {fbxPaths.Count} 个 FBX，建立 {remapCount} 条材质重映射，新建 {createdCount} 个独立双面材质。";
                if (errors.Count > 0) message += "\n" + string.Join("\n", errors);
                Debug.Log(message);
                if (showDialog) EditorUtility.DisplayDialog("Rhino FBX 双面材质", message, "完成");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void RestoreBaseColorTextures(IReadOnlyList<string> fbxPaths)
        {
            if (fbxPaths == null || fbxPaths.Count == 0) return;
            int copiedCount = 0;
            int assignedCount = 0;
            List<string> notFound = new List<string>();

            try
            {
                for (int index = 0; index < fbxPaths.Count; index++)
                {
                    string fbxPath = fbxPaths[index];
                    EditorUtility.DisplayProgressBar("恢复 Rhino BaseColor 贴图", "处理 " + fbxPath, (float)index / fbxPaths.Count);
                    ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (importer == null) continue;

                    string modelName = Path.GetFileNameWithoutExtension(fbxPath);
                    string textureModelName = GetTextureModelName(modelName);
                    string[] sourceTextures = FindRhinoBaseColorTextures(textureModelName, fbxPath);
                    if (sourceTextures.Length == 0)
                    {
                        notFound.Add(modelName);
                        continue;
                    }

                    Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remaps = importer.GetExternalObjectMap();
                    Texture2D singleTextureFallback = null;
                    if (sourceTextures.Length == 1)
                    {
                        string fallbackAssetPath = CopyTextureIntoProject(sourceTextures[0], textureModelName, ref copiedCount);
                        singleTextureFallback = AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackAssetPath);
                    }
                    foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> mapping in remaps)
                    {
                        if (mapping.Key.type != typeof(Material) || !(mapping.Value is Material material)) continue;
                        string partName = NormalizeMaterialPartName(mapping.Key.name);
                        string sourceTexture = FindTextureForPart(sourceTextures, partName);
                        Texture2D texture = singleTextureFallback;
                        if (!string.IsNullOrEmpty(sourceTexture))
                        {
                            string textureAssetPath = CopyTextureIntoProject(sourceTexture, textureModelName, ref copiedCount);
                            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
                        }
                        if (texture == null) continue;
                        material.SetTexture("_BaseMap", texture);
                        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                        EditorUtility.SetDirty(material);
                        assignedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                string message = $"[Manor][RhinoMaterials] 已复制 {copiedCount} 张 BaseColor 贴图，并赋给 {assignedCount} 个独立材质。";
                if (notFound.Count > 0) message += "\n未找到贴图文件夹/文件的 FBX: " + string.Join("、", notFound.Distinct());
                Debug.Log(message);
                EditorUtility.DisplayDialog("恢复 Rhino BaseColor 贴图", message, "完成");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string[] FindRhinoBaseColorTextures(string modelName, string fbxPath)
        {
            if (!Directory.Exists(RhinoTextureLibraryRoot)) return Array.Empty<string>();
            string folderName = Path.GetFileName(Path.GetDirectoryName(fbxPath));
            string[] prefixes = new[] { modelName, folderName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // 第一层：与 FBX 名称相同的贴图前缀（例如“新版主楼_tripo_part_0_basecolor”）。
            IEnumerable<string> matches = prefixes.SelectMany(prefix =>
                Directory.GetFiles(RhinoTextureLibraryRoot, prefix + "_*_basecolor.*", SearchOption.AllDirectories));

            // 第二层：Rhino 整理/拆件后的 FBX 经常叫“已拆开G03”，原图却放在“G03/*.fbm”
            // 且实际文件前缀是 tripo_convert UUID。找到同名资源文件夹后直接读取里面的 basecolor。
            // 这也覆盖“木门1”“废弃木屋主用”等资源目录，不依赖 FBX 名称和贴图前缀完全一致。
            string roomCode = ExtractRoomCode(modelName) ?? ExtractRoomCode(folderName);
            IEnumerable<string> folderKeys = prefixes
                .Concat(string.IsNullOrEmpty(roomCode) ? Array.Empty<string>() : new[] { roomCode })
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (string key in folderKeys)
            {
                IEnumerable<string> candidateFolders = Directory.GetDirectories(RhinoTextureLibraryRoot, key, SearchOption.AllDirectories)
                    .Where(path => string.Equals(Path.GetFileName(path), key, StringComparison.OrdinalIgnoreCase));
                matches = matches.Concat(candidateFolders.SelectMany(folder =>
                    Directory.GetFiles(folder, "*_basecolor.*", SearchOption.AllDirectories)));
            }

            return matches
                .Where(IsImageFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string ExtractRoomCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                value, @"(?<![A-Za-z0-9])(G(?:0[1-9]|1[0-2])|B0[1-2])(?![A-Za-z0-9])",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Value.ToUpperInvariant() : null;
        }

        private static string GetTextureModelName(string fbxModelName)
        {
            const string unityCompatibleSuffix = "_UnityCompatible";
            return fbxModelName.EndsWith(unityCompatibleSuffix, StringComparison.OrdinalIgnoreCase)
                ? fbxModelName.Substring(0, fbxModelName.Length - unityCompatibleSuffix.Length)
                : fbxModelName;
        }

        private static string FindTextureForPart(IEnumerable<string> texturePaths, string partName)
        {
            // FBX 内的材质常为 Material_tripo_node_xxx，而文件是
            // tripo_node_xxx_BaseColor.jpg（开头前没有下划线），所以不能要求前缀 "_"。
            // 用完整零件名 + BaseColor 后缀匹配，仍不会误配为相邻零件。
            return texturePaths.FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).IndexOf(partName + "_basecolor", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string CopyTextureIntoProject(string sourceTexturePath, string modelName, ref int copiedCount)
        {
            EnsureFolder(ImportedTexturesRoot);
            string destinationFolder = ImportedTexturesRoot + "/" + SanitizeName(modelName);
            EnsureFolder(destinationFolder);
            string destinationPath = destinationFolder + "/" + SanitizeName(Path.GetFileName(sourceTexturePath));
            string absoluteDestinationPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", destinationPath));
            if (!File.Exists(absoluteDestinationPath))
            {
                FileUtil.CopyFileOrDirectory(sourceTexturePath, absoluteDestinationPath);
                AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
                copiedCount++;
            }
            return destinationPath;
        }

        private static string NormalizeMaterialPartName(string materialName)
        {
            string value = materialName ?? string.Empty;
            if (value.StartsWith("Material_", StringComparison.OrdinalIgnoreCase)) value = value.Substring("Material_".Length);
            int instanceSuffix = value.LastIndexOf('.');
            if (instanceSuffix >= 0 && value.Substring(instanceSuffix + 1).All(char.IsDigit)) value = value.Substring(0, instanceSuffix);
            // Tripo/Rhino 部分 FBX 将材质导出为 "tripo_part_0_material"，
            // 原始贴图则为 "大门1_tripo_part_0_basecolor"。去掉该导出后缀后才能正确对应。
            const string materialSuffix = "_material";
            if (value.EndsWith(materialSuffix, StringComparison.OrdinalIgnoreCase))
                value = value.Substring(0, value.Length - materialSuffix.Length);
            return value;
        }

        private static bool IsImageFile(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
        }

        private static Material GetOrCreateDoubleSidedMaterial(Material source, ref int createdCount)
        {
            if (source == null) return null;

            if (IsGeneratedMaterial(source))
            {
                SetDoubleSided(source);
                EditorUtility.SetDirty(source);
                return source;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (!IsFbx(sourcePath))
            {
                // 已经是独立材质时，只修改可写的项目内材质；不复制，不改变既有的共享关系。
                if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    SetDoubleSided(source);
                    EditorUtility.SetDirty(source);
                }
                return source;
            }

            string sourceKey = GetSourceKey(source, sourcePath);
            Material existing = FindGeneratedMaterial(sourceKey);
            if (existing != null)
            {
                SetDoubleSided(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            EnsureFolder(GeneratedMaterialsRoot);
            string modelFolder = GeneratedMaterialsRoot + "/" + SanitizeName(Path.GetFileNameWithoutExtension(sourcePath));
            EnsureFolder(modelFolder);
            string materialName = "MAT_RhinoDS_" + SanitizeName(source.name) + "_" + ShortHash(sourceKey);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(modelFolder + "/" + materialName + ".mat");

            // Instantiate 是 Unity 的序列化复制：Shader、纹理引用、颜色、金属度、光滑度、关键字等均保留。
            Material copy = new Material(source) { name = Path.GetFileNameWithoutExtension(assetPath) };
            SetDoubleSided(copy);
            AssetDatabase.CreateAsset(copy, assetPath);
            AssetImporter.GetAtPath(assetPath).userData = MarkerPrefix + sourceKey;
            createdCount++;
            return copy;
        }

        private static void SetDoubleSided(Material material)
        {
            if (material == null) return;

            // URP/Lit 的标准属性是 _Cull；兼容使用 _CullMode 的自定义 URP Shader。
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            if (material.HasProperty("_CullMode")) material.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
            material.doubleSidedGI = true;
        }

        private static Material FindGeneratedMaterial(string sourceKey)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { GeneratedMaterialsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer != null && string.Equals(importer.userData, MarkerPrefix + sourceKey, StringComparison.Ordinal))
                    return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            return null;
        }

        private static bool IsGeneratedMaterial(Material material)
        {
            string path = AssetDatabase.GetAssetPath(material);
            AssetImporter importer = string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path);
            return importer != null && importer.userData.StartsWith(MarkerPrefix, StringComparison.Ordinal);
        }

        private static string GetSourceKey(Material material, string assetPath)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long localId))
                return guid + ":" + localId;
            return assetPath + ":" + material.name;
        }

        private static IEnumerable<string> GetSelectedFbxPaths()
        {
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (IsFbx(path)) yield return path;
            }
        }

        private static IEnumerable<string> GetSelectedFolderPaths()
        {
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path)) yield return path;
            }
        }

        private static IEnumerable<string> FindFbxPathsInFolder(string folderPath)
        {
            return AssetDatabase.FindAssets("t:Model", new[] { folderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsFbx);
        }

        private static bool IsFbx(string path)
        {
            return !string.IsNullOrEmpty(path) && string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException("无法创建材质目录 / Invalid material folder: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string SanitizeName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string result = string.IsNullOrWhiteSpace(value) ? "Material" : value;
            foreach (char character in invalid) result = result.Replace(character, '_');
            return result.Replace(' ', '_');
        }

        private static string ShortHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value) hash = (hash ^ character) * 16777619;
                return hash.ToString("X8");
            }
        }
    }

    /// <summary>SourceModels 目录中新导入的 FBX 会自动走同一套外置材质流程。</summary>
    internal sealed class RhinoFbxDoubleSidedMaterialPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> PendingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool queued;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (RhinoFbxDoubleSidedMaterials.ShouldAutoProcess(path)) PendingPaths.Add(path);
            }

            if (PendingPaths.Count == 0 || queued) return;
            queued = true;
            EditorApplication.delayCall += ProcessPending;
        }

        private static void ProcessPending()
        {
            queued = false;
            string[] paths = PendingPaths.ToArray();
            PendingPaths.Clear();
            foreach (string path in paths) RhinoFbxDoubleSidedMaterials.ProcessImportedFbx(path);
        }
    }

    /// <summary>
    /// 给 Rhino 导入的深色贴图提供可读的冷灰环境光。
    /// 只创建/更新本工具名下的灯光对象和当前场景的环境光，不会修改任何模型、Prefab 或 Transform。
    /// </summary>
    public static class RhinoManorLighting
    {
        private const string MenuRoot = "庄园/美术/庄园场景灯光/";
        private const string RootName = "Lighting_RhinoManor_冷灰补光";
        private const string KeyName = "MoonKey_冷灰主光";
        private const string FillName = "SkyFill_冷灰补光";

        [MenuItem(MenuRoot + "修复当前场景的主楼过黑（冷灰补光）")]
        public static void ConfigureCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("庄园场景灯光", "没有可编辑的已打开场景。", "知道了");
                return;
            }

            GameObject root = FindRoot(scene);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create Rhino manor lighting");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            Light key = GetOrCreateLight(root.transform, KeyName);
            key.type = LightType.Directional;
            key.color = new Color(0.57f, 0.67f, 0.76f, 1f);
            // Rhino 贴图本身偏深，较弱的实时光会把木材压成纯黑；
            // 提高到可读但仍保持夜间冷灰感的强度。
            key.intensity = 1.65f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.70f;
            key.shadowBias = 0.05f;
            key.transform.localPosition = Vector3.zero;
            key.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);

            Light fill = GetOrCreateLight(root.transform, FillName);
            fill.type = LightType.Directional;
            fill.color = new Color(0.48f, 0.60f, 0.67f, 1f);
            fill.intensity = 0.52f;
            fill.shadows = LightShadows.None;
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localRotation = Quaternion.Euler(28f, 152f, 0f);

            // 平坦环境光确保深色 Rhino BaseColor 仍能读出木材和窗框层次；
            // 它不替换或删除原有油灯，因此大门的暖光仍是局部焦点。
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.31f, 0.38f, 0.42f, 1f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.reflectionIntensity = 0.8f;

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("[Manor][Lighting] 已加入冷灰主光、冷灰补光和环境光。建筑/桥/大门的位置与缩放均未修改。");
            EditorUtility.DisplayDialog(
                "庄园场景灯光",
                "已补上冷灰环境光和两盏方向光。\n\n现在看主楼，黑色区域应能看到木材和窗框细节。满意后按 Ctrl+S 保存场景。\n\n本操作没有移动、缩放或替换任何建筑、木桥和大门。",
                "知道了");
        }

        private static GameObject FindRoot(Scene scene)
        {
            return scene.GetRootGameObjects().FirstOrDefault(item => item != null && item.name == RootName);
        }

        private static Light GetOrCreateLight(Transform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            if (child != null)
            {
                Light existing = child.GetComponent<Light>();
                if (existing != null) return existing;
            }

            GameObject lightObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Rhino manor light");
            lightObject.transform.SetParent(parent, false);
            return lightObject.AddComponent<Light>();
        }
    }

    /// <summary>
    /// 将当前打开场景中 Unity 用洋红色显示的失效 Shader 材质，转换为安全的 URP/Lit 副本。
    /// 只替换失效材质槽位；不修改网格、Transform、FBX 或正常材质。
    /// </summary>
    public static class ManorMissingShaderRepair
    {
        private const string MenuRoot = "庄园/美术/庄园场景修复/";
        private const string RepairMaterialsRoot = "Assets/_Project/Art/Materials/RepairedMissingShaders";

        [MenuItem(MenuRoot + "修复当前场景的粉色缺失材质")]
        public static void RepairCurrentScene()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("修复粉色材质", "没有找到 URP/Lit Shader，无法安全修复。", "知道了");
                return;
            }

            int repairedSlots = 0;
            int createdMaterials = 0;
            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] original = renderer.sharedMaterials;
                    if (original == null || original.Length == 0) continue;
                    Material[] updated = null;
                    for (int index = 0; index < original.Length; index++)
                    {
                        Material source = original[index];
                        if (!IsMissingShaderMaterial(source)) continue;
                        if (updated == null) updated = (Material[])original.Clone();
                        if (!replacements.TryGetValue(source, out Material repaired))
                        {
                            repaired = CreateUrpReplacement(source, urpLit);
                            replacements.Add(source, repaired);
                            createdMaterials++;
                        }
                        updated[index] = repaired;
                        repairedSlots++;
                    }
                    if (updated == null) continue;
                    Undo.RecordObject(renderer, "Repair missing Shader material");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (repairedSlots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = repairedSlots > 0
                ? $"已修复 {repairedSlots} 个粉色材质槽位，创建 {createdMaterials} 个 URP 材质副本。\n\n模型的位置、缩放和网格没有修改。"
                : "当前场景没有检测到 Unity 的粉色缺失 Shader 材质。";
            Debug.Log("[Manor][MissingShader] " + message);
            EditorUtility.DisplayDialog("修复粉色材质", message, "完成");
        }

        /// <summary>
        /// The selected object is known visually to be pink, but Unity does not always expose
        /// that state as Hidden/InternalErrorShader.  This deliberate one-object recovery
        /// replaces every material slot below the selected root with safe URP/Lit copies while
        /// preserving each source BaseMap/BaseColor where readable.
        /// </summary>
        [MenuItem(MenuRoot + "修复当前选中的粉色模型（强制 URP）", true)]
        private static bool CanRepairSelectedPinkModel()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem(MenuRoot + "修复当前选中的粉色模型（强制 URP）")]
        public static void RepairSelectedPinkModel()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("修复选中粉色模型", "没有找到 URP/Lit Shader。", "知道了");
                return;
            }

            int slots = 0;
            int materials = 0;
            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] original = renderer.sharedMaterials;
                    if (original == null || original.Length == 0) continue;
                    Material[] updated = (Material[])original.Clone();
                    bool changed = false;
                    for (int index = 0; index < original.Length; index++)
                    {
                        Material source = original[index];
                        if (!replacements.TryGetValue(source, out Material repaired))
                        {
                            repaired = CreateUrpReplacement(source, urpLit);
                            replacements.Add(source, repaired);
                            materials++;
                        }
                        updated[index] = repaired;
                        slots++;
                        changed = true;
                    }
                    if (!changed) continue;
                    Undo.RecordObject(renderer, "Force repair selected pink model");
                    renderer.sharedMaterials = updated;
                    EditorUtility.SetDirty(renderer);
                }
            }
            if (slots > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            string message = slots > 0
                ? $"已把当前选中模型的 {slots} 个材质槽位替换为 URP 材质副本（新建 {materials} 个材质）。\n\n模型网格、位置、旋转和缩放没有修改。"
                : "当前选中物体下面没有可修复的 Renderer。";
            Debug.Log("[Manor][MissingShader] " + message);
            EditorUtility.DisplayDialog("修复选中粉色模型", message, "完成");
        }

        private static bool IsMissingShaderMaterial(Material material)
        {
            return material != null && (material.shader == null || material.shader.name == "Hidden/InternalErrorShader");
        }

        private static Material CreateUrpReplacement(Material source, Shader urpLit)
        {
            EnsureRepairFolder(RepairMaterialsRoot);
            Texture mainTexture = source != null && source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source == null ? null : source.mainTexture;
            Color color = source != null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source == null ? Color.white : source.color;
            Material repaired = new Material(urpLit)
            {
                name = "MAT_Repaired_" + SanitizeMaterialName(source == null ? "MissingMaterial" : source.name)
            };
            repaired.SetColor("_BaseColor", color);
            repaired.SetTexture("_BaseMap", mainTexture);
            repaired.SetFloat("_Cull", (float)CullMode.Off);
            string path = AssetDatabase.GenerateUniqueAssetPath(RepairMaterialsRoot + "/" + repaired.name + ".mat");
            AssetDatabase.CreateAsset(repaired, path);
            return repaired;
        }

        private static void EnsureRepairFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException("无法创建材质目录: " + path);
            EnsureRepairFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string SanitizeMaterialName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Material" : value;
            foreach (char character in Path.GetInvalidFileNameChars()) result = result.Replace(character, '_');
            return result.Replace(' ', '_');
        }
    }

}
#endif

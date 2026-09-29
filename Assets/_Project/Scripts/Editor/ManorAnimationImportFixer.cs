#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Manor.Editor
{
    public static class ManorAnimationImportFixer
    {
        private static readonly string[] Paths =
        {
            "Assets/_Project/Art/Characters/StagedRigs/Animations/Butcher_ProvisionalRig_Animations.fbx",
            "Assets/_Project/Art/Characters/StagedRigs/Animations/GhostChild_ProvisionalRig_Animations.fbx"
        };

        [MenuItem("庄园/自动化/修复临时角色动画剪辑名")]
        public static void ConfigureProvisionalClips()
        {
            foreach (string path in Paths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing animation importer: " + path);
                ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
                if (defaults == null || defaults.Length < 2)
                    throw new InvalidOperationException("Expected Idle and Walk takes in: " + path);

                ModelImporterClipAnimation idle = Find(defaults, "Idle") ?? defaults[0];
                ModelImporterClipAnimation walk = Find(defaults, "Walk") ?? defaults[1];
                idle.name = "Idle";
                idle.loopTime = true;
                walk.name = "Walk";
                walk.loopTime = true;
                importer.clipAnimations = new[] { idle, walk };
                importer.importAnimation = true;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Manor][Animations] Explicit Idle/Walk clips configured.");
        }

        private static ModelImporterClipAnimation Find(ModelImporterClipAnimation[] clips, string value)
        {
            return clips.FirstOrDefault(clip => clip.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || clip.takeName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
#endif

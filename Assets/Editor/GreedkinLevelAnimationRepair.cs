using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the six Greedkin variants aligned with their corresponding sprite-sheet rows.
/// Each row is one level.  Frames are read from left to right, rather than by their
/// generated name, because empty cells in the Run sheet are intentionally not sliced.
/// </summary>
internal static class GreedkinLevelAnimationRepair
{
    private const string AnimationRoot = "Assets/Animations/Enemies/Greedkin";
    private const string SessionKey = "TinyKingdom.GreedkinLevelAnimationRepair.Completed";

    private static readonly AnimationSource[] Sources =
    {
        new("Attack", "Assets/Dai/Attacks_GreedkinEnemy.png"),
        new("Die", "Assets/Dai/Die_Enemy.png"),
        new("Idle", "Assets/Dai/Idle_GreedkinEnemy.png"),
        new("Run", "Assets/Dai/Run_GreedkinEnemy1.png"),
        new("Walk", "Assets/Dai/Walk_GreedkinEnemy.png"),
    };

    [InitializeOnLoadMethod]
    private static void RepairOnceAfterImport()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += RebuildAndRename;
    }

    [MenuItem("Tiny Kingdom/Greedkin/Rebuild Level Animation Clips")]
    private static void RebuildAndRename()
    {
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int level = 1; level <= 6; level++)
            {
                string oldFolder = $"{AnimationRoot}/Greedkin_{level:00}";
                string levelFolder = $"{AnimationRoot}/Level_{level:00}";
                MoveFolderIfNeeded(oldFolder, levelFolder);

                foreach (AnimationSource source in Sources)
                {
                    AnimationClip clip = LoadClip(levelFolder, level, source.State);
                    if (clip == null)
                    {
                        Debug.LogError($"Greedkin level {level}: missing {source.State} animation clip.");
                        continue;
                    }

                    Sprite[] frames = GetLevelFrames(source, level);
                    if (frames.Length == 0)
                    {
                        Debug.LogError($"Greedkin level {level}: no {source.State} sprites were found.");
                        continue;
                    }

                    BuildClip(clip, frames);
                    RenameAssetIfNeeded(AssetDatabase.GetAssetPath(clip), $"Greedkin_Level_{level:00}_{source.State}");
                }

                AnimatorControllerRename(levelFolder, level);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("Greedkin animations rebuilt: each level now uses only the sprites from its matching row.");
    }

    private static void MoveFolderIfNeeded(string oldFolder, string levelFolder)
    {
        if (AssetDatabase.IsValidFolder(levelFolder) || !AssetDatabase.IsValidFolder(oldFolder))
        {
            return;
        }

        string error = AssetDatabase.MoveAsset(oldFolder, levelFolder);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Could not rename '{oldFolder}': {error}");
        }
    }

    private static AnimationClip LoadClip(string folder, int level, string state)
    {
        string oldName = $"Greedkin_{level:00}_{state}.anim";
        string newName = $"Greedkin_Level_{level:00}_{state}.anim";
        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{folder}/{newName}")
               ?? AssetDatabase.LoadAssetAtPath<AnimationClip>($"{folder}/{oldName}");
    }

    private static Sprite[] GetLevelFrames(AnimationSource source, int level)
    {
        string prefix = $"Greedkin_{level:00}_{source.State}_";
        return AssetDatabase.LoadAllAssetsAtPath(source.Path)
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();
    }

    private static void BuildClip(AnimationClip clip, IReadOnlyList<Sprite> frames)
    {
        const float framesPerSecond = 12f;
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int frame = 0; frame < frames.Count; frame++)
        {
            keyframes[frame] = new ObjectReferenceKeyframe
            {
                time = frame / framesPerSecond,
                value = frames[frame],
            };
        }

        clip.frameRate = framesPerSecond;
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        EditorUtility.SetDirty(clip);
    }

    private static void AnimatorControllerRename(string folder, int level)
    {
        string oldPath = $"{folder}/Greedkin_{level:00}.controller";
        string newPath = $"{folder}/Greedkin_Level_{level:00}.controller";
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(newPath) != null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(oldPath) != null)
        {
            RenameAssetIfNeeded(oldPath, $"Greedkin_Level_{level:00}");
        }
    }

    private static void RenameAssetIfNeeded(string path, string newName)
    {
        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null || asset.name == newName)
        {
            return;
        }

        string error = AssetDatabase.RenameAsset(path, newName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Could not rename '{path}': {error}");
        }
    }

    private readonly struct AnimationSource
    {
        public readonly string State;
        public readonly string Path;

        public AnimationSource(string state, string path)
        {
            State = state;
            Path = path;
        }
    }
}

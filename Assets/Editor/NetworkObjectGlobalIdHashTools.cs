#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// NGO's NetworkObject.OnValidate() skips recomputing GlobalObjectIdHash while the Editor is in
// (or transitioning into/out of) Play Mode, so GameObjects duplicated at the wrong moment silently
// keep their source's hash. NGO requires every scene-placed NetworkObject to have a unique hash,
// so duplicates collide at scene-load time. This tool finds and fixes those collisions.
//
// Two distinct duplicate shapes exist, needing two different DETECTION strategies, but both are
// FIXED the same way (see TryFix below):
//
//  - Top-level scene prefab instances (e.g. Strawberry placed directly in the scene): each has its
//    own separate PrefabInstance materialized in the scene file, so comparing live GlobalObjectIdHash
//    values reliably finds duplicates.
//
//  - Prefabs nested INSIDE another prefab (e.g. a fruit nested inside its branch/tree container),
//    placed multiple times via the outer container (e.g. Pomegranate nested in PomegranateBranch,
//    placed 6x in the scene): there is only ONE PrefabInstance connecting the nested prefab to its
//    container, baked once inside the container prefab's own asset file and shared identically by
//    every scene placement — there is no per-placement anchor for OnValidate's normal
//    RecordPrefabInstancePropertyModifications to attach an override to. OnValidate still computes
//    an in-memory value per placement when the scene loads, so live-hash comparison can look
//    transiently unique each Editor session (which previously made this tool wrongly report "no
//    duplicates") while what's actually saved to disk never changes and keeps colliding. Detection
//    for this shape is STRUCTURAL instead: any two nested (non-outermost) NetworkObjects that
//    resolve to the same source prefab asset path are a duplicate risk regardless of their current
//    live hash value.
//
// FIX (both shapes): assign a fresh unique hash to the live object, then explicitly attach it as a
// property override on the OUTERMOST scene prefab instance's modification list via
// PrefabUtility.SetPropertyModifications — the same mechanism Unity uses when you override any
// property of a nested prefab from the Inspector (the little "Overrides" list), which correctly
// supports targeting components arbitrarily deep in a nested prefab hierarchy. This fully preserves
// every prefab link (container AND nested prefab) for every copy — no data loss, nothing destroyed,
// nothing unpacked. Not undoable via Ctrl+Z (Unity does not track Undo for this API), so the tool
// insists on Edit Mode and tells the user to save immediately after running Fix.
public static class NetworkObjectGlobalIdHashTools
{
    private class ScanResult
    {
        public List<List<NetworkObject>> TopLevelDuplicateGroups = new List<List<NetworkObject>>();
        public List<List<NetworkObject>> NestedDuplicateGroups = new List<List<NetworkObject>>();

        public bool IsEmpty => TopLevelDuplicateGroups.Count == 0 && NestedDuplicateGroups.Count == 0;
    }

    [MenuItem("Tools/NGO/Report Duplicate GlobalObjectIdHash In Open Scene")]
    public static void ReportDuplicates()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var result = Scan(new[] { scene });

        if (result.IsEmpty)
        {
            Debug.Log($"[NetworkObjectGlobalIdHashTools] No duplicate GlobalObjectIdHash values found in '{scene.name}'.");
            return;
        }

        Debug.LogWarning(DescribeScanResult(result, scene.name));
    }

    [MenuItem("Tools/NGO/Fix Duplicate GlobalObjectIdHash (Reinstantiate Prefabs)")]
    public static void FixDuplicates()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Cannot fix in Play Mode",
                "Exit Play Mode first. NetworkObject only recomputes GlobalObjectIdHash while the Editor is in Edit Mode.",
                "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var summary = FixDuplicatesInScene(scene);
        Debug.Log(summary);
        EditorUtility.DisplayDialog("Fix Duplicate GlobalObjectIdHash", summary, "OK");
    }

    private static string DescribeScanResult(ScanResult result, string sceneName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[NetworkObjectGlobalIdHashTools] Duplicate GlobalObjectIdHash risk found in '{sceneName}':");
        foreach (var group in result.TopLevelDuplicateGroups)
        {
            sb.AppendLine($"  Hash {group[0].PrefabIdHash} (top-level, {group.Count} instances):");
            foreach (var networkObject in group)
            {
                sb.AppendLine($"    - {GetHierarchyPath(networkObject.transform)}");
            }
        }
        foreach (var group in result.NestedDuplicateGroups)
        {
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(group[0].gameObject);
            sb.AppendLine($"  Nested prefab '{assetPath}' ({group.Count} placements share one un-persistable identity):");
            foreach (var networkObject in group)
            {
                sb.AppendLine($"    - {GetHierarchyPath(networkObject.transform)}");
            }
        }
        return sb.ToString();
    }

    private static string FixDuplicatesInScene(Scene scene)
    {
        var result = Scan(new[] { scene });

        if (result.IsEmpty)
        {
            return $"No duplicate GlobalObjectIdHash values found in '{scene.name}'.";
        }

        // Every hash currently in use anywhere in the scene, so freshly-generated replacements can't
        // accidentally collide with an existing (or another newly-assigned) value.
        var usedHashes = new HashSet<uint>(
            UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(no => no.gameObject.scene == scene)
                .Select(no => no.PrefabIdHash));

        int fixedCount = 0;
        int skippedCount = 0;
        var skippedReasons = new List<string>();

        foreach (var group in result.TopLevelDuplicateGroups.Concat(result.NestedDuplicateGroups))
        {
            // Keep the first placement in each group untouched; give every other placement its own
            // fresh, independent hash.
            for (int i = 1; i < group.Count; i++)
            {
                var duplicate = group[i];
                if (TryFix(duplicate, usedHashes, out string skipReason))
                {
                    fixedCount++;
                }
                else
                {
                    skippedCount++;
                    skippedReasons.Add($"{GetHierarchyPath(duplicate.transform)}: {skipReason}");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        sb.AppendLine($"Fixed {fixedCount} duplicate object(s) in '{scene.name}'.");
        sb.AppendLine($"Skipped {skippedCount} object(s).");
        foreach (var reason in skippedReasons)
        {
            sb.AppendLine($"  - {reason}");
        }
        if (fixedCount > 0)
        {
            sb.AppendLine("This is NOT tracked by Editor Undo (Ctrl+Z will not revert it) — make sure you have a git " +
                "commit/stash to fall back to.");
        }
        sb.AppendLine("Scene marked dirty. Review the Hierarchy, then SAVE THE SCENE NOW (Ctrl+S) " +
            "— none of this is real until it's saved to disk.");
        return sb.ToString();
    }

    // Assigns a fresh unique GlobalObjectIdHash to the live object, then records it as an explicit
    // property override on the outermost scene prefab instance — the same mechanism Unity itself
    // uses for overriding any property of a nested prefab from the Inspector. Works identically for
    // top-level and arbitrarily-nested NetworkObjects, and never destroys or unpacks anything, so
    // every existing prefab link (container and nested) and every other existing override survives
    // untouched.
    private static bool TryFix(NetworkObject networkObject, HashSet<uint> usedHashes, out string skipReason)
    {
        var gameObject = networkObject.gameObject;

        if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            skipReason = "not part of a prefab instance, skipped rather than touched mechanically.";
            return false;
        }

        var outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
        if (outermostRoot == null)
        {
            skipReason = "could not resolve an outermost prefab instance root to attach an override to.";
            return false;
        }

        var serializedObject = new SerializedObject(networkObject);
        var hashProperty = serializedObject.FindProperty("GlobalObjectIdHash");
        if (hashProperty == null)
        {
            skipReason = "could not find the serialized GlobalObjectIdHash property.";
            return false;
        }

        uint newHash = GenerateUniqueHash(usedHashes);
        hashProperty.intValue = unchecked((int)newHash);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        var existingMods = PrefabUtility.GetPropertyModifications(outermostRoot) ?? Array.Empty<PropertyModification>();
        var withoutStaleHashMod = existingMods.Where(m => !(ReferenceEquals(m.target, networkObject) && m.propertyPath == "GlobalObjectIdHash"));
        var newMod = new PropertyModification
        {
            target = networkObject,
            propertyPath = "GlobalObjectIdHash",
            value = newHash.ToString(),
            objectReference = null,
        };
        PrefabUtility.SetPropertyModifications(outermostRoot, withoutStaleHashMod.Concat(new[] { newMod }).ToArray());

        skipReason = null;
        return true;
    }

    private static uint GenerateUniqueHash(HashSet<uint> usedHashes)
    {
        uint candidate;
        do
        {
            unchecked
            {
                candidate = (uint)Guid.NewGuid().GetHashCode();
            }
        } while (candidate == 0 || usedHashes.Contains(candidate));

        usedHashes.Add(candidate);
        return candidate;
    }

    // Classifies every NetworkObject in the given scenes by its prefab nesting shape, then applies
    // the detection strategy appropriate to each shape (see class-level comment).
    private static ScanResult Scan(IEnumerable<Scene> scenes)
    {
        var sceneSet = new HashSet<Scene>(scenes);
        var all = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(no => sceneSet.Contains(no.gameObject.scene));

        var topLevel = new List<NetworkObject>();
        var nested = new List<NetworkObject>();

        foreach (var no in all)
        {
            var go = no.gameObject;
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
            {
                continue;
            }

            if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go)
            {
                topLevel.Add(no);
                continue;
            }

            if (PrefabUtility.GetNearestPrefabInstanceRoot(go) == go)
            {
                nested.Add(no);
            }
            // Anything that's part of a prefab instance but neither the outermost nor a nested
            // instance root isn't a shape this tool acts on.
        }

        var result = new ScanResult();

        var topLevelGroups = new Dictionary<uint, List<NetworkObject>>();
        foreach (var no in topLevel)
        {
            if (!topLevelGroups.TryGetValue(no.PrefabIdHash, out var list))
            {
                list = new List<NetworkObject>();
                topLevelGroups[no.PrefabIdHash] = list;
            }
            list.Add(no);
        }
        result.TopLevelDuplicateGroups = topLevelGroups.Values.Where(g => g.Count > 1).ToList();

        // Structural grouping for nested instances: two placements collide if they resolve to the
        // same source prefab asset, regardless of what their current live hash happens to read.
        var nestedGroups = new Dictionary<string, List<NetworkObject>>();
        foreach (var no in nested)
        {
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(no.gameObject) ?? "<unresolved>";
            if (!nestedGroups.TryGetValue(assetPath, out var list))
            {
                list = new List<NetworkObject>();
                nestedGroups[assetPath] = list;
            }
            list.Add(no);
        }
        result.NestedDuplicateGroups = nestedGroups.Values.Where(g => g.Count > 1).ToList();

        return result;
    }

    private static Scene FindLoadedScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path == path)
            {
                return scene;
            }
        }
        return default;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        var current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    // Guard 1: refuse to enter Play Mode while the currently loaded scenes contain duplicate
    // GlobalObjectIdHash values, so the collision is caught locally instead of surfacing later.
    [InitializeOnLoad]
    private static class PlayModeGuard
    {
        static PlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            var scenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }

            var result = Scan(scenes);
            if (result.IsEmpty)
            {
                return;
            }

            EditorApplication.isPlaying = false;

            var sb = new StringBuilder();
            sb.AppendLine("Entering Play Mode was cancelled: duplicate GlobalObjectIdHash values were found.");
            sb.AppendLine("Run Tools > NGO > Fix Duplicate GlobalObjectIdHash (Reinstantiate Prefabs) first, then SAVE THE SCENE.");
            sb.AppendLine();
            sb.Append(DescribeScanResult(result, "loaded scenes"));

            Debug.LogError(sb.ToString());
            EditorUtility.DisplayDialog("Duplicate GlobalObjectIdHash detected", sb.ToString(), "OK");
        }
    }

    // Guard 2: fail the build loudly if any scene in Build Settings has duplicate
    // GlobalObjectIdHash values, instead of shipping a build that throws at runtime.
    public class BuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var problems = new StringBuilder();

            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled)
                {
                    continue;
                }

                // If the scene is already loaded (e.g. it's the active scene), scan it in place
                // rather than opening/closing it — avoids disturbing other open scenes or unsaved
                // multi-scene setups.
                var alreadyLoaded = FindLoadedScene(buildScene.path);
                var scene = alreadyLoaded.IsValid() ? alreadyLoaded : EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);

                var result = Scan(new[] { scene });
                if (!result.IsEmpty)
                {
                    problems.AppendLine($"Scene '{buildScene.path}':");
                    problems.Append(DescribeScanResult(result, buildScene.path));
                }

                if (!alreadyLoaded.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (problems.Length > 0)
            {
                throw new BuildFailedException(
                    "Duplicate GlobalObjectIdHash values found in build scenes:\n" + problems +
                    "\nRun Tools > NGO > Fix Duplicate GlobalObjectIdHash (Reinstantiate Prefabs) on each affected scene before building.");
            }
        }
    }
}
#endif

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Collections.Generic;

public static class HierarchyExporter
{
    // Stored as a semicolon-delimited list of project-relative folders (e.g., "Assets/Prefabs;Assets/MyPrefabs")
    private const string PrefabFoldersPrefsKey = "HierarchyExporter.PrefabFolders";

    [MenuItem("Tools/Export Hierarchy/Export LOADED Scenes + Prefabs (Full Serialized)")]
    public static void ExportLoadedScenesPlusPrefabsFull()
    {
        var pkg = BuildBasePackage();
        pkg.options.includeLoadedScenes = true;
        pkg.options.includePrefabs = true;

        // Ensure prefab folders are configured (required for prefab export).
        EnsurePrefabFoldersConfiguredOrAbort(out bool abort);
        if (abort) return;

        ExportLoadedScenesInto(pkg);
        ExportPrefabsInto(pkg);

        WriteJson(pkg, $"SceneHierarchy_LoadedPlusPrefabs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
    }

    [MenuItem("Tools/Export Hierarchy/Configure Prefab Folders...")]
    public static void ConfigurePrefabFoldersMenu()
    {
        ConfigurePrefabFoldersInteractive();
    }

    // ----------------------------
    // Prefab folders configuration
    // ----------------------------

    private static string[] GetPrefabFolders()
    {
        string raw = EditorPrefs.GetString(PrefabFoldersPrefsKey, "");
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();

        var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim().Replace("\\", "/");

        return parts;
    }

    private static void SetPrefabFolders(IEnumerable<string> folders)
    {
        EditorPrefs.SetString(PrefabFoldersPrefsKey, string.Join(";", folders));
    }

    private static void EnsurePrefabFoldersConfiguredOrAbort(out bool abort)
    {
        abort = false;

        var current = GetPrefabFolders();
        if (current.Length > 0) return;

        int choice = EditorUtility.DisplayDialogComplex(
            "Prefab folders not configured",
            "This export includes prefabs. Please configure one or more folders under Assets/ that contain prefabs.\n\n" +
            "Example: Assets/Prefabs",
            "Configure Now",
            "Cancel Export",
            "Continue (no prefabs)"
        );

        // 0 = Configure Now, 1 = Cancel, 2 = Continue (no prefabs)
        if (choice == 1) { abort = true; return; }
        if (choice == 2) return;

        ConfigurePrefabFoldersInteractive();
    }

    private static void ConfigurePrefabFoldersInteractive()
    {
        var folders = new List<string>(GetPrefabFolders());

        bool clear = EditorUtility.DisplayDialog(
            "Configure Prefab Folders",
            folders.Count == 0
                ? "No prefab folders are currently set.\n\nAdd one or more folders under Assets/."
                : $"Currently exporting prefabs from:\n\n{string.Join("\n", folders)}\n\nDo you want to clear and reselect?",
            "Clear + Reselect",
            "Keep + Add"
        );

        if (clear) folders.Clear();

        while (true)
        {
            string abs = EditorUtility.OpenFolderPanel(
                "Select Prefab Folder (must be under Assets/)",
                Application.dataPath,
                ""
            );

            if (string.IsNullOrEmpty(abs)) break;

            string rel = FileUtil.GetProjectRelativePath(abs).Replace("\\", "/");
            if (string.IsNullOrEmpty(rel) || !rel.StartsWith("Assets/") || !AssetDatabase.IsValidFolder(rel))
            {
                EditorUtility.DisplayDialog("Invalid Folder",
                    "Please select a folder inside the project's Assets/ directory.",
                    "OK");
                continue;
            }

            if (!folders.Contains(rel))
                folders.Add(rel);

            if (!EditorUtility.DisplayDialog("Add Another Folder?",
                    $"Selected folders:\n\n{string.Join("\n", folders)}",
                    "Add Another",
                    "Done"))
                break;
        }

        if (folders.Count > 0)
        {
            SetPrefabFolders(folders);
            Debug.Log($"[HierarchyExporter] Prefab folders set:\n- {string.Join("\n- ", folders)}");
        }
        else
        {
            EditorPrefs.DeleteKey(PrefabFoldersPrefsKey);
            Debug.Log("[HierarchyExporter] Prefab folders cleared.");
        }
    }

    // ----------------------------
    // Export pipeline
    // ----------------------------

    private static HierarchyExportPackage BuildBasePackage()
    {
        return new HierarchyExportPackage
        {
            exportUtc = DateTime.UtcNow.ToString("o"),
            unityVersion = Application.unityVersion,
            productName = Application.productName,
            options = new ExportOptions
            {
                includeLoadedScenes = true,
                includePrefabs = true,

                // Sensible defaults; edit to taste
                maxPropertiesPerComponent = 4000,
                maxArrayElementsPerArray = 40,
                maxStringLength = 300
            },
            loadedScenes = new List<SceneHierarchy>(),
            prefabs = new List<PrefabHierarchy>()
        };
    }

    private static void ExportLoadedScenesInto(HierarchyExportPackage pkg)
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var sh = new SceneHierarchy
            {
                sceneName = scene.name,
                scenePath = scene.path,
                rootObjects = new List<GameObjectData>()
            };

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                sh.rootObjects.Add(ProcessGameObject(root, scene.name, parentPath: "", depth: 0, options: pkg.options));

            pkg.loadedScenes.Add(sh);
        }
    }

    private static void ExportPrefabsInto(HierarchyExportPackage pkg)
    {
        var folders = GetPrefabFolders();
        if (folders.Length == 0) return;

        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;

                var ph = new PrefabHierarchy
                {
                    prefabName = prefab.name,
                    assetPath = path,
                    assetGuid = guid,   // Unity asset GUID
                    isPrefab = true,
                    rootObject = ProcessGameObject(prefab, sceneName: "(PREFAB)", parentPath: "", depth: 0, options: pkg.options, isPrefabRoot: true)
                };

                pkg.prefabs.Add(ph);
            }
        }
    }

    private static void WriteJson(HierarchyExportPackage pkg, string fileName)
    {
        string outputPath = Path.Combine(Application.dataPath, "../" + fileName);
        File.WriteAllText(outputPath, JsonUtility.ToJson(pkg, true));
        Debug.Log($"[HierarchyExporter] Wrote export to: {outputPath}");
    }

    // ----------------------------
    // Object processing
    // ----------------------------

    private static GameObjectData ProcessGameObject(GameObject go, string sceneName, string parentPath, int depth, ExportOptions options, bool isPrefabRoot = false)
    {
        string currentPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

        var data = new GameObjectData
        {
            instanceId = go.GetInstanceID(),
            name = go.name,
            path = currentPath,
            sceneName = sceneName,

            tag = SafeTag(go),
            layer = LayerMask.LayerToName(go.layer),
            hideFlags = go.hideFlags.ToString(),
            activeSelf = go.activeSelf,
            activeInHierarchy = go.activeInHierarchy,
            isStatic = go.isStatic,

            siblingIndex = go.transform.GetSiblingIndex(),

            isPrefabInstance = !isPrefabRoot && PrefabUtility.IsPartOfPrefabInstance(go),
            prefabAssetPath = !isPrefabRoot ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) : "",
            prefabAssetGuid = "",

            depth = depth,

            components = new List<ComponentData>(),
            children = new List<GameObjectData>()
        };

        if (data.isPrefabInstance && !string.IsNullOrEmpty(data.prefabAssetPath))
            data.prefabAssetGuid = AssetDatabase.AssetPathToGUID(data.prefabAssetPath);

        // Transform
        var t = go.transform;
        data.transform = new TransformData
        {
            localPosition = t.localPosition,
            position = t.position,
            localEulerAngles = t.localEulerAngles,
            rotation = t.rotation,
            localScale = t.localScale
        };

        // Components (+ serialized settings)
        var comps = go.GetComponents<Component>();
        foreach (var comp in comps)
        {
            if (!comp) continue;
            bool truncated = false;
            var cd = BuildComponentData(comp, options, ref truncated);
            cd.truncatedProperties = truncated;
            data.components.Add(cd);
        }

        // Children
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i).gameObject;
            data.children.Add(ProcessGameObject(child, sceneName, currentPath, depth + 1, options, isPrefabRoot: false));
        }

        return data;
    }

    private static string SafeTag(GameObject go)
    {
        try { return go.tag; }
        catch { return "Untagged"; }
    }

    // ----------------------------
    // Component serialization
    // ----------------------------

    private static ComponentData BuildComponentData(Component comp, ExportOptions options, ref bool truncatedPropertiesFlag)
    {
        var type = comp.GetType();
        var cd = new ComponentData
        {
            instanceId = comp.GetInstanceID(),
            componentType = type.FullName,
            assemblyQualifiedName = type.AssemblyQualifiedName,
            isCustomScript = type.Namespace == null || !type.Namespace.StartsWith("UnityEngine"),
            fields = new List<FieldEntry>()
        };

        if (comp is Behaviour b) { cd.hasEnabledProperty = true; cd.enabled = b.enabled; }
        else if (comp is Renderer r) { cd.hasEnabledProperty = true; cd.enabled = r.enabled; }

        try
        {
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            int propCount = 0;
            var arrayElementCounts = new Dictionary<string, int>();

            bool enterChildren = true;
            if (!prop.Next(true)) return cd; // include non-visible properties too

            do
            {
                enterChildren = true;

                // Skip noisy self-link
                if (prop.name == "m_GameObject") { enterChildren = false; continue; }

                // Cap total properties per component
                if (propCount++ >= options.maxPropertiesPerComponent)
                {
                    truncatedPropertiesFlag = true;
                    break;
                }

                // Cap array/list element expansion per array
                if (TryGetArrayBasePath(prop.propertyPath, out var arrayBasePath))
                {
                    arrayElementCounts.TryGetValue(arrayBasePath, out int count);
                    count++;
                    arrayElementCounts[arrayBasePath] = count;

                    if (count > options.maxArrayElementsPerArray)
                    {
                        truncatedPropertiesFlag = true;
                        enterChildren = false; // don't dive into this element
                        continue;              // skip emitting this entry
                    }
                }

                var entry = new FieldEntry
                {
                    propertyPath = prop.propertyPath,
                    displayName = prop.displayName,
                    type = prop.propertyType.ToString()
                };

                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    entry.kind = FieldKind.ObjectReference;
                    entry.refInfo = BuildObjectRef(prop.objectReferenceValue);
                    entry.value = entry.refInfo != null ? entry.refInfo.name : "null";
                }
                else
                {
                    entry.kind = FieldKind.Value;
                    entry.value = StringifyValueCapped(prop, options.maxStringLength);
                }

                cd.fields.Add(entry);
            }
            while (prop.Next(enterChildren));
        }
        catch (Exception ex)
        {
            cd.fields.Add(new FieldEntry
            {
                propertyPath = "__error",
                displayName = "__error",
                type = "Exception",
                kind = FieldKind.Value,
                value = ex.Message
            });
        }

        return cd;
    }

    private static ObjectRef BuildObjectRef(UnityEngine.Object obj)
    {
        if (!obj) return null;

        var or = new ObjectRef
        {
            name = obj.name,
            type = obj.GetType().FullName,
            instanceId = obj.GetInstanceID(),
            assetPath = "",
            assetGuid = "",
            isAsset = false
        };

        // Persistent assets (Project window) have an asset path + GUID.
        if (EditorUtility.IsPersistent(obj))
        {
            or.assetPath = AssetDatabase.GetAssetPath(obj);
            or.assetGuid = string.IsNullOrEmpty(or.assetPath) ? "" : AssetDatabase.AssetPathToGUID(or.assetPath);
            or.isAsset = !string.IsNullOrEmpty(or.assetPath);
        }

        return or;
    }

    // Detects array/list paths like "myList.Array.data[3].field" -> base "myList"
    private static bool TryGetArrayBasePath(string propertyPath, out string arrayBasePath)
    {
        arrayBasePath = null;
        if (string.IsNullOrEmpty(propertyPath)) return false;

        int arrayIdx = propertyPath.IndexOf(".Array.data[", StringComparison.Ordinal);
        if (arrayIdx < 0) return false;

        arrayBasePath = propertyPath.Substring(0, arrayIdx);
        return true;
    }

    private static string StringifyValueCapped(SerializedProperty prop, int maxLen)
    {
        string s = StringifyValue(prop);
        if (string.IsNullOrEmpty(s)) return s;
        if (maxLen <= 0 || s.Length <= maxLen) return s;
        return s.Substring(0, maxLen) + "…(truncated)";
    }

    private static string StringifyValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
            case SerializedPropertyType.Float: return prop.floatValue.ToString();
            case SerializedPropertyType.String: return prop.stringValue ?? "";
            case SerializedPropertyType.Color: return prop.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue ? prop.objectReferenceValue.name : "null";
            case SerializedPropertyType.Enum:
                return prop.enumDisplayNames != null && prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length
                    ? prop.enumDisplayNames[prop.enumValueIndex]
                    : prop.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
            case SerializedPropertyType.Rect: return prop.rectValue.ToString();
            case SerializedPropertyType.Bounds: return prop.boundsValue.ToString();
            case SerializedPropertyType.Quaternion: return prop.quaternionValue.ToString();
            case SerializedPropertyType.AnimationCurve: return prop.animationCurveValue != null ? "[AnimationCurve]" : "null";
            case SerializedPropertyType.LayerMask: return prop.intValue.ToString();
            case SerializedPropertyType.Vector2Int: return prop.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int: return prop.vector3IntValue.ToString();
            case SerializedPropertyType.RectInt: return prop.rectIntValue.ToString();
            case SerializedPropertyType.BoundsInt: return prop.boundsIntValue.ToString();
            case SerializedPropertyType.ManagedReference:
                return prop.managedReferenceFullTypename ?? "[ManagedReference]";
            default:
                if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                    return $"[Array size={prop.arraySize}]";
                return "[Unsupported]";
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using System.IO;
using System.Collections.Generic;
using System;

public static class HierarchyExporter
{
    // Optional: where to scan prefabs when using "Export Hierarchy (All Loaded Scenes)"
    private static readonly string[] PrefabFolders = new[]
    {
        "Assets/Prefabs"
        // add more folders if needed
        // "Assets/UI",
        // "Assets/_Project/Prefabs"
    };

    [MenuItem("Tools/Export ACTIVE Scene Hierarchy (LLM Rich)")]
    public static void ExportActiveSceneHierarchyLLM()
    {
        var pkg = BuildBasePackage();

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("[HierarchyExporter] Active scene not loaded.");
            return;
        }

        pkg.activeSceneName = scene.name;
        pkg.activeScenePath = scene.path;
        pkg.notes = new[]
        {
            "Exported ACTIVE scene only (prevents wrong-scene exports).",
            $"ActiveScenePath: {scene.path}"
        };

        var sh = new SceneHierarchy { sceneName = scene.name };
        foreach (var root in scene.GetRootGameObjects())
            sh.rootObjects.Add(ProcessGameObject(root));

        pkg.scenes.Add(sh);

        string fileName = $"SceneHierarchy_{scene.name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string outputPath = Path.Combine(Application.dataPath, "../" + fileName);
        File.WriteAllText(outputPath, JsonUtility.ToJson(pkg, true));

        Debug.Log($"[HierarchyExporter] Wrote export to: {outputPath}");
    }

    [MenuItem("Tools/Export Hierarchy (LLM Rich)")]
    public static void ExportHierarchyLLM()
    {
        var pkg = BuildBasePackage();

        // Scenes (all loaded)
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var sh = new SceneHierarchy { sceneName = scene.name };
            foreach (var root in scene.GetRootGameObjects())
                sh.rootObjects.Add(ProcessGameObject(root));

            pkg.scenes.Add(sh);
        }

        // Prefabs
        foreach (string folder in PrefabFolders)
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
                    isPrefab = true,
                    rootObject = ProcessGameObject(prefab, parentPath: "", isPrefabRoot: true)
                };
                pkg.prefabs.Add(ph);
            }
        }

        string outputPath = Path.Combine(Application.dataPath, "../SceneHierarchy.json");
        File.WriteAllText(outputPath, JsonUtility.ToJson(pkg, true));
        Debug.Log($"[HierarchyExporter] Wrote export to: {outputPath}");
    }

    private static HierarchyExportPackage BuildBasePackage()
    {
        return new HierarchyExportPackage
        {
            unityVersion = Application.unityVersion,
            exportTimeUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private static GameObjectData ProcessGameObject(GameObject go, string parentPath = "", int depth = 0, bool isPrefabRoot = false)
    {
        string currentPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

        var data = new GameObjectData
        {
            name = go.name,
            path = currentPath,
            tag = SafeTag(go),
            layer = LayerMask.LayerToName(go.layer),

            isActiveSelf = go.activeSelf,
            isActiveInHierarchy = go.activeInHierarchy,
            isStatic = go.isStatic,

            isPrefabInstance = !isPrefabRoot && PrefabUtility.IsPartOfPrefabInstance(go),
            prefabAssetPath = !isPrefabRoot ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) : ""
        };

        data.depth = depth;

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

        // RectTransform (if present)
        var rt = go.GetComponent<RectTransform>();
        if (rt)
        {
            data.rectTransform = new RectTransformData
            {
                anchorMin = rt.anchorMin,
                anchorMax = rt.anchorMax,
                pivot = rt.pivot,
                anchoredPosition = rt.anchoredPosition,
                sizeDelta = rt.sizeDelta
            };
        }

        // Components
        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) continue;
            var cd = BuildComponentData(comp);
            data.components.Add(cd);
            UpdateHints(go, comp, cd, data.hints);
        }

        // Children
        foreach (Transform child in t)
            data.children.Add(ProcessGameObject(child.gameObject, currentPath, depth + 1, isPrefabRoot));

        // Finalize UI warnings
        var warnings = new List<string>();
        if (data.hints.hasCanvas && data.hints.canvasRenderMode == "ScreenSpaceCamera" && string.IsNullOrEmpty(data.hints.canvasWorldCamera))
            warnings.Add("Canvas is ScreenSpaceCamera but worldCamera is null.");

        if (data.hints.hasCanvas && !data.hints.hasGraphicRaycaster)
            warnings.Add("Canvas missing GraphicRaycaster—UI may not receive clicks.");

        if (data.hints.isButton && (data.hints.raycastsBlocked || !data.hints.buttonInteractable))
            warnings.Add("Button likely not clickable (CanvasGroup blocksRaycasts==false or Button.interactable==false).");

        data.hints.uiWarnings = warnings.ToArray();

        return data;
    }

    private static ComponentData BuildComponentData(Component comp)
    {
        var type = comp.GetType();
        var cd = new ComponentData
        {
            componentType = type.FullName,
            assemblyQualifiedName = type.AssemblyQualifiedName,
            isCustomScript = type.Namespace == null || !type.Namespace.StartsWith("UnityEngine")
        };

        // SerializedObject dump (readable)
        try
        {
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name == "m_GameObject") continue;

                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var obj = prop.objectReferenceValue;
                        cd.objectReferences[prop.displayName] = ObjectRefToString(obj);
                    }
                    else
                    {
                        cd.serializedFields[prop.displayName] = PropertyToString(prop);
                    }
                } while (prop.NextVisible(false));
            }
        }
        catch (Exception ex)
        {
            cd.serializedFields["__error"] = ex.Message;
        }

        // Targeted compact fields
        try
        {
            switch (comp)
            {
                case Canvas canvas:
                    cd.serializedFields["renderMode"] = canvas.renderMode.ToString();
                    cd.serializedFields["worldCamera"] = canvas.worldCamera ? canvas.worldCamera.name : "";
                    cd.serializedFields["sortingLayerName"] = canvas.sortingLayerName;
                    cd.serializedFields["sortingOrder"] = canvas.sortingOrder.ToString();
                    break;

                case CanvasScaler scaler:
                    cd.serializedFields["uiScaleMode"] = scaler.uiScaleMode.ToString();
                    cd.serializedFields["referenceResolution"] = scaler.referenceResolution.ToString();
                    cd.serializedFields["matchWidthOrHeight"] = scaler.matchWidthOrHeight.ToString();
                    break;

                case GraphicRaycaster gr:
                    cd.serializedFields["blockingObjects"] = gr.blockingObjects.ToString();
                    break;

                case CanvasGroup cg:
                    cd.serializedFields["alpha"] = cg.alpha.ToString();
                    cd.serializedFields["interactable"] = cg.interactable.ToString();
                    cd.serializedFields["blocksRaycasts"] = cg.blocksRaycasts.ToString();
                    break;

                case Image img:
                    cd.serializedFields["raycastTarget"] = img.raycastTarget.ToString();
                    cd.serializedFields["color"] = img.color.ToString();
                    break;

                case Button btn:
                    cd.serializedFields["interactable"] = btn.interactable.ToString();
                    cd.serializedFields["transition"] = btn.transition.ToString();
                    break;

                case TMP_Text tmp:
                    cd.serializedFields["text"] = tmp.text ?? "";
                    cd.serializedFields["fontSize"] = tmp.fontSize.ToString();
                    cd.serializedFields["raycastTarget"] = tmp.raycastTarget.ToString();
                    break;

                case Camera cam:
                    cd.serializedFields["orthographic"] = cam.orthographic.ToString();
                    cd.serializedFields["orthographicSize"] = cam.orthographicSize.ToString();
                    cd.serializedFields["fieldOfView"] = cam.fieldOfView.ToString();
                    cd.serializedFields["depth"] = cam.depth.ToString();
                    cd.serializedFields["cullingMask"] = MaskToLayerNames(cam.cullingMask);
                    cd.serializedFields["nearClipPlane"] = cam.nearClipPlane.ToString();
                    cd.serializedFields["farClipPlane"] = cam.farClipPlane.ToString();
                    break;

                case Renderer rend:
                    cd.serializedFields["sortingLayerName"] = rend.sortingLayerName;
                    cd.serializedFields["sortingOrder"] = rend.sortingOrder.ToString();
                    break;

                case Collider col:
                    cd.serializedFields["isTrigger"] = col.isTrigger.ToString();
                    break;

                case Rigidbody rb:
                    cd.serializedFields["mass"] = rb.mass.ToString();
                    cd.serializedFields["constraints"] = rb.constraints.ToString();
                    cd.serializedFields["useGravity"] = rb.useGravity.ToString();
                    break;

                case AudioSource audio:
                    cd.serializedFields["clip"] = audio.clip ? audio.clip.name : "";
                    cd.serializedFields["loop"] = audio.loop.ToString();
                    cd.serializedFields["playOnAwake"] = audio.playOnAwake.ToString();
                    cd.serializedFields["volume"] = audio.volume.ToString();
                    break;
            }
        }
        catch (Exception ex)
        {
            cd.serializedFields["__hintError"] = ex.Message;
        }

        return cd;
    }

    private static void UpdateHints(GameObject go, Component comp, ComponentData cd, LLMHints hints)
    {
        switch (comp)
        {
            case Canvas canvas:
                hints.hasCanvas = true;
                hints.canvasRenderMode = canvas.renderMode.ToString();
                hints.canvasWorldCamera = canvas.worldCamera ? canvas.worldCamera.name : "";
                break;

            case CanvasScaler scaler:
                hints.hasCanvasScaler = true;
                hints.scalerUiScaleMode = scaler.uiScaleMode.ToString();
                hints.scalerReferenceResolution = scaler.referenceResolution.ToString();
                hints.scalerMatchWidthOrHeight = scaler.matchWidthOrHeight;
                break;

            case GraphicRaycaster _:
                hints.hasGraphicRaycaster = true;
                break;

            case CanvasGroup cg:
                hints.raycastsBlocked = (cg.blocksRaycasts == false); // FIXED (was inverted in older exporter)
                hints.interactableFalse = (cg.interactable == false);
                break;

            case Button btn:
                hints.isButton = true;
                hints.buttonInteractable = btn.interactable;
                break;

            case Toggle _:
                hints.isToggle = true;
                break;

            case TMP_Text tmp:
                hints.isTMPText = true;
                hints.textSample = (tmp.text != null && tmp.text.Length > 64) ? tmp.text.Substring(0, 64) : (tmp.text ?? "");
                break;

            case Renderer rend:
                hints.isRenderer = true;
                hints.sortingLayer = rend.sortingLayerName;
                hints.sortingOrder = rend.sortingOrder;
                break;

            case Collider col:
                hints.hasCollider = true;
                hints.colliderIsTrigger = col.isTrigger;
                break;

            case Rigidbody _:
                hints.hasRigidbody = true;
                break;

            case Camera cam:
                hints.isCamera = true;
                hints.cameraDepth = cam.depth;
                hints.cameraCullingMask = MaskToLayerNames(cam.cullingMask);
                break;

            case AudioSource _:
                hints.hasAudioSource = true;
                break;
        }

        // Per-scene global presence check
        if (!hints.hasEventSystemInScene)
        {
            var es = GameObject.FindFirstObjectByType<EventSystem>();
            hints.hasEventSystemInScene = es != null && es.enabled;
        }
    }

    private static string SafeTag(GameObject go)
    {
        try { return go.tag; }
        catch { return "Untagged"; }
    }

    private static string ObjectRefToString(UnityEngine.Object obj)
    {
        if (obj == null) return "null";
        if (obj is GameObject gref) return GetPath(gref);
        if (obj is Component cref) return $"{GetPath(cref.gameObject)}:{cref.GetType().Name}";
        return obj.name;
    }

    private static string GetPath(GameObject go)
    {
        var stack = new List<string>();
        var t = go.transform;
        while (t != null)
        {
            stack.Add(t.name);
            if (t.parent == null) break;
            t = t.parent;
        }
        stack.Reverse();
        return string.Join("/", stack);
    }

    private static string MaskToLayerNames(int mask)
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                string n = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(n) ? $"Layer{i}" : n);
            }
        }
        return string.Join("|", names);
    }

    private static string PropertyToString(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
            case SerializedPropertyType.Float: return prop.floatValue.ToString();
            case SerializedPropertyType.String: return prop.stringValue ?? "";
            case SerializedPropertyType.Color: return prop.colorValue.ToString();
            case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
            case SerializedPropertyType.Quaternion: return prop.quaternionValue.ToString();
            case SerializedPropertyType.Rect: return prop.rectValue.ToString();
            case SerializedPropertyType.Enum:
                return (prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length)
                    ? prop.enumDisplayNames[prop.enumValueIndex]
                    : prop.enumValueIndex.ToString();
            case SerializedPropertyType.LayerMask: return prop.intValue.ToString();
            case SerializedPropertyType.AnimationCurve: return "[AnimationCurve]";
            default: return prop.displayName + " (unsupported)";
        }
    }
}

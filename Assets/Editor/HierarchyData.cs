using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schema v3.x (backward-compatible with your v2.1-llm fields).
/// NOTE: Unity JsonUtility does NOT serialize Dictionary. Keep dictionaries only for legacy.
/// Prefer List<FieldEntry> for serialized fields.
/// </summary>
[Serializable]
public class HierarchyExportPackage
{
    // ---- Legacy v2 fields (keep for compatibility) ----
    public string version = "3.2";
    public string unityVersion;
    public string exportTimeUtc;                 // legacy name
    public string activeSceneName;
    public string activeScenePath;
    public string[] notes = new[]
    {
        "This JSON is designed for AI troubleshooting.",
        "Includes non-visible SerializedProperties; arrays are capped and flagged.",
        "Asset references include GUID where available."
    };
    public List<SceneHierarchy> scenes = new List<SceneHierarchy>();   // legacy name
    public List<PrefabHierarchy> prefabs = new List<PrefabHierarchy>();

    // ---- Newer/expanded fields (used by updated exporter) ----
    public string exportUtc;                      // new name
    public string productName;
    public List<SceneHierarchy> loadedScenes = new List<SceneHierarchy>(); // new name
    public ExportOptions options = new ExportOptions();
}

[Serializable]
public class ExportOptions
{
    public bool includeLoadedScenes = true;
    public bool includePrefabs = true;

    public int maxPropertiesPerComponent = 4000;
    public int maxArrayElementsPerArray = 40;
    public int maxStringLength = 300;
}

[Serializable]
public class SceneHierarchy
{
    public string sceneName;
    public string scenePath;
    public List<GameObjectData> rootObjects = new List<GameObjectData>();
}

[Serializable]
public class PrefabHierarchy
{
    public string prefabName;
    public string assetPath;
    public string assetGuid;     // Unity asset GUID (from .meta)
    public bool isPrefab = true;
    public GameObjectData rootObject;
}

[Serializable]
public class GameObjectData
{
    public int instanceId;
    public string name;
    public string path;
    public string sceneName;

    public string tag;
    public string layer;
    public string hideFlags;

    // ---- Legacy names ----
    public bool isActiveSelf;
    public bool isActiveInHierarchy;

    // ---- New names (exporter uses these) ----
    public bool activeSelf;
    public bool activeInHierarchy;

    public bool isStatic;
    public int siblingIndex;

    public bool isPrefabInstance;
    public string prefabAssetPath;
    public string prefabAssetGuid;

    public TransformData transform;
    public RectTransformData rectTransform; // present when applicable

    public List<ComponentData> components = new List<ComponentData>();
    public List<GameObjectData> children = new List<GameObjectData>();

    // Hints (keep)
    public LLMHints hints = new LLMHints();

    // Exporter meta
    public bool truncatedChildren;
    public int depth;
}

[Serializable]
public class TransformData
{
    public Vector3 localPosition;
    public Vector3 position;
    public Vector3 localEulerAngles;
    public Quaternion rotation;
    public Vector3 localScale;
}

[Serializable]
public class RectTransformData
{
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 pivot;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
}

/// <summary>
/// Component data. Prefer fields (List<FieldEntry>) for JsonUtility compatibility.
/// </summary>
[Serializable]
public class ComponentData
{
    public int instanceId;
    public string componentType;
    public string assemblyQualifiedName;
    public bool isCustomScript;

    // Optional enabled flag (Behaviour/Renderer/etc.)
    public bool hasEnabledProperty;
    public bool enabled;

    // New: serialized fields (JsonUtility-friendly)
    public List<FieldEntry> fields = new List<FieldEntry>();
    public bool truncatedProperties;

    // ---- Legacy dictionaries (NOT serialized by JsonUtility) ----
    public Dictionary<string, string> serializedFields = new Dictionary<string, string>();
    public Dictionary<string, string> objectReferences = new Dictionary<string, string>();
}

public enum FieldKind { Value, ObjectReference }

[Serializable]
public class FieldEntry
{
    public string propertyPath;
    public string displayName;
    public string type;
    public FieldKind kind;
    public string value;

    public ObjectRef refInfo; // only when kind == ObjectReference
}

[Serializable]
public class ObjectRef
{
    public string name;
    public string type;
    public int instanceId;

    public bool isAsset;
    public string assetPath;
    public string assetGuid;
}

[Serializable]
public class LLMHints
{
    // UI
    public bool hasCanvas;
    public string canvasRenderMode; // Overlay / ScreenSpaceCamera / World
    public string canvasWorldCamera;
    public bool hasCanvasScaler;
    public string scalerUiScaleMode;
    public string scalerReferenceResolution;
    public float scalerMatchWidthOrHeight;
    public bool hasGraphicRaycaster;
    public bool raycastsBlocked;
    public bool interactableFalse;
    public bool hasEventSystemInScene;
    public string[] uiWarnings;

    // Interaction
    public bool isButton;
    public bool buttonInteractable;
    public bool isToggle;
    public bool toggleIsOn;

    // Camera/physics (kept from your v2 schema idea; safe defaults)
    public bool hasCamera;
    public bool hasRigidbody;
    public bool hasCollider;
}

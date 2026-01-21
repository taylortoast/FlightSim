using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HierarchyExportPackage
{
    public string version = "2.1-llm";
    public string unityVersion;
    public string exportTimeUtc;

    public string activeSceneName;
    public string activeScenePath;

    public string[] notes = new[]
    {
        "This JSON is designed for AI troubleshooting.",
        "LLMHints contain precomputed signals (UI/camera/physics).",
        "serializedFields are stringified for readability."
    };

    public List<SceneHierarchy> scenes = new List<SceneHierarchy>();
    public List<PrefabHierarchy> prefabs = new List<PrefabHierarchy>();
}

[Serializable]
public class SceneHierarchy
{
    public string sceneName;
    public List<GameObjectData> rootObjects = new List<GameObjectData>();
}

[Serializable]
public class PrefabHierarchy
{
    public string prefabName;
    public string assetPath;
    public bool isPrefab = true;
    public GameObjectData rootObject;
}

[Serializable]
public class GameObjectData
{
    public string name;
    public string path;
    public string tag;
    public string layer;

    public bool isActiveSelf;
    public bool isActiveInHierarchy;
    public bool isStatic;

    public bool isPrefabInstance;
    public string prefabAssetPath;

    public TransformData transform;
    public RectTransformData rectTransform; // present when applicable

    public List<ComponentData> components = new List<ComponentData>();
    public List<GameObjectData> children = new List<GameObjectData>();

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

[Serializable]
public class ComponentData
{
    public string componentType;
    public string assemblyQualifiedName;
    public bool isCustomScript;

    public Dictionary<string, string> serializedFields = new Dictionary<string, string>();
    public Dictionary<string, string> objectReferences = new Dictionary<string, string>(); // fieldName -> scene path or asset name
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
    public bool raycastsBlocked;    // CanvasGroup.blocksRaycasts == false
    public bool interactableFalse;  // CanvasGroup.interactable == false
    public bool hasEventSystemInScene;
    public string[] uiWarnings;

    // Interaction
    public bool isButton;
    public bool buttonInteractable;
    public bool isToggle;
    public bool isTMPText;
    public string textSample;

    // Rendering
    public bool isRenderer;
    public string sortingLayer;
    public int sortingOrder;

    // Physics
    public bool hasCollider;
    public bool colliderIsTrigger;
    public bool hasRigidbody;

    // Camera
    public bool isCamera;
    public float cameraDepth;
    public string cameraCullingMask; // human-readable layer names

    // Audio
    public bool hasAudioSource;
}

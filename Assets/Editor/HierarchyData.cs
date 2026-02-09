using System;
using System.Collections.Generic;
using UnityEngine;

// Compact export schema: only CDU-relevant UI text + interactives.
// Keeps JSON small enough to share.

[Serializable]
public class CduUiExport
{
    public string version = "CDU_UI_1.0";
    public string unityVersion;
    public string exportUtc;
    public string activeScene;
    public string rootPathFilter; // optional: export only this subtree

    public List<CduTextElement> text = new List<CduTextElement>();
    public List<CduInteractiveElement> interactives = new List<CduInteractiveElement>();
}

[Serializable]
public class CduTextElement
{
    public string scene;
    public string path; // full transform path
    public string name;
    public bool activeInHierarchy;

    // TMP snapshot (minimal)
    public string tmpType; // TextMeshProUGUI / TextMeshPro
    public string text;
    public float fontSize;
    public string alignment;
    public bool raycastTarget;
}

[Serializable]
public class CduInteractiveElement
{
    public string scene;
    public string path;
    public string name;
    public bool activeInHierarchy;

    public bool hasButton;
    public bool hasToggle;
    public bool hasInputField;

    public bool interactable;
    public string linkedTextPath; // first TMP under this object (label)
}

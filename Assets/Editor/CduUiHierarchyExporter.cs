using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// CDU-focused exporter: emits a small JSON containing TMP text elements + interactive controls.
// Use a root path filter to export ONLY the FMS/CDU subtree.
namespace FMS.CDU.Export
{
    public static class CduUiHierarchyExporter
    {
        private const string RootFilterPrefsKey = "CduUiExporter.RootPathFilter";

        [MenuItem("Tools/FMS/CDU/Export UI (Text + Interactives)")]

        public static void ExportCduUi()
        {
            string rootFilter = EditorPrefs.GetString(RootFilterPrefsKey, "");

            var pkg = new CduUiExport
            {
                unityVersion = Application.unityVersion,
                exportUtc = DateTime.UtcNow.ToString("o"),
                activeScene = SceneManager.GetActiveScene().name,
                rootPathFilter = rootFilter
            };

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!string.IsNullOrEmpty(rootFilter))
                    {
                        var match = FindByPath(root.transform, rootFilter);
                        if (match == null) continue;
                        Traverse(match, scene.name, pkg);
                    }
                    else
                    {
                        Traverse(root.transform, scene.name, pkg);
                    }
                }
            }

            WriteJson(pkg, $"CDU_UI_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            Debug.Log($"[CDU_UI_Exporter] Exported {pkg.text.Count} TMP text elements, {pkg.interactives.Count} interactives.");
        }

        // Call from Console:
        // HierarchyExporter.SetRootPathFilter("WorldRoot/Canvas/FMS_Screen");
        public static void SetRootPathFilter(string transformPath)
        {
            EditorPrefs.SetString(RootFilterPrefsKey, (transformPath ?? "").Replace("\\", "/"));
            Debug.Log($"[CDU_UI_Exporter] Root filter set to: {EditorPrefs.GetString(RootFilterPrefsKey)}");
        }

        private static void Traverse(Transform t, string sceneName, CduUiExport pkg)
        {
            CollectAt(t, sceneName, pkg);
            for (int i = 0; i < t.childCount; i++)
                Traverse(t.GetChild(i), sceneName, pkg);
        }

        private static void CollectAt(Transform t, string sceneName, CduUiExport pkg)
        {
            // TMP text (most important)
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                pkg.text.Add(new CduTextElement
                {
                    scene = sceneName,
                    path = GetPath(t),
                    name = t.name,
                    activeInHierarchy = t.gameObject.activeInHierarchy,
                    tmpType = tmp.GetType().Name,
                    text = Trunc(tmp.text, 180),
                    fontSize = tmp.fontSize,
                    alignment = tmp.alignment.ToString(),
                    raycastTarget = tmp.raycastTarget
                });
            }

            // Interactives (LSKs, function keys, keypad, etc.)
            bool hasButton = t.GetComponent<Button>() != null;
            bool hasToggle = t.GetComponent<Toggle>() != null;
            bool hasInput = t.GetComponent<TMP_InputField>() != null || t.GetComponent<InputField>() != null;

            if (hasButton || hasToggle || hasInput)
            {
                bool interactable = true;
                var btn = t.GetComponent<Button>(); if (btn) interactable = btn.interactable;
                var tog = t.GetComponent<Toggle>(); if (tog) interactable = tog.interactable;
                var tif = t.GetComponent<TMP_InputField>(); if (tif) interactable = tif.interactable;
                var uif = t.GetComponent<InputField>(); if (uif) interactable = uif.interactable;

                string linkedTextPath = null;
                var linked = t.GetComponentInChildren<TMP_Text>(true);
                if (linked != null) linkedTextPath = GetPath(linked.transform);

                pkg.interactives.Add(new CduInteractiveElement
                {
                    scene = sceneName,
                    path = GetPath(t),
                    name = t.name,
                    activeInHierarchy = t.gameObject.activeInHierarchy,
                    hasButton = hasButton,
                    hasToggle = hasToggle,
                    hasInputField = hasInput,
                    interactable = interactable,
                    linkedTextPath = linkedTextPath
                });
            }
        }

        private static void WriteJson(CduUiExport pkg, string fileName)
        {
            string outputPath = Path.Combine(Application.dataPath, "../" + fileName);
            File.WriteAllText(outputPath, JsonUtility.ToJson(pkg, true));
            Debug.Log($"[CDU_UI_Exporter] Wrote export to: {outputPath}");
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            var cur = t;
            while (cur != null) { stack.Push(cur.name); cur = cur.parent; }
            return string.Join("/", stack);
        }

        private static Transform FindByPath(Transform root, string fullPath)
        {
            if (root == null || string.IsNullOrEmpty(fullPath)) return null;
            string path = fullPath.Replace("\\", "/");
            if (path == root.name) return root;
            if (path.StartsWith(root.name + "/")) path = path.Substring(root.name.Length + 1);

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            Transform cur = root;
            foreach (var p in parts)
            {
                var child = cur.Find(p);
                if (child == null) return null;
                cur = child;
            }
            return cur;
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}

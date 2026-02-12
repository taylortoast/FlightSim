using UnityEngine;
using System;
using System.IO;
using System.Collections;

/// <summary>
/// Spawns a local grid of quad tiles from StreamingAssets, scaled in meters (1u = 1m).
/// Tile scale is derived from scenario latitude + WebMercator meters-per-tile.
/// Optionally derives the center tile X/Y from scenario lat/lon using Slippy Map math.
/// </summary>
public class LocalTileGrid : MonoBehaviour
{
    [Header("Scenario (authoritative)")]
    // Optional fallback: normally provided at runtime via ScenarioRuntime.Set(...)
    public ScenarioDefinition scenario;
    public int zoomOverride = -1;                  // -1 = use scenario.baseZoom (or range lock)
    public bool deriveCenterFromScenarioLatLon = true;

    [Header("Tile Index / Grid")]
    public int z = 14;                             // overwritten by scenario / overrides
    public int centerX = 0;
    public int centerY = 0;
    public int radius = 10;                        // (2r+1)^2 tiles
    public float tileSizeM = 540f;                 // overwritten if scenario is assigned

    [Header("Anchors")]
    public Transform anchor;                       // optional: AircraftRoot; uses XZ, forces Y=0
    public Transform tileParent;                   // optional; defaults to this.transform

    [Header("Rendering")]
    public Material tileMatTemplate;               // Unlit/Texture
    public bool destroyAndRebuild = true;

    [Header("StreamingAssets Layout")]
    // IMPORTANT: must match your folder under Assets/StreamingAssets/
    // Example: Assets/StreamingAssets/tiles_nd_dark_v1/12/.., 13/.., 14/..
    public string tilesRootFolder = "tiles_nd_dark_v1";

    [Header("ND Range Lock (Prototype Contract)")]
    public bool lockRangeToZoom = true;            // 20NM→z14, 10NM→z13, 5NM→z12
    public int defaultRangeNm = 20;

    public NDRangeState rangeState;
    public float rebuildDelay = 0.1f;
    Coroutine pending;

    void OnEnable()
    {
        ScenarioRuntime.OnChanged += HandleScenarioChanged;

        if (rangeState != null)
            rangeState.OnRangeChanged += HandleRangeChanged;
    }

    void OnDisable()
    {
        ScenarioRuntime.OnChanged -= HandleScenarioChanged;

        if (rangeState != null)
            rangeState.OnRangeChanged -= HandleRangeChanged;
    }

    void HandleScenarioChanged(ScenarioDefinition s)
    {
        scenario = s;

        // Rebuild using current ND range contract (or scenario zoom if not locked).
        if (lockRangeToZoom)
            SetNdRangeNm(defaultRangeNm);
        else
            Rebuild();
    }

    void HandleRangeChanged(int nm)
    {
        if (pending != null) StopCoroutine(pending);
        pending = StartCoroutine(RebuildAfterDelay(nm));
    }

    IEnumerator RebuildAfterDelay(int nm)
    {
        yield return new WaitForSeconds(rebuildDelay);
        SetNdRangeNm(nm); // uses zoomOverride + Rebuild()
        pending = null;
    }

    IEnumerator Start()
    {
        // Timing fix: allow other systems (PlaneController/Awake, etc.) to settle.
        yield return null;

        // If ScenarioSelection already set a scenario before this scene finished loading:
        if (scenario == null)
            scenario = ScenarioRuntime.Current;

        if (scenario == null)
        {
            Debug.LogWarning("[LocalTileGrid] No ScenarioDefinition assigned yet. Waiting for ScenarioRuntime.Set(...).");
            yield break;
        }

        if (lockRangeToZoom)
            SetNdRangeNm(defaultRangeNm);
        else
            Rebuild();
    }

    public void Rebuild()
    {
        ApplyScenario();

        if (destroyAndRebuild)
            DestroyChildren();

        BuildTiles();
    }

    void ApplyScenario()
    {
        // If no scenario, we can still spawn using inspector fields.
        if (scenario == null)
        {
            Debug.LogWarning("[LocalTileGrid] No ScenarioDefinition assigned. Using inspector fields.");
            return;
        }

        // Decide zoom:
        // Priority: explicit zoomOverride -> range lock mapping -> scenario.baseZoom
        if (zoomOverride >= 0)
            z = zoomOverride;
        else if (!lockRangeToZoom)
            z = scenario.baseZoom;

        // Meters-per-tile from latitude (Web Mercator).
        tileSizeM = WebMercator.MetersPerTile(scenario.centerLatDeg, z);

        // Derive slippy-map tile indices from lat/lon (center tile).
        if (deriveCenterFromScenarioLatLon)
            LatLonToTileXY(scenario.centerLatDeg, scenario.centerLonDeg, z, out centerX, out centerY);

        Debug.Log($"[LocalTileGrid] scenario='{scenario.name}' z={z} center=({centerX},{centerY}) tileSizeM={tileSizeM:F2}m");
    }

    void DestroyChildren()
    {
        Transform parent = tileParent ? tileParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    public void BuildTiles()
    {
        if (tileMatTemplate == null)
        {
            Debug.LogError("[LocalTileGrid] tileMatTemplate is not assigned.");
            return;
        }

        Transform parent = tileParent ? tileParent : transform;

        // Anchor grid to the final aircraft/anchor position (XZ only), otherwise parent position.
        Vector3 origin = parent.position;
        if (anchor != null)
        {
            Vector3 a = anchor.position;
            origin = new Vector3(a.x, 0f, a.z);
        }

        int found = 0;
        int missing = 0;

        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                // StreamingAssets/<tilesRootFolder>/<z>/<x>/<y>.png
                string rel = Path.Combine(tilesRootFolder, z.ToString(), x.ToString(), y + ".png");
                string path = Path.Combine(Application.streamingAssetsPath, rel);

                if (!File.Exists(path))
                {
                    missing++;
                    continue;
                }

                found++;

                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"Tile_z{z}_{x}_{y}";
                go.transform.SetParent(parent, true);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(tileSizeM, tileSizeM, 1f);

                // -dy keeps North-up alignment (slippy Y increases southward).
                go.transform.position = origin + new Vector3(dx * tileSizeM, 0f, -dy * tileSizeM);

                var mat = new Material(tileMatTemplate);
                mat.mainTexture = tex;
                go.GetComponent<MeshRenderer>().material = mat;

                Destroy(go.GetComponent<Collider>());
            }

        if (found == 0)
        {
            Debug.LogError("[LocalTileGrid] Found 0 tiles. Most common causes: tilesRootFolder mismatch, wrong z (zoom), or centerX/centerY don't match exported tile set.");
        }
        else
        {
            Debug.Log($"[LocalTileGrid] Built tiles: found={found}, missing={missing}, z={z}.");
        }
    }

    /// <summary>Converts lat/lon to slippy-map (x,y) tile indices at zoom z.</summary>
    public static void LatLonToTileXY(double latDeg, double lonDeg, int z, out int x, out int y)
    {
        double latRad = latDeg * Math.PI / 180.0;
        int n = 1 << z;

        double xf = (lonDeg + 180.0) / 360.0 * n;
        double yf = (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        x = Mathf.Clamp((int)Math.Floor(xf), 0, n - 1);
        y = Mathf.Clamp((int)Math.Floor(yf), 0, n - 1);
    }

    /// <summary>
    /// Contract: 20NM→z14, 10NM→z13, 5NM→z12 (since your current tile set maxzoom is 14).
    /// This sets zoomOverride so ApplyScenario cannot overwrite the zoom.
    /// </summary>
    public void SetNdRangeNm(int rangeNm)
    {
        int mappedZ = rangeNm switch
        {
            20 => 14,
            10 => 13,
            5 => 12,
            _ => z
        };

        z = mappedZ;
        zoomOverride = mappedZ;

        Rebuild();
    }
}

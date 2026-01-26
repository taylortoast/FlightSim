using System.IO;
using UnityEngine;

public class RasterTileGridLoader : MonoBehaviour
{

    // temp change
    [Header("Tile source")]
    public int zoom = 12;
    public int centerX = 1033;
    public int centerY = 1628;
    public int gridRadius = 2;          // 2 → 5x5 (center ±2)
    public string baseFolder = "raster-tiles"; // under StreamingAssets

    [Header("World scale")]
    public float tileSizeMeters = 256f; // 256 px → 256 m
    [SerializeField] public Material tileMaterialTemplate;

    [Header("Parenting")]
    public Transform tilesParent;       // optional; if null, uses this.transform

    void Start()
    {
        if (tilesParent == null)
            tilesParent = this.transform;

        LoadGrid();
    }
    //sdf
    void LoadGrid()
    {
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dy = -gridRadius; dy <= gridRadius; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                string path = GetTilePath(zoom, x, y);
                Texture2D tex = LoadTexture(path);
                if (tex == null)
                {
                    Debug.LogWarning($"Missing tile: {path}");
                    continue;
                }

                CreateTileQuad(dx, dy, tex, x, y);
            }
        }
    }

    string GetTilePath(int z, int x, int y)
    {
        // Assets/StreamingAssets/<baseFolder>/<z>/<x>/<y>.png
        string root = Application.streamingAssetsPath;
        return Path.Combine(root, baseFolder, z.ToString(), x.ToString(), y + ".png");
    }

    Texture2D LoadTexture(string fullPath)
    {
        if (!File.Exists(fullPath))
            return null;

        byte[] data = File.ReadAllBytes(fullPath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(data))
            return null;

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    void CreateTileQuad(int dx, int dy, Texture2D tex, int x, int y)
    {
        // World offsets in meters, center tile at (0,0)
        float worldX = dx * tileSizeMeters;
        float worldZ = dy * tileSizeMeters;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"tile_z{zoom}_x{x}_y{y}";
        quad.transform.SetParent(tilesParent, false);

        // Rotate to lie flat on XZ plane
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localPosition = new Vector3(worldX, 0f, worldZ);
        quad.transform.localScale = new Vector3(tileSizeMeters, tileSizeMeters, 1f);

        Material mat;

        if (tileMaterialTemplate != null)
        {
            // Clone the template material
            mat = new Material(tileMaterialTemplate);
        }
        else
        {
            // Fallback to Standard shader
            Shader shader = Shader.Find("Standard");
            mat = new Material(shader);
        }


        mat.mainTexture = tex;
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
    }
}

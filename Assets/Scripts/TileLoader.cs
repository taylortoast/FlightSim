public class TileLoader : MonoBehaviour
{
    public InfiniteTileGrid grid; // reference to your grid
    private Dictionary<Vector2Int, Texture2D> cache = new();

    void Update()
    {
        foreach (var tile in grid.AllTiles)
        {
            Vector2Int tileIndex = tile.WorldTileIndex;

            if (!cache.ContainsKey(tileIndex))
            {
                StartCoroutine(LoadTileTexture(tileIndex, tile));
            }
            else
            {
                tile.Content.SetTexture(cache[tileIndex]);
            }
        }
    }

    private IEnumerator LoadTileTexture(Vector2Int index, Tile tile)
    {
        // Step 9 will fill this in
        yield return null;
    }
}

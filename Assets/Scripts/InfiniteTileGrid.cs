using UnityEngine;

public class InfiniteTileGrid : MonoBehaviour
{
    [Header("Tile Setup")]
    [SerializeField] private Transform aircraft;
    [SerializeField] private Transform tilePrefab;
    [SerializeField] private int gridSize = 3; // must be odd: 3, 5, 7...
    [SerializeField] private float tileSize = 305f;

    private Transform[,] tiles;
    private Vector2Int centerIndex;

    void Start()
    {
        if (gridSize % 2 == 0)
        {
            Debug.LogError("Grid size must be odd (3, 5, 7...)");
            return;
        }

        tiles = new Transform[gridSize, gridSize];
        centerIndex = new Vector2Int(gridSize / 2, gridSize / 2);

        SpawnGrid();
    }

    void Update()
    {
        UpdateGrid();
    }

    private void SpawnGrid()
    {
        Quaternion tileRotation = Quaternion.Euler(90f, 0f, 0f);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 pos = new Vector3(
                    (x - centerIndex.x) * tileSize,
                    0f,
                    (y - centerIndex.y) * tileSize
                );

                tiles[x, y] = Instantiate(tilePrefab, pos, tileRotation, transform);
            }
        }
    }


    private void UpdateGrid()
    {
        Vector3 aircraftPos = aircraft.position;

        // Determine which tile the aircraft is over
        int aircraftTileX = Mathf.FloorToInt(aircraftPos.x / tileSize);
        int aircraftTileY = Mathf.FloorToInt(aircraftPos.z / tileSize);

        // Reposition grid root so center tile matches aircraft tile
        Vector3 gridOrigin = new Vector3(
            aircraftTileX * tileSize,
            0f,
            aircraftTileY * tileSize
        );

        transform.position = gridOrigin;

        // Now reposition each tile relative to the grid root
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                tiles[x, y].localPosition = new Vector3(
                    (x - centerIndex.x) * tileSize,
                    0f,
                    (y - centerIndex.y) * tileSize
                );
            }
        }
    }
}

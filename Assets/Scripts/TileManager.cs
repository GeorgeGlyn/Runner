using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Core Tile & Obstacle Prefabs")]
    public GameObject tilePrefab;
    public GameObject[] obstaclePrefabs;
    public GameObject movingTrainPrefab;
    public GameObject coinPrefab;
    public GameObject hoverboardPickupPrefab;
    public Transform playerTransform;

    [Header("Urban Scenery & Environment Prefabs")]
    public GameObject overheadGantryPrefab;
    public GameObject[] frontBuildingPrefabs;
    public GameObject[] skyscraperPrefabs;
    public GameObject[] propPrefabs;

    [Header("Pool & Corridor Settings")]
    public float tileLength = 30f;
    public int numberOfTiles = 9;

    private float spawnZ = 0f;
    // Circular tile pool: Tiles circulate endlessly without instantiating/destroying scenery
    private List<GameObject> activeTiles = new List<GameObject>();

    void Start()
    {
        // 1. Locate player if not set
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // 2. Clean up any static pre-placed tiles in the scene
        Transform tilesRoot = GameObject.Find("TrackTiles")?.transform;
        if (tilesRoot != null)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform t in tilesRoot)
            {
                toDestroy.Add(t.gameObject);
            }
            for (int i = 0; i < toDestroy.Count; i++)
            {
                Destroy(toDestroy[i]);
            }
        }

        spawnZ = 0f;
        activeTiles.Clear();

        // 3. Pre-warm and build the initial corridor pool!
        // First 2 tiles have clean tracks without obstacles for start run
        for (int i = 0; i < 2; i++)
        {
            BuildNewTile(false);
        }

        // Remaining tiles have obstacles and pickups
        for (int i = 2; i < numberOfTiles; i++)
        {
            BuildNewTile(true);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // When player advances past the oldest active tile by 35m, recycle it forward!
        if (playerTransform.position.z - 35f > spawnZ - (numberOfTiles * tileLength))
        {
            RecycleOldestTile();
        }
    }

    /// <summary>
    /// Builds a pooled tile with static urban buildings, scenery props, and dynamic gameplay items.
    /// </summary>
    private void BuildNewTile(bool spawnObstacles)
    {
        if (tilePrefab == null) return;

        GameObject tile = Instantiate(tilePrefab, Vector3.forward * spawnZ, Quaternion.identity);
        tile.name = "TrackTile_Pooled_" + activeTiles.Count;

        Transform tilesRoot = GameObject.Find("TrackTiles")?.transform;
        if (tilesRoot != null) tile.transform.SetParent(tilesRoot, true);

        // Create container for dynamic gameplay items that get refreshed on recycle
        GameObject dynamicItems = new GameObject("DynamicGameplayItems");
        dynamicItems.transform.SetParent(tile.transform, false);

        // Populate permanent urban city scenery (attached once, never destroyed!)
        BuildCityScenery(tile, spawnZ);

        // Populate dynamic obstacles, coins, and pickups
        PopulateDynamicItems(dynamicItems.transform, spawnZ, spawnObstacles);

        activeTiles.Add(tile);
        spawnZ += tileLength;
    }

    /// <summary>
    /// Recycles the oldest tile from the back to the front (Conveyor belt architecture).
    /// Zero garbage collection allocations for tile base, buildings, and scenery!
    /// </summary>
    private void RecycleOldestTile()
    {
        if (activeTiles.Count == 0) return;

        // 1. Dequeue oldest tile
        GameObject tile = activeTiles[0];
        activeTiles.RemoveAt(0);

        // 2. Reposition tile forward to the new spawn position
        tile.transform.position = Vector3.forward * spawnZ;

        // 3. Find or recreate dynamic items container
        Transform dynamicItems = tile.transform.Find("DynamicGameplayItems");
        if (dynamicItems != null)
        {
            // Clear old obstacles, coins, and pickups
            int childCount = dynamicItems.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(dynamicItems.GetChild(i).gameObject);
            }
        }
        else
        {
            GameObject container = new GameObject("DynamicGameplayItems");
            container.transform.SetParent(tile.transform, false);
            dynamicItems = container.transform;
        }

        // 4. Populate fresh gameplay items for this segment
        PopulateDynamicItems(dynamicItems, spawnZ, true);

        // 5. Enqueue tile to back of active list
        activeTiles.Add(tile);
        spawnZ += tileLength;
    }

    /// <summary>
    /// Generates static urban scenery (skyscrapers, shops, trees, lamps) permanently on the tile.
    /// </summary>
    private void BuildCityScenery(GameObject tile, float tileZ)
    {
        // 1. Front Row Mid-Rise Commercial Buildings (x = Â±8.5m)
        if (frontBuildingPrefabs != null && frontBuildingPrefabs.Length > 0)
        {
            float[] zOffsets = new float[] { 5f, 15f, 25f };

            // Left side
            for (int i = 0; i < 3; i++)
            {
                GameObject bPrefab = frontBuildingPrefabs[Random.Range(0, frontBuildingPrefabs.Length)];
                float z = zOffsets[i] + Random.Range(-1.2f, 1.2f);
                float x = -8.5f + Random.Range(-0.4f, 0.4f);
                float rotY = Random.Range(0, 4) * 90f;
                float heightScale = Random.Range(3.5f, 5.5f);

                GameObject b = Instantiate(bPrefab, new Vector3(x, 0f, tileZ + z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(4.5f, heightScale, 4.5f);
            }

            // Right side
            for (int i = 0; i < 3; i++)
            {
                GameObject bPrefab = frontBuildingPrefabs[Random.Range(0, frontBuildingPrefabs.Length)];
                float z = zOffsets[i] + Random.Range(-1.2f, 1.2f);
                float x = 8.5f + Random.Range(-0.4f, 0.4f);
                float rotY = Random.Range(0, 4) * 90f;
                float heightScale = Random.Range(3.5f, 5.5f);

                GameObject b = Instantiate(bPrefab, new Vector3(x, 0f, tileZ + z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(4.5f, heightScale, 4.5f);
            }
        }

        // 2. Back Row Towering Skyscrapers (x = Â±17.5m)
        if (skyscraperPrefabs != null && skyscraperPrefabs.Length > 0)
        {
            float[] zOffsetsSky = new float[] { 7f, 22f };

            // Left background
            for (int i = 0; i < 2; i++)
            {
                GameObject skyPrefab = skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
                float z = zOffsetsSky[i] + Random.Range(-2f, 2f);
                float x = -17.5f + Random.Range(-0.8f, 0.8f);
                float rotY = Random.Range(0, 4) * 90f;
                float skyHeight = Random.Range(7.5f, 13.0f);

                GameObject b = Instantiate(skyPrefab, new Vector3(x, 0f, tileZ + z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(6.5f, skyHeight, 6.5f);
            }

            // Right background
            for (int i = 0; i < 2; i++)
            {
                GameObject skyPrefab = skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
                float z = zOffsetsSky[i] + Random.Range(-2f, 2f);
                float x = 17.5f + Random.Range(-0.8f, 0.8f);
                float rotY = Random.Range(0, 4) * 90f;
                float skyHeight = Random.Range(7.5f, 13.0f);

                GameObject b = Instantiate(skyPrefab, new Vector3(x, 0f, tileZ + z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(6.5f, skyHeight, 6.5f);
            }
        }

        // 3. Sidewalk Props (Trees, lamps, containers)
        if (propPrefabs != null && propPrefabs.Length > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject pPrefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
                float side = (Random.value > 0.5f) ? 1f : -1f;
                float px = side * 6.2f;
                float pz = (i * 9f) + Random.Range(1f, 4f);

                GameObject pObj = Instantiate(pPrefab, new Vector3(px, 0f, tileZ + pz), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), tile.transform);
                if (pPrefab.name.Contains("tree") || pPrefab.name.Contains("grass"))
                {
                    pObj.transform.localScale = Vector3.one * 1.8f;
                }
            }
        }
    }

    /// <summary>
    /// Spawns randomized gameplay elements (gantry, oncoming trains, obstacles, coins, hoverboard powerup).
    /// </summary>
    private void PopulateDynamicItems(Transform parent, float tileZ, bool spawnObstacles)
    {
        // 1. Overhead Railway Signal Gantry (Every 2-3 tiles)
        if (overheadGantryPrefab != null && Random.value < 0.40f)
        {
            Instantiate(overheadGantryPrefab, new Vector3(0f, 0f, tileZ + 15f), Quaternion.identity, parent);
        }

        // 2. Obstacles, Trains, Coins, and Powerups
        if (spawnObstacles && obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            float[] lanes = new float[] { -2.5f, 0f, 2.5f };
            int rLane = Random.Range(0, 3);
            float laneX = lanes[rLane];

            // Obstacle or moving train
            GameObject obsPrefab = (Random.value > 0.50f && movingTrainPrefab != null) ? movingTrainPrefab : obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            Vector3 obsPos = new Vector3(laneX, 0f, tileZ + 15f);
            GameObject obs = Instantiate(obsPrefab, obsPos, Quaternion.identity, parent);

            if (obs.name.Contains("Train") && Random.value > 0.5f)
            {
                if (obs.GetComponent<MovingTrain>() == null) obs.AddComponent<MovingTrain>();
            }

            // Spawn Coin streak on adjacent open lane
            if (coinPrefab != null)
            {
                int cLane = (rLane + 1) % 3;
                float cLaneX = lanes[cLane];
                for (int c = 0; c < 5; c++)
                {
                    Instantiate(coinPrefab, new Vector3(cLaneX, 0.75f, tileZ + 8f + (c * 3f)), Quaternion.identity, parent);
                }
            }

            // Spawn Hoverboard Powerup (~28% chance on remaining open lane)
            if (hoverboardPickupPrefab != null && Random.value < 0.28f)
            {
                int pLane = (rLane + 2) % 3;
                float pLaneX = lanes[pLane];
                Instantiate(hoverboardPickupPrefab, new Vector3(pLaneX, 0.85f, tileZ + 20f), Quaternion.identity, parent);
            }
        }
    }
}

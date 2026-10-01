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
        /// <summary>
    /// Spawns randomized gameplay elements (gantry, oncoming trains, obstacles, coins, hoverboard powerup).
    /// </summary>
    private void PopulateDynamicItems(Transform parent, float tileZ, bool spawnObstacles)
    {
        float[] lanes = new float[] { -2.5f, 0f, 2.5f };

        // 1. Overhead Railway Signal Gantry (Every 2-3 tiles)
        if (overheadGantryPrefab != null && Random.value < 0.40f)
        {
            Instantiate(overheadGantryPrefab, new Vector3(0f, 0f, tileZ + 15f), Quaternion.identity, parent);
        }

        // On starting introductory tiles (without obstacles): spawn a straight line of coins on center track!
        if (!spawnObstacles)
        {
            if (coinPrefab != null)
            {
                SpawnGroundCoins(parent, 0f, tileZ + 8f, 6, 2.6f, 0.75f);
            }
            return;
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // 2. Obstacles, Trains, Coins, and Powerups
        int rLane = Random.Range(0, 3);
        float laneX = lanes[rLane];

        // Randomly select obstacle or train
        GameObject obsPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Vector3 obsPos = new Vector3(laneX, 0f, tileZ + 15f);
        GameObject obs = Instantiate(obsPrefab, obsPos, Quaternion.identity, parent);

        bool isTrain = obs.name.Contains("Train") || obsPrefab.name.Contains("Train");
        bool isMovingTrain = false;
        bool isWaitingTrain = false;

        if (isTrain)
        {
            // Requirement 2: Support trains that are waiting (stationary) as well as moving!
            // 50% chance of waiting (parked/stationary) train, 50% oncoming moving train
            if (Random.value < 0.50f)
            {
                isWaitingTrain = true;
                obs.name = "WaitingTrain";
                // Ensure no MovingTrain script is attached so it sits waiting on the tracks
                MovingTrain mt = obs.GetComponent<MovingTrain>();
                if (mt != null) Destroy(mt);
            }
            else
            {
                isMovingTrain = true;
                obs.name = "MovingTrain";
                MovingTrain mt = obs.GetComponent<MovingTrain>();
                if (mt == null) mt = obs.AddComponent<MovingTrain>();
                mt.speed = Random.Range(10.5f, 13.5f);
            }
        }

        // 3. Dynamic Coin Formations
        // Requirement 1:
        // - NO OVERLAP of coins across tracks (every coin is strictly centered on its track lane, no diagonals)
        // - When there is a train, there shouldn't be ANY coin below that!
        if (coinPrefab != null)
        {
            // A. Obstacle Lane (laneX):
            if (isTrain)
            {
                // CRITICAL RULE: If there is a train (waiting or moving), NEVER spawn any coins on this lane!
                // Zero coins below, inside, or touching the train.
            }
            else
            {
                // If it is a low obstacle (LowHurdle), spawn a parabolic coin arc OVER the hurdle!
                bool isLowObstacle = !obs.name.Contains("High") && !obs.name.Contains("Overhead");
                if (isLowObstacle && Random.value < 0.65f)
                {
                    // Smooth jump arc over hurdle: 7 coins rising to 1.70m, strictly centered on laneX
                    SpawnCoinArc(parent, laneX, tileZ + 9f, 12f, 7, 1.70f, 0.75f);
                }
            }

            // B. Open Adjacent Track Lane (cLaneX):
            int cLane = (rLane + 1) % 3;
            float cLaneX = lanes[cLane];

            // 50% clean straight ground line, 50% athletic jump arc on open track
            if (Random.value < 0.50f)
            {
                // Clean ground run: 6 coins strictly centered on cLaneX
                SpawnGroundCoins(parent, cLaneX, tileZ + 7f, 6, 2.6f, 0.75f);
            }
            else
            {
                // Athletic jump arc: 7 coins strictly centered on cLaneX
                SpawnCoinArc(parent, cLaneX, tileZ + 8f, 13f, 7, 1.65f, 0.75f);
            }
        }

        // 4. Hoverboard Powerup (~28% chance on the remaining 3rd open lane)
        if (hoverboardPickupPrefab != null && Random.value < 0.28f)
        {
            int pLane = (rLane + 2) % 3;
            float pLaneX = lanes[pLane];
            Instantiate(hoverboardPickupPrefab, new Vector3(pLaneX, 0.85f, tileZ + 20f), Quaternion.identity, parent);
        }
    }

    /// <summary>
    /// Spawns a smooth parabolic coin arc matching the player's jump trajectory,
    /// strictly aligned to a single track lane.
    /// </summary>
    private void SpawnCoinArc(Transform parent, float laneX, float startZ, float arcLength, int coinCount, float arcHeight, float groundY)
    {
        if (coinPrefab == null) return;
        float step = arcLength / (coinCount - 1);
        for (int i = 0; i < coinCount; i++)
        {
            float t = (float)i / (coinCount - 1);
            float z = startZ + (i * step);
            float y = groundY + (Mathf.Sin(t * Mathf.PI) * arcHeight);
            Instantiate(coinPrefab, new Vector3(laneX, y, z), Quaternion.identity, parent);
        }
    }

    /// <summary>
    /// Spawns a straight ground run of coins strictly centered on a single track lane.
    /// </summary>
    private void SpawnGroundCoins(Transform parent, float laneX, float startZ, int count, float spacing, float y)
    {
        if (coinPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            float z = startZ + (i * spacing);
            Instantiate(coinPrefab, new Vector3(laneX, y, z), Quaternion.identity, parent);
        }
    }
}

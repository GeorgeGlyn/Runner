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

    public float tileLength = 30f;
    public int numberOfTiles = 9;
    private float spawnZ = 0f;
    private List<GameObject> activeTiles = new List<GameObject>();

    void Start()
    {
        // 1. Clean up any old static pre-placed tiles in the scene so full city is generated from z = 0!
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

        // 2. Generate full city corridor from z = 0!
        // First 2 tiles have full city scenery but no obstacles for a clean start run
        for (int i = 0; i < 2; i++)
        {
            SpawnTile(false);
        }

        // Remaining tiles have full city scenery and obstacles
        for (int i = 2; i < numberOfTiles; i++)
        {
            SpawnTile(true);
        }
    }

    void Update()
    {
        if (playerTransform != null && playerTransform.position.z - 35f > spawnZ - (numberOfTiles * tileLength))
        {
            SpawnTile(true);
            DeleteTile();
        }
    }

    public void SpawnTile(bool spawnObstacles)
    {
        if (tilePrefab == null) return;

        GameObject tile = Instantiate(tilePrefab, Vector3.forward * spawnZ, Quaternion.identity);
        Transform tilesRoot = GameObject.Find("TrackTiles")?.transform;
        if (tilesRoot != null) tile.transform.SetParent(tilesRoot, true);

        activeTiles.Add(tile);

        // 1. Spawn Overhead Railway Gantry (Every 2-3 tiles)
        if (overheadGantryPrefab != null && Random.value < 0.40f)
        {
            Instantiate(overheadGantryPrefab, new Vector3(0f, 0f, spawnZ + 15f), Quaternion.identity, tile.transform);
        }

        // 2. FRONT ROW BUILDINGS (Mid-rise shops, commercial complexes, right along the street)
        // Placed close to the retaining wall (x = ±8.5m), packed tightly (3 buildings per 30m tile)
        if (frontBuildingPrefabs != null && frontBuildingPrefabs.Length > 0)
        {
            float[] zOffsets = new float[] { 5f, 15f, 25f };

            // Left Front Row (x = -8.5m)
            for (int i = 0; i < 3; i++)
            {
                GameObject bPrefab = frontBuildingPrefabs[Random.Range(0, frontBuildingPrefabs.Length)];
                float z = spawnZ + zOffsets[i] + Random.Range(-1.5f, 1.5f);
                float x = -8.5f + Random.Range(-0.4f, 0.4f);
                float rotY = Random.Range(0, 4) * 90f;
                float heightScale = Random.Range(3.5f, 5.5f);

                GameObject b = Instantiate(bPrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(4.5f, heightScale, 4.5f);
            }

            // Right Front Row (x = +8.5m)
            for (int i = 0; i < 3; i++)
            {
                GameObject bPrefab = frontBuildingPrefabs[Random.Range(0, frontBuildingPrefabs.Length)];
                float z = spawnZ + zOffsets[i] + Random.Range(-1.5f, 1.5f);
                float x = 8.5f + Random.Range(-0.4f, 0.4f);
                float rotY = Random.Range(0, 4) * 90f;
                float heightScale = Random.Range(3.5f, 5.5f);

                GameObject b = Instantiate(bPrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(4.5f, heightScale, 4.5f);
            }
        }

        // 3. BACK ROW TOWERING SKYSCRAPERS (Massive skyline background layer)
        // Placed at x = ±17.5m, towering 35m-60m high into the sky!
        if (skyscraperPrefabs != null && skyscraperPrefabs.Length > 0)
        {
            float[] zOffsetsSky = new float[] { 7f, 22f };

            // Left Background Skyscrapers
            for (int i = 0; i < 2; i++)
            {
                GameObject skyPrefab = skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
                float z = spawnZ + zOffsetsSky[i] + Random.Range(-2f, 2f);
                float x = -17.5f + Random.Range(-0.8f, 0.8f);
                float rotY = Random.Range(0, 4) * 90f;
                float skyHeight = Random.Range(7.5f, 13.0f);

                GameObject b = Instantiate(skyPrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(6.5f, skyHeight, 6.5f);
            }

            // Right Background Skyscrapers
            for (int i = 0; i < 2; i++)
            {
                GameObject skyPrefab = skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
                float z = spawnZ + zOffsetsSky[i] + Random.Range(-2f, 2f);
                float x = 17.5f + Random.Range(-0.8f, 0.8f);
                float rotY = Random.Range(0, 4) * 90f;
                float skyHeight = Random.Range(7.5f, 13.0f);

                GameObject b = Instantiate(skyPrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, rotY, 0f), tile.transform);
                b.transform.localScale = new Vector3(6.5f, skyHeight, 6.5f);
            }
        }

        // 4. SIDEWALK PROPS (Trees, street lamps, containers between wall and buildings)
        if (propPrefabs != null && propPrefabs.Length > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject pPrefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
                float side = (Random.value > 0.5f) ? 1f : -1f;
                float px = side * 6.2f;
                float pz = spawnZ + (i * 9f) + Random.Range(1f, 4f);

                GameObject pObj = Instantiate(pPrefab, new Vector3(px, 0f, pz), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), tile.transform);
                if (pPrefab.name.Contains("tree") || pPrefab.name.Contains("grass"))
                {
                    pObj.transform.localScale = Vector3.one * 1.8f;
                }
            }
        }

        // 5. Spawn Gameplay Obstacles, Trains, and Pickups
        if (spawnObstacles && obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            float[] lanes = new float[] { -2.5f, 0f, 2.5f };
            int rLane = Random.Range(0, 3);
            float laneX = lanes[rLane];

            GameObject obsPrefab = (Random.value > 0.5f && movingTrainPrefab != null) ? movingTrainPrefab : obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            Vector3 obsPos = new Vector3(laneX, 0f, spawnZ + 15f);
            GameObject obs = Instantiate(obsPrefab, obsPos, Quaternion.identity, tile.transform);

            if (obs.name.Contains("Train") && Random.value > 0.5f)
            {
                if (obs.GetComponent<MovingTrain>() == null) obs.AddComponent<MovingTrain>();
            }

            // Spawn Coins
            if (coinPrefab != null)
            {
                int cLane = (rLane + 1) % 3;
                float cLaneX = lanes[cLane];
                for (int c = 0; c < 5; c++)
                {
                    Instantiate(coinPrefab, new Vector3(cLaneX, 0.75f, spawnZ + 8f + (c * 3f)), Quaternion.identity, tile.transform);
                }
            }

            // Spawn Hoverboard Powerup (~28% chance on open lane)
            if (hoverboardPickupPrefab != null && Random.value < 0.28f)
            {
                int pLane = (rLane + 2) % 3;
                float pLaneX = lanes[pLane];
                Instantiate(hoverboardPickupPrefab, new Vector3(pLaneX, 0.85f, spawnZ + 20f), Quaternion.identity, tile.transform);
            }
        }

        spawnZ += tileLength;
    }

    void DeleteTile()
    {
        if (activeTiles.Count > 0)
        {
            Destroy(activeTiles[0]);
            activeTiles.RemoveAt(0);
        }
    }
}

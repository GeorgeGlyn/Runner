using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

public class RunnerSceneBuilder
{
    [MenuItem("Tools/Build Complete Runner Game")]
    public static void BuildGame()
    {
        Debug.Log("Building Complete Subway Surfers Runner Game with Adjusted Train and Player Scales...");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        Material roadMat = CreateColorMat("Assets/Materials/RoadMat.mat", new Color(0.12f, 0.13f, 0.15f));
        Material ballastMat = CreateColorMat("Assets/Materials/BallastMat.mat", new Color(0.24f, 0.22f, 0.20f));
        Material steelMat = CreateColorMat("Assets/Materials/SteelRailMat.mat", new Color(0.80f, 0.80f, 0.84f));
        Material tieMat = CreateColorMat("Assets/Materials/WoodTieMat.mat", new Color(0.38f, 0.24f, 0.16f));
        Material obsMat = CreateColorMat("Assets/Materials/ObstacleMat.mat", new Color(0.88f, 0.18f, 0.18f));
        Material coinMat = CreateColorMat("Assets/Materials/CoinMat.mat", new Color(1.0f, 0.85f, 0.10f));

        GameObject trackModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TrainKit/Models/FBX format/railroad-straight.fbx")
                                  ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TrainKit/Models/FBX format/track.fbx");

        // 1. Create Track Tile Prefab with 3 Sets of Continuous Railway Tracks & City Scenery
        GameObject tileGO = new GameObject("TilePrefab");

        // Main Ground / Stone Foundation (30m long)
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Ground";
        road.transform.SetParent(tileGO.transform, false);
        road.transform.localScale = new Vector3(9.2f, 0.2f, 30f);
        road.transform.localPosition = new Vector3(0, -0.1f, 15f);
        road.GetComponent<Renderer>().sharedMaterial = roadMat;

        // Railway Tracks on ALL 3 LANES: Left (-2.5f), Middle (0f), Right (+2.5f)
        float[] laneXs = new float[] { -2.5f, 0.0f, 2.5f };
        for (int l = 0; l < laneXs.Length; l++)
        {
            float laneX = laneXs[l];
            GameObject trackLaneGO = new GameObject("RailwayTrack_Lane_" + l);
            trackLaneGO.transform.SetParent(tileGO.transform, false);

            // Ballast gravel bed under each track
            GameObject ballast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ballast.name = "BallastBed";
            ballast.transform.SetParent(trackLaneGO.transform, false);
            ballast.transform.localScale = new Vector3(2.2f, 0.08f, 30f);
            ballast.transform.localPosition = new Vector3(laneX, 0.03f, 15f);
            ballast.GetComponent<Renderer>().sharedMaterial = ballastMat;
            Object.DestroyImmediate(ballast.GetComponent<Collider>());

            // Procedural Steel Rails (Left & Right rail running the full 30m length)
            GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railL.name = "Rail_L";
            railL.transform.SetParent(trackLaneGO.transform, false);
            railL.transform.localScale = new Vector3(0.08f, 0.14f, 30f);
            railL.transform.localPosition = new Vector3(laneX - 0.72f, 0.12f, 15f);
            railL.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(railL.GetComponent<Collider>());

            GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railR.name = "Rail_R";
            railR.transform.SetParent(trackLaneGO.transform, false);
            railR.transform.localScale = new Vector3(0.08f, 0.14f, 30f);
            railR.transform.localPosition = new Vector3(laneX + 0.72f, 0.12f, 15f);
            railR.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(railR.GetComponent<Collider>());

            // Wooden Ties / Sleepers spaced every 1.2 meters
            for (float z = 0.6f; z < 30f; z += 1.2f)
            {
                GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tie.name = "Tie_" + z;
                tie.transform.SetParent(trackLaneGO.transform, false);
                tie.transform.localScale = new Vector3(1.9f, 0.08f, 0.28f);
                tie.transform.localPosition = new Vector3(laneX, 0.06f, z);
                tie.GetComponent<Renderer>().sharedMaterial = tieMat;
                Object.DestroyImmediate(tie.GetComponent<Collider>());
            }

            // TrainKit track pieces
            if (trackModelAsset != null)
            {
                for (float z = 2f; z <= 28f; z += 4f)
                {
                    GameObject trackSeg = Object.Instantiate(trackModelAsset, trackLaneGO.transform);
                    trackSeg.name = "TrackFbx_" + z;
                    trackSeg.transform.localPosition = new Vector3(laneX, 0.06f, z);
                    trackSeg.transform.localRotation = Quaternion.identity;
                    trackSeg.transform.localScale = new Vector3(1.7f, 1.0f, 1.0f);
                }
            }
        }

        // Add City Scenery (buildings and trees on sides of tracks)
        GameObject buildingA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/CityKit/building-small-a.glb");
        GameObject buildingB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/CityKit/building-small-b.glb");
        GameObject trees = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/CityKit/grass-trees-tall.glb");

        if (buildingA != null)
        {
            GameObject bLeft = Object.Instantiate(buildingA, tileGO.transform);
            bLeft.transform.localPosition = new Vector3(-7.2f, 0f, 8f);
            bLeft.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            bLeft.transform.localScale = Vector3.one * 1.5f;

            GameObject bRight = Object.Instantiate(buildingB ?? buildingA, tileGO.transform);
            bRight.transform.localPosition = new Vector3(7.2f, 0f, 22f);
            bRight.transform.localRotation = Quaternion.Euler(0, -90f, 0);
            bRight.transform.localScale = Vector3.one * 1.5f;

            if (trees != null)
            {
                GameObject tLeft = Object.Instantiate(trees, tileGO.transform);
                tLeft.transform.localPosition = new Vector3(-6.9f, 0f, 22f);
                tLeft.transform.localScale = Vector3.one * 1.4f;

                GameObject tRight = Object.Instantiate(trees, tileGO.transform);
                tRight.transform.localPosition = new Vector3(6.9f, 0f, 8f);
                tRight.transform.localScale = Vector3.one * 1.4f;
            }
        }

        GameObject tilePrefab = PrefabUtility.SaveAsPrefabAsset(tileGO, "Assets/Prefabs/TrackTile.prefab");
        Object.DestroyImmediate(tileGO);

        // 2. Create Obstacle Prefabs
        // A. Low Hurdle
        GameObject hurdleGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hurdleGO.name = "LowHurdle";
        hurdleGO.tag = "Obstacle";
        hurdleGO.transform.localScale = new Vector3(2.2f, 0.7f, 0.5f);
        hurdleGO.transform.position = new Vector3(0, 0.35f, 0);
        hurdleGO.GetComponent<Renderer>().sharedMaterial = obsMat;
        hurdleGO.AddComponent<Obstacle>();
        GameObject hurdlePrefab = PrefabUtility.SaveAsPrefabAsset(hurdleGO, "Assets/Prefabs/LowHurdle.prefab");
        Object.DestroyImmediate(hurdleGO);

        // B. High Barrier
        GameObject barrierGO = new GameObject("HighBarrier");
        barrierGO.tag = "Obstacle";
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.transform.SetParent(barrierGO.transform);
        bar.transform.localScale = new Vector3(2.4f, 0.8f, 0.4f);
        bar.transform.localPosition = new Vector3(0, 1.8f, 0);
        bar.GetComponent<Renderer>().sharedMaterial = obsMat;
        GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        postL.transform.SetParent(barrierGO.transform);
        postL.transform.localScale = new Vector3(0.15f, 1.1f, 0.15f);
        postL.transform.localPosition = new Vector3(-1.1f, 1.1f, 0);
        GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        postR.transform.SetParent(barrierGO.transform);
        postR.transform.localScale = new Vector3(0.15f, 1.1f, 0.15f);
        postR.transform.localPosition = new Vector3(1.1f, 1.1f, 0);
        BoxCollider barCol = barrierGO.AddComponent<BoxCollider>();
        barCol.size = new Vector3(2.4f, 0.8f, 0.4f);
        barCol.center = new Vector3(0, 1.8f, 0);
        barrierGO.AddComponent<Obstacle>();
        GameObject barrierPrefab = PrefabUtility.SaveAsPrefabAsset(barrierGO, "Assets/Prefabs/HighBarrier.prefab");
        Object.DestroyImmediate(barrierGO);

        // C. Authentic Subway Train Obstacle (ENLARGED to 2.15x for grand Subway Surfers scale!)
        GameObject trainHeadAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TrainKit/Models/FBX format/train-electric-subway-a.fbx");
        GameObject trainMidAsset  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TrainKit/Models/FBX format/train-electric-subway-b.fbx");
        GameObject trainTailAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TrainKit/Models/FBX format/train-electric-subway-c.fbx");

        GameObject trainPrefab = null;
        if (trainHeadAsset != null)
        {
            GameObject trainRoot = new GameObject("TrainObstacle");
            trainRoot.tag = "Obstacle";

            // Increased train scale from 1.7 to 2.15 for impressive, imposing subway train size
            float trainScale = 2.15f;

            // Front Car (Subway Cab with headlights)
            GameObject carFront = Object.Instantiate(trainHeadAsset, trainRoot.transform);
            carFront.name = "FrontCar";
            carFront.transform.localPosition = new Vector3(0f, 0.14f, 4.8f);
            carFront.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            carFront.transform.localScale = Vector3.one * trainScale;

            if (trainMidAsset != null)
            {
                GameObject carMid = Object.Instantiate(trainMidAsset, trainRoot.transform);
                carMid.name = "MidCar";
                carMid.transform.localPosition = new Vector3(0f, 0.14f, 0.0f);
                carMid.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                carMid.transform.localScale = Vector3.one * trainScale;
            }

            if (trainTailAsset != null)
            {
                GameObject carTail = Object.Instantiate(trainTailAsset, trainRoot.transform);
                carTail.name = "RearCar";
                carTail.transform.localPosition = new Vector3(0f, 0.14f, -4.8f);
                carTail.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                carTail.transform.localScale = Vector3.one * trainScale;
            }

            // Collider sized to encompass the enlarged train
            BoxCollider tc = trainRoot.AddComponent<BoxCollider>();
            tc.size = new Vector3(2.4f, 3.2f, 14.5f);
            tc.center = new Vector3(0f, 1.6f, 0f);
            trainRoot.AddComponent<Obstacle>();

            trainPrefab = PrefabUtility.SaveAsPrefabAsset(trainRoot, "Assets/Prefabs/Train.prefab");
            Object.DestroyImmediate(trainRoot);
        }

        // 3. Create Coin Prefab
        AudioClip coinAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Temp/Audio/switch3.ogg")
                               ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Temp/Audio/click1.ogg");

        GameObject coinGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coinGO.name = "Coin";
        coinGO.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
        coinGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coinGO.GetComponent<Renderer>().sharedMaterial = coinMat;
        Collider col = coinGO.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Coin coinComp = coinGO.AddComponent<Coin>();
        if (coinAudioClip != null) coinComp.coinSound = coinAudioClip;
        GameObject coinPrefab = PrefabUtility.SaveAsPrefabAsset(coinGO, "Assets/Prefabs/Coin.prefab");
        Object.DestroyImmediate(coinGO);

        // 4. Build New Scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Pre-place 5 initial tiles under 'TrackTiles' in scene
        GameObject trackTilesRoot = new GameObject("TrackTiles");
        for (int i = 0; i < 5; i++)
        {
            float zPos = i * 30f;
            GameObject tile = Object.Instantiate(tilePrefab, trackTilesRoot.transform);
            tile.name = "TrackTile_" + i;
            tile.transform.position = new Vector3(0f, 0f, zPos);
        }

        // Create Player with CharacterController & PlayerController
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 0.8f, 0);
        player.transform.rotation = Quaternion.identity; // Root faces forward (+Z down track)

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0, 0);
        cc.height = 1.5f;
        cc.radius = 0.35f;
        player.AddComponent<PlayerController>();

        // Build Humanoid Character with adjusted sleek player size
        SetupHumanoidCharacter(player);

        // Setup Main Camera (following from behind looking forward down the railway tracks)
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 3.4f, -4.8f);
            cam.transform.rotation = Quaternion.Euler(14f, 0, 0);
            CameraFollow cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.target = player.transform;
            cf.offset = new Vector3(0f, 3.4f, -4.8f);
        }

        // Setup TileManager
        GameObject tmGO = new GameObject("TileManager");
        TileManager tm = tmGO.AddComponent<TileManager>();
        tm.tilePrefab = tilePrefab;
        tm.obstaclePrefabs = new GameObject[] { hurdlePrefab, barrierPrefab, trainPrefab };
        tm.coinPrefab = coinPrefab;
        tm.playerTransform = player.transform;

        // Setup GameManager
        GameObject gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();

        // Setup ShopManager (Persistent Currency & Upgrades)
        GameObject shopGO = new GameObject("ShopManager");
        shopGO.AddComponent<ShopManager>();

        // Setup AudioManager with Synthesized SFX & BGM Clips
        GameObject audioGO = new GameObject("AudioManager");
        AudioManager am = audioGO.AddComponent<AudioManager>();
        am.bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/bgm.wav");
        am.coinClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/coin.wav");
        am.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/jump.wav");
        am.slideClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/slide.wav");
        am.hoverboardClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/hoverboard.wav");
        am.shieldSaveClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/shield_save.wav");
        am.crashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/crash.wav");
        am.powerupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/powerup.wav");

        // Save Scene
        string scenePath = "Assets/Scenes/MainRunnerScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(scenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Build Completed! MainRunnerScene created with Adjusted Train and Player Scales!");
    }

    [MenuItem("Tools/Upgrade Player to 3D Humanoid")]
    public static void UpgradePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("No GameObject with tag 'Player' found in the active scene!");
            return;
        }
        SetupHumanoidCharacter(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Successfully upgraded Player to Animated Humanoid!");
    }

    public static void SetupHumanoidCharacter(GameObject player)
    {
        MeshRenderer mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Clear existing children
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(player.transform.GetChild(i).gameObject);
        }

        string modelPath = "Assets/Models/KenneyCharacters/Model/characterMedium.fbx";
        string skinPath = "Assets/Models/KenneyCharacters/Skins/skaterMaleA.png";
        string animControllerPath = "Assets/Models/KenneyCharacters/RunnerAnimator.controller";

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        Texture2D skinTex = AssetDatabase.LoadAssetAtPath<Texture2D>(skinPath);
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animControllerPath);

        // Find Humanoid Avatar
        Avatar modelAvatar = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (obj is Avatar av && av.isValid)
            {
                modelAvatar = av;
                break;
            }
        }

        if (modelAsset == null)
        {
            Debug.LogError("Character FBX not found at " + modelPath);
            return;
        }

        // Reduced player scale slightly from 0.68 to 0.54 for ideal Subway Surfers proportions
        float playerScale = 0.54f;

        GameObject characterInstance = Object.Instantiate(modelAsset, player.transform);
        characterInstance.name = "CharacterModel";
        characterInstance.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        // Face FORWARD down the track into the screen (+Z)
        characterInstance.transform.localRotation = Quaternion.identity;
        characterInstance.transform.localScale = Vector3.one * playerScale;

        // Assign Jake Skater texture
        if (skinTex != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material skaterMat = new Material(shader);
            skaterMat.mainTexture = skinTex;
            AssetDatabase.CreateAsset(skaterMat, "Assets/Materials/SkaterCharacterMat.mat");

            foreach (var smr in characterInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterial = skaterMat;
                smr.updateWhenOffscreen = true;
            }
        }

        // Attach and configure Animator with Humanoid Avatar
        Animator animator = characterInstance.GetComponent<Animator>();
        if (animator == null) animator = characterInstance.AddComponent<Animator>();
        if (modelAvatar != null) animator.avatar = modelAvatar;
        if (animController != null) animator.runtimeAnimatorController = animController;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Floating Neon Hoverboard DIRECTLY UNDERNEATH SNEAKERS
        Material boardMat = CreateColorMat("Assets/Materials/HoverboardMat.mat", new Color(0.0f, 0.88f, 0.88f));
        Material railMat = CreateColorMat("Assets/Materials/ObstacleMat.mat", new Color(0.85f, 0.15f, 0.15f));

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Hoverboard";
        board.transform.SetParent(player.transform, false);
        board.transform.localScale = new Vector3(0.52f, 0.045f, 1.15f);
        board.transform.localPosition = new Vector3(0f, -0.73f, 0.04f);
        board.GetComponent<Renderer>().sharedMaterial = boardMat;
        Object.DestroyImmediate(board.GetComponent<Collider>());

        GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "Rail_L";
        railL.transform.SetParent(board.transform, false);
        railL.transform.localScale = new Vector3(0.08f, 1.5f, 1.0f);
        railL.transform.localPosition = new Vector3(-0.45f, 0.1f, 0f);
        railL.GetComponent<Renderer>().sharedMaterial = railMat;
        Object.DestroyImmediate(railL.GetComponent<Collider>());

        GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "Rail_R";
        railR.transform.SetParent(board.transform, false);
        railR.transform.localScale = new Vector3(0.08f, 1.5f, 1.0f);
        railR.transform.localPosition = new Vector3(0.45f, 0.1f, 0f);
        railR.GetComponent<Renderer>().sharedMaterial = railMat;
        Object.DestroyImmediate(railR.GetComponent<Collider>());

        // Attach RunnerCharacterAnimator for hoverboard & banking
        RunnerCharacterAnimator rca = player.GetComponent<RunnerCharacterAnimator>();
        if (rca == null) rca = player.AddComponent<RunnerCharacterAnimator>();
        rca.hoverboard = board.transform;

        Debug.Log("Successfully created Animated Humanoid Runner with Adjusted Scale!");
    }

    private static Material CreateColorMat(string path, Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = c;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}

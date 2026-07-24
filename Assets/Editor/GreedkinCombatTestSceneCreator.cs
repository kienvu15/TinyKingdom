using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

internal static class GreedkinCombatTestSceneCreator
{
    private const string ScenePath = "Assets/0_TinyKingdom/0_Scenes/GreedkinCombatTest.unity";
    private const string SpawnConfigPath = "Assets/Configurations/Enemies/GreedkinTestSpawnConfig.asset";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/Greedkin/Greedkin_01/Greedkin_01.prefab";
    private const string WarriorPrefabPath = "Assets/Prefabs/Yellow Units/Warrior/Warrior_Yellow.prefab";
    private const string CastlePath = "Assets/Tiny Swords (Free Pack)/Buildings/Yellow Buildings/Castle.png";
    private const string TowerPath = "Assets/Tiny Swords (Free Pack)/Buildings/Yellow Buildings/Tower.png";
    private const string HousePath = "Assets/Tiny Swords (Free Pack)/Buildings/Yellow Buildings/House1.png";
    private const string BarracksPath = "Assets/Tiny Swords (Free Pack)/Buildings/Yellow Buildings/Barracks.png";

    [InitializeOnLoadMethod]
    private static void PopulateOnceAfterCompilation()
    {
        EditorApplication.delayCall += () => CreateOrPopulateScene(false);
    }

    [MenuItem("Tiny Kingdom/Greedkin/Create 2D Combat Test Scene")]
    private static void CreateOrPopulateScene()
    {
        CreateOrPopulateScene(true);
    }

    [MenuItem("Tiny Kingdom/Greedkin/Rebuild Top-Down Combat Test")]
    private static void RebuildTopDownCombatTest()
    {
        CreateOrPopulateScene(true);
    }

    private static void CreateOrPopulateScene(bool forceRebuild)
    {
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefabPath);
        GreedkinSpawnConfig spawnConfig = AssetDatabase.LoadAssetAtPath<GreedkinSpawnConfig>(SpawnConfigPath);
        if (enemyPrefab == null || warriorPrefab == null || spawnConfig == null)
        {
            Debug.LogError("Greedkin combat test scene could not be created because one of its required assets is missing.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeWhenFinished = false;
        if (!scene.isLoaded)
        {
            scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            scene.name = "GreedkinCombatTest";
            closeWhenFinished = true;
        }

        GameObject existingContent = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Greedkin Combat Test Content");
        if (existingContent != null)
        {
            bool alreadyTopDown = existingContent.transform.Find("Main Castle - 500 HP") != null;
            if (!forceRebuild && alreadyTopDown)
            {
                return;
            }

            Object.DestroyImmediate(existingContent);
        }

        GameObject contentRoot = new GameObject("Greedkin Combat Test Content");
        SceneManager.MoveGameObjectToScene(contentRoot, scene);
        CreateCamera(contentRoot.transform);
        NavMeshSurface surface = CreateNavMeshLane(contentRoot.transform);
        CreateGround(contentRoot.transform);
        Transform mainHouse = CreateMainCastle(contentRoot.transform);
        CreatePlayerUnit(warriorPrefab, "Knight Guard A - 90 HP", new Vector2(-0.2f, -0.65f), contentRoot.transform);
        CreatePlayerUnit(warriorPrefab, "Knight Guard B - 90 HP", new Vector2(1.3f, 0.6f), contentRoot.transform);
        CreateBuilding(contentRoot.transform, "Watch Tower - 220 HP", TowerPath, new Vector2(3.6f, 2.65f), 220f, 0.22f);
        CreateBuilding(contentRoot.transform, "Village House - 120 HP", HousePath, new Vector2(3.9f, -2.65f), 120f, 0.33f);
        CreateBuilding(contentRoot.transform, "Barracks - 300 HP", BarracksPath, new Vector2(2.35f, -2.0f), 300f, 0.27f);
        GreedkinRoute route = CreateRoute(contentRoot.transform);
        CreateSpawner(spawnConfig, route, mainHouse, contentRoot.transform);

        surface?.BuildNavMesh();

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (closeWhenFinished)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created 2D Greedkin combat test scene: {ScenePath}");
    }

    private static void CreateCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.6f;
        camera.backgroundColor = new Color(0.20f, 0.42f, 0.23f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, true);
    }

    private static NavMeshSurface CreateNavMeshLane(Transform parent)
    {
        GameObject surfaceObject = new GameObject("2D NavMesh Lane", typeof(NavMeshSurface));
        surfaceObject.AddComponent<BoxCollider>().size = new Vector3(20f, 0.1f, 10f);
        NavMeshSurface surface = surfaceObject.GetComponent<NavMeshSurface>();
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surfaceObject.transform.SetParent(parent, true);
        return surface;
    }

    private static void CreateGround(Transform parent)
    {
        // The orthographic camera's grass-green clear colour is the board.  A
        // plain transform documents the arena without adding a 3D placeholder.
        GameObject ground = new GameObject("Top-Down Grass Arena");
        ground.name = "Top-Down Grass Arena";
        ground.transform.SetParent(parent, false);
    }

    private static Transform CreateMainCastle(Transform parent)
    {
        return CreateBuilding(parent, "Main Castle - 500 HP", CastlePath, new Vector2(5.25f, 0.55f), 500f, 0.28f);
    }

    private static Transform CreateBuilding(Transform parent, string name, string spritePath, Vector2 position, float healthValue, float scale)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"Missing test building sprite: {spritePath}");
        }

        GameObject building = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CombatHealth));
        building.transform.SetParent(parent, false);
        building.transform.position = position;
        building.transform.localScale = Vector3.one * scale;

        SpriteRenderer spriteRenderer = building.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f);

        BoxCollider2D collider = building.GetComponent<BoxCollider2D>();
        collider.size = sprite.bounds.size;
        building.GetComponent<CombatHealth>().ConfigureRuntime(CombatTeam.Player, healthValue);
        return building.transform;
    }

    private static void CreatePlayerUnit(GameObject warriorPrefab, string name, Vector2 position, Transform parent)
    {
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(warriorPrefab);
        player.name = name;
        player.transform.position = position;
        player.transform.localScale = Vector3.one * 0.34f;
        CombatHealth health = player.GetComponent<CombatHealth>() ?? player.AddComponent<CombatHealth>();
        health.ConfigureRuntime(CombatTeam.Player, 90f);
        foreach (SpriteRenderer spriteRenderer in player.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f) + 20;
        }
        player.transform.SetParent(parent, true);
    }

    private static GreedkinRoute CreateRoute(Transform parent)
    {
        GameObject routeObject = new GameObject("Greedkin Route");
        routeObject.transform.SetParent(parent, false);
        GreedkinRoute route = routeObject.AddComponent<GreedkinRoute>();
        Transform[] waypoints =
        {
            CreateWaypoint("Waypoint 01", -4.1f, parent),
            CreateWaypoint("Waypoint 02", -2.2f, parent),
            CreateWaypoint("Waypoint 03", 0.1f, parent),
            CreateWaypoint("Waypoint 04", 2.5f, parent),
        };
        SerializedObject serializedRoute = new SerializedObject(route);
        SerializedProperty waypointProperty = serializedRoute.FindProperty("waypoints");
        waypointProperty.arraySize = waypoints.Length;
        for (int index = 0; index < waypoints.Length; index++)
        {
            waypointProperty.GetArrayElementAtIndex(index).objectReferenceValue = waypoints[index];
        }
        serializedRoute.ApplyModifiedPropertiesWithoutUndo();
        return route;
    }

    private static Transform CreateWaypoint(string name, float x, Transform parent)
    {
        GameObject waypoint = new GameObject(name);
        waypoint.transform.position = new Vector3(x, 0f, 0f);
        waypoint.transform.SetParent(parent, true);
        return waypoint.transform;
    }

    private static void CreateSpawner(GreedkinSpawnConfig config, GreedkinRoute route, Transform mainHouse, Transform parent)
    {
        GameObject spawnerObject = new GameObject("Greedkin Level 1 Spawner");
        spawnerObject.transform.position = new Vector3(-6.3f, 0f, 0f);
        spawnerObject.transform.SetParent(parent, true);
        GreedkinSpawner spawner = spawnerObject.AddComponent<GreedkinSpawner>();
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("spawnConfig").objectReferenceValue = config;
        serializedSpawner.FindProperty("route").objectReferenceValue = route;
        serializedSpawner.FindProperty("mainHouse").objectReferenceValue = mainHouse;
        serializedSpawner.FindProperty("spawnPoint").objectReferenceValue = spawnerObject.transform;
        serializedSpawner.FindProperty("spawnOnStart").boolValue = true;
        serializedSpawner.FindProperty("spawnSpacing").floatValue = 0.65f;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings()
    {
        if (EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
        {
            return;
        }
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes
            .Append(new EditorBuildSettingsScene(ScenePath, true))
            .ToArray();
        EditorBuildSettings.scenes = scenes;
    }
}

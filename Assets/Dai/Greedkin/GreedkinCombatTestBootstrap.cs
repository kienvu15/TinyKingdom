using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds the 2D test arena at runtime.  It keeps the visual XY world and
/// creates the matching XZ NavMesh surface required by GreedkinEnemyBrain.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public sealed class GreedkinCombatTestBootstrap : MonoBehaviour
{
    [SerializeField] private GreedkinSpawnConfig levelOneSpawnConfig;
    [SerializeField] private GameObject playerWarriorPrefab;

    private GameObject editorPreviewRoot;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            CreateEditorPreview();
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            CreateEditorPreview();
            return;
        }

        // The editor scene creator saves a visible arena into the scene.  Keep
        // this runtime bootstrap only as a fallback for a stripped scene.
        if (GameObject.Find("Greedkin Level 1 Spawner") != null)
        {
            return;
        }

        if (levelOneSpawnConfig == null || playerWarriorPrefab == null)
        {
            Debug.LogError("GreedkinCombatTestBootstrap is missing its Spawn Config or Player Warrior prefab.", this);
            enabled = false;
            return;
        }

        CreateCamera();
        NavMeshSurface surface = CreateNavMeshLane();
        Transform mainHouse = CreateMainHouse();
        CreatePlayerWarrior();
        GreedkinRoute route = CreateRoute();
        CreateSpawner(route, mainHouse);
        surface.BuildNavMesh();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying && editorPreviewRoot != null)
        {
            DestroyImmediate(editorPreviewRoot);
        }
    }

    private void CreateEditorPreview()
    {
        // The saved test scene supplies the full pixel-art arena.  Preview
        // objects are only a fallback when this component is used elsewhere.
        if (GameObject.Find("Greedkin Combat Test Content") != null ||
            editorPreviewRoot != null || levelOneSpawnConfig == null || playerWarriorPrefab == null)
        {
            return;
        }

        editorPreviewRoot = new GameObject("[Preview] Combat Test Arena");
        editorPreviewRoot.hideFlags = HideFlags.DontSaveInEditor;
        editorPreviewRoot.transform.SetParent(transform, false);

        GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
        house.name = "[Preview] Main House - 250 HP";
        house.transform.SetParent(editorPreviewRoot.transform, false);
        house.transform.localPosition = new Vector3(7f, 0f, 0f);
        house.transform.localScale = new Vector3(1.5f, 2.5f, 1f);

        GameObject warrior = Instantiate(playerWarriorPrefab, new Vector3(1.5f, 0f, 0f), Quaternion.identity);
        warrior.name = "[Preview] Player Warrior - 60 HP";
        warrior.transform.SetParent(editorPreviewRoot.transform, true);
        warrior.transform.localScale = Vector3.one * 0.34f;

        if (levelOneSpawnConfig.entries.Count > 0 && levelOneSpawnConfig.entries[0].prefab != null)
        {
            GameObject enemy = Instantiate(levelOneSpawnConfig.entries[0].prefab, new Vector3(-7f, 0f, 0f), Quaternion.identity);
            enemy.name = "[Preview] Greedkin Level 1";
            enemy.transform.SetParent(editorPreviewRoot.transform, true);
        }

        CreatePreviewWaypoint("[Preview] Waypoint 01", -4f);
        CreatePreviewWaypoint("[Preview] Waypoint 02", -1f);
        CreatePreviewWaypoint("[Preview] Waypoint 03", 3.5f);
    }

    private void CreatePreviewWaypoint(string label, float x)
    {
        GameObject waypoint = new GameObject(label);
        waypoint.transform.SetParent(editorPreviewRoot.transform, false);
        waypoint.transform.localPosition = new Vector3(x, 0f, 0f);
    }

    private static void CreateCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.17f);
    }

    private static NavMeshSurface CreateNavMeshLane()
    {
        GameObject lane = new GameObject("2D NavMesh Lane", typeof(NavMeshSurface), typeof(BoxCollider));
        lane.GetComponent<BoxCollider>().size = new Vector3(20f, 0.1f, 6f);
        NavMeshSurface surface = lane.GetComponent<NavMeshSurface>();
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        return surface;
    }

    private static Transform CreateMainHouse()
    {
        GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
        house.name = "Main House - 250 HP";
        house.transform.position = new Vector3(7f, 0f, 0f);
        house.transform.localScale = new Vector3(1.5f, 2.5f, 1f);
        // Keep the primitive only as a visible 2D target; the NavMesh lane is
        // the sole 3D navigation source.
        house.GetComponent<Collider>().enabled = false;
        BoxCollider2D collider = house.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.5f, 2.5f);
        CombatHealth health = house.AddComponent<CombatHealth>();
        health.ConfigureRuntime(CombatTeam.Player, 250f);
        return house.transform;
    }

    private void CreatePlayerWarrior()
    {
        GameObject warrior = Instantiate(playerWarriorPrefab, new Vector3(1.5f, 0f, 0f), Quaternion.identity);
        warrior.name = "Player Warrior Target - 60 HP";
        CombatHealth health = warrior.GetComponent<CombatHealth>() ?? warrior.AddComponent<CombatHealth>();
        health.ConfigureRuntime(CombatTeam.Player, 60f);
    }

    private static GreedkinRoute CreateRoute()
    {
        GameObject routeObject = new GameObject("Greedkin Route");
        GreedkinRoute route = routeObject.AddComponent<GreedkinRoute>();
        route.SetWaypoints(
            CreateWaypoint("Waypoint 01", -4f),
            CreateWaypoint("Waypoint 02", -1f),
            CreateWaypoint("Waypoint 03", 3.5f));
        return route;
    }

    private static Transform CreateWaypoint(string label, float x)
    {
        GameObject waypoint = new GameObject(label);
        waypoint.transform.position = new Vector3(x, 0f, 0f);
        return waypoint.transform;
    }

    private void CreateSpawner(GreedkinRoute route, Transform mainHouse)
    {
        GameObject spawnerObject = new GameObject("Greedkin Level 1 Spawner");
        spawnerObject.transform.position = new Vector3(-7f, 0f, 0f);
        GreedkinSpawner spawner = spawnerObject.AddComponent<GreedkinSpawner>();
        spawner.Configure(levelOneSpawnConfig, route, mainHouse, spawnerObject.transform);
    }
}

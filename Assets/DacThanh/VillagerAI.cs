using UnityEngine;
using UnityEngine.AI;

public class VillagerAI : MonoBehaviour
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public float arriveDistance = 0.5f;
    
    [Header("Random Area")]
    public float minX = -10;
    public float maxX = 10;
    public float minZ = -10;
    public float maxZ = 10;

    [Header("Target")]
    public Transform tool;

    private Vector3 randomTarget;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        SetRandomDestination();
    }

    void Update()
    {
        // Nếu Tool tồn tại thì luôn chạy tới Tool
        if (tool != null && tool.gameObject.activeInHierarchy)
        {
            agent.SetDestination(tool.position);
            return;
        }

        // Đã tới điểm ngẫu nhiên
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            SetRandomDestination();
        }
    }

    void SetRandomDestination()
    {
        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);

        randomTarget = new Vector3(x, transform.position.y, z);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomTarget, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
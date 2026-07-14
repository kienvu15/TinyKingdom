using UnityEngine;
using UnityEngine.AI;

public class Villager : MonoBehaviour
{
    //self
    private NavMeshAgent agent;
    //move
    [Header("Random Area")]
    public float arriveDistance = 0.5f;// kiểm soát sai số của Nav AI
    public float minX = -10;
    public float maxX = 10;
    public float minZ = -10;
    public float maxZ = 10;
    private Vector3 randomTarget;
    public Vector3 TargetPosition;
    
    //bool
    public bool hasTool;
    public bool hasMoveCommand;
    void Start()
    {
        hasMoveCommand =false;
        hasTool = false;
        if (agent == null)
            agent = gameObject.GetComponent<NavMeshAgent>();

        SetRandomDestination();
    }
    void Update()
    {
        if (!hasMoveCommand && !hasTool)
        {
            SetRandomDestination();
        }
        else if(!hasMoveCommand && hasTool)
        {
            MoveCommand(gameObject.transform.position);
        }
        else
        {
            MoveCommand(TargetPosition);
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
            MoveCommand(hit.position);
        }
    }

    public void MoveCommand(Vector3 position)
    {
        agent.SetDestination(position);
    }
    
}

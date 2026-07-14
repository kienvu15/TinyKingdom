using UnityEngine;

public class Villager : MonoBehaviour
{
    [Header("Move")]
    public float speed = 3f;
    public float arriveDistance = 0.1f;

    [Header("Random Area")]
    public float minX = -10;
    public float maxX = 10;
    public float minY = -10;
    public float maxY = 10;

    public Vector2 TargetPosition;

    private Vector2 randomTarget;

    public bool hasTool;
    public bool hasMoveCommand;

    private void Start()
    {
        hasTool = false;
        hasMoveCommand = false;

        SetRandomDestination();
    }

    private void Update()
    {
        if (!hasMoveCommand && !hasTool)
        {
            if (Vector2.Distance(transform.position, randomTarget) <= arriveDistance)
            {
                SetRandomDestination();
            }

            MoveCommand(randomTarget);
        }
        else if (hasMoveCommand)
        {
            MoveCommand(TargetPosition);
        }
    }

    void SetRandomDestination()
    {
        randomTarget = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
    }

    public void MoveCommand(Vector2 position)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            position,
            speed * Time.deltaTime);
    }
}
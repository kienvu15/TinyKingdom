using UnityEngine;

public class Person : MonoBehaviour
{
    [Header("Job")]
    public JobType jobType = JobType.Person;

    [Header("Move")]
    public float speed = 3f;
    public float arriveDistance = 0.1f;

    [Header("Random Area")]
    public Vector2 TargetPosition;
    private Vector2 randomTarget;
    private Vector2 spawnpointPosition;
    // All Bool variable
    public bool hasTool;
    public bool hasMoveCommand;
    public bool isSelected;
    public bool isDay = true;

    private void Start()
    {
        spawnpointPosition = transform.position;
        hasTool = false;
        hasMoveCommand = false;
        isSelected = false;
        VillagerMove(); // chọn điểm random đầu tiên
    }

    private void Update()
    {
        Jobs();
    }

    private void OnMouseDown()
    {
        if (jobType == JobType.Person) return;

        isSelected = !isSelected;
        if (isSelected)
        {
            hasMoveCommand = true; // lưu ý: xem ghi chú bên dưới
        }
    }

    public void Move(Vector2 position)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            position,
            speed * Time.deltaTime);
    }

    // ====== Job dispatcher ======
    void Jobs()
    {
        switch (jobType)
        {
            case JobType.Person:
                DoPersonJob();
                break;
            case JobType.Villager:
                DoVillagerJob();
                break;
            case JobType.Archer:
                DoArcherJob();
                break;
        }
    }

    #region Person Jobs

    void DoPersonJob()
    {
        if (Vector2.Distance(transform.position, randomTarget) <= arriveDistance)
        {
            PersonMove(); 
        }
        else
        {
            Move(randomTarget);
        }
    }
    void PersonMove()
    {
        float minX = -4;
        float maxX = 4;
        float minY = -4;
        float maxY = 4;

        randomTarget = spawnpointPosition + new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
        // không gọi Move ở đây, Update() lo việc di chuyển mỗi frame
    }

    void MoveToCastle(){}
    #endregion
    #region Villager Jobs
// ====== Villager ======
    void DoVillagerJob()
    {
        if (hasMoveCommand)
        {
            Move(TargetPosition);
            return;
        }

        if (!hasTool)
        {
            FindTool(); // chưa có công cụ thì đi tìm
            if (hasMoveCommand) return; // vừa tìm thấy tool và có lệnh di chuyển thì không random nữa
        }

        if (Vector2.Distance(transform.position, randomTarget) <= arriveDistance)
        {
            VillagerMove(); // đến nơi rồi thì chọn điểm mới
        }
        else
        {
            Move(randomTarget);
        }
    }

    void VillagerMove()
    {
        float minX = -10;
        float maxX = 10;
        float minY = -10;
        float maxY = 10;

        randomTarget = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
    }

    void FindTool()
    {
        Tool[] tools = FindObjectsOfType<Tool>();

        float minDistance = Mathf.Infinity;
        Tool nearestTool = null;

        foreach (Tool tool in tools)
        {
            if (tool == null) continue;
            if (tool.owner != null) continue;

            float distance = Vector2.Distance(
                transform.position,
                tool.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTool = tool;
            }
        }

        if (nearestTool != null)
        {
            nearestTool.owner = gameObject;
            TargetPosition = nearestTool.transform.position;
            hasMoveCommand = true;
        }
    }
    #endregion
    #region Archer Jobs
    // ====== Archer ======
    void DoArcherJob()
    {
        if (!hasMoveCommand)
        {
            if (isDay) FindTower();
            return;
        }

        Move(TargetPosition);
    }

    private void FindTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();

        float minDistance = Mathf.Infinity;
        Tower nearestTower = null;

        foreach (Tower tower in towers)
        {
            if (tower == null) continue;
            if (tower.owner != null) continue;

            float distance = Vector2.Distance(
                transform.position,
                tower.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTower = tower;
            }
        }

        if (nearestTower != null)
        {
            nearestTower.owner = gameObject;
            TargetPosition = nearestTower.transform.position;
            hasMoveCommand = true;
        }
    }
#endregion

    void Command() { }
}
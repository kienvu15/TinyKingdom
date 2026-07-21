using UnityEngine;

public enum JobType
{
    Person,
    Villager,
    Archer,
    Spearman
    // Thêm job mới ở đây trong tương lai
}
[CreateAssetMenu(fileName = "JobData", menuName = "Game/Job Data")]
public class JobData : ScriptableObject
{
    [Header("Identity")]
    public JobType jobType = JobType.Person;
    public string jobName;

    [Header("Movement")]
    public float speed = 3f;
}

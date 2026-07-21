using UnityEngine;

public enum ToolType
{
    None,
    Bow,
    Spear
    // Thêm tool mới ở đây trong tương lai
}

[CreateAssetMenu(fileName = "ToolData", menuName = "Game/Tool Data")]
public class ToolData : ScriptableObject
{
    [Header("Identity")]
    public ToolType toolType;
    public string toolName;

    [Header("Prefab (tuỳ chọn, để spawn tool này)")]
    public GameObject prefab;

    [Header("Kết quả khi nhặt")]
    [Tooltip("Person nhặt tool này sẽ đổi sang job nào")]
    public JobData resultingJob;
}

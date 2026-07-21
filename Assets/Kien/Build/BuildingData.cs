using UnityEngine;

/// <summary>
/// Một mốc tiến độ xây dựng: khi coin tích lũy >= coinThreshold thì đổi sang sprite này.
/// Ví dụ: 0 coin -> nền móng, 10 coin -> tường gạch, 25 coin -> tháp hoàn thiện.
/// </summary>
[System.Serializable]
public struct BuildStage
{
    public int coinThreshold;
    public Sprite sprite;
    [Tooltip("Prefab sẽ được SPAWN (Instantiate) tại vị trí building khi đạt tới stage này. " +
             "Dùng cho hiệu ứng (nổ, bụi...) hoặc vật thể mới xuất hiện (bụi cây, cờ...). " +
             "LƯU Ý: đây phải là Prefab asset kéo từ Project, KHÔNG phải object có sẵn trong scene.")]
    public GameObject[] prefabsToSpawnOnThisStage;
}

/// <summary>
/// Dữ liệu định nghĩa MỘT LOẠI building (Wall, ArcherTower, Farm...).
/// Muốn thêm building mới -> Chuột phải > Create > Building > New Building Data, KHÔNG cần sửa code.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Thông tin chung")]
    public string buildingId;
    public string displayName;

    [Header("Chi phí")]
    public int totalCoinCost = 25;

    [Header("Các mốc hiển thị theo tiến độ (sắp xếp tăng dần theo coinThreshold)")]
    public BuildStage[] stages;

    [Header("Sau khi xây xong")]
    [Tooltip("Component/Behaviour sẽ được bật khi building hoàn thành, ví dụ ArcherTowerBehaviour, WallBehaviour...")]
    public MonoBehaviour completedBehaviourPrefab; // hoặc dùng string + factory nếu muốn tách rời hẳn

    /// <summary>
    /// Tự tìm stage có coinThreshold lớn nhất mà vẫn &lt;= coinCount hiện tại.
    /// KHÔNG phụ thuộc thứ tự phần tử trong mảng "stages" (dù bạn kéo-thả lộn xộn trong Inspector
    /// hoặc bấm "+" thêm phần tử mới vào cuối, kết quả vẫn đúng).
    /// </summary>
    public BuildStage GetStageForCoinCount(int coinCount)
    {
        bool found = false;
        BuildStage best = default;

        foreach (var stage in stages)
        {
            if (coinCount >= stage.coinThreshold)
            {
                if (!found || stage.coinThreshold > best.coinThreshold)
                {
                    best = stage;
                    found = true;
                }
            }
        }

        // Nếu coinCount nhỏ hơn threshold của mọi stage (VD stage thấp nhất là 5 mà coinCount = 0),
        // fallback về stage có threshold nhỏ nhất để tránh trả về default rỗng (sprite = null).
        if (!found && stages.Length > 0)
        {
            best = stages[0];
            foreach (var stage in stages)
                if (stage.coinThreshold < best.coinThreshold)
                    best = stage;
        }

        return best;
    }
}
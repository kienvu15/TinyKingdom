using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gắn component này lên building đặt trong scene (hoặc trên prefab site trống chờ xây).
/// Không chứa logic riêng của từng loại building - chỉ lo:
///   1. Nhận coin qua ICoinReceiver
///   2. Đổi sprite theo tiến độ (dựa trên BuildingData.stages)
///   3. Bắn event khi hoàn thành, cho phép gắn thêm behaviour riêng (tháp bắn cung, tường chặn đường...)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingConstructionSite : MonoBehaviour, ICoinReceiver
{
    [Header("Cấu hình building")]
    public BuildingData data;

    [Header("Trạng thái (chỉ đọc, debug)")]
    [SerializeField] private int currentCoin = 0;
    [SerializeField] private bool isCompleted = false;

    [Header("Events - chỗ để expand behavior riêng cho từng loại building")]
    public UnityEvent onStageChanged;
    public UnityEvent onConstructionCompleted;

    private SpriteRenderer sr;
    private int lastStageThreshold = -1; // để biết stage có thực sự VỪA đổi hay không, tránh spawn lặp lại prefab

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        RefreshVisual();
    }

    // ---------- ICoinReceiver ----------
    public bool CanReceiveCoin() => !isCompleted;

    public Vector3 GetReceivePoint() => transform.position;

    public void ReceiveCoin(int amount)
    {
        if (isCompleted) return;

        currentCoin += amount;
        RefreshVisual();

        if (currentCoin >= data.totalCoinCost)
        {
            currentCoin = data.totalCoinCost;
            CompleteConstruction();
        }
    }
    // ------------------------------------

    private void RefreshVisual()
    {
        if (data == null || data.stages.Length == 0) return;

        BuildStage stage = data.GetStageForCoinCount(currentCoin);
        sr.sprite = stage.sprite;

        // Chỉ spawn prefab khi ĐÂY LÀ LẦN ĐẦU đạt tới stage này (tránh Instantiate lặp lại
        // mỗi khi ReceiveCoin được gọi thêm trong lúc coin vẫn đang ở cùng 1 stage).
        bool justEnteredNewStage = stage.coinThreshold != lastStageThreshold;
        lastStageThreshold = stage.coinThreshold;

        if (justEnteredNewStage && stage.prefabsToSpawnOnThisStage != null)
        {
            foreach (var prefab in stage.prefabsToSpawnOnThisStage)
                if (prefab != null)
                    Instantiate(prefab, transform.position, Quaternion.identity, transform);
        }

        onStageChanged?.Invoke();
    }

    private void CompleteConstruction()
    {
        isCompleted = true;
        onConstructionCompleted?.Invoke();
        // Nếu muốn tự động bật behaviour hoàn thiện từ data.completedBehaviourPrefab,
        // xử lý ở đây (Instantiate hoặc GetComponent<>().enabled = true tuỳ cách bạn tổ chức).
    }

    public float ProgressPercent => data == null ? 0 : (float)currentCoin / data.totalCoinCost;
}
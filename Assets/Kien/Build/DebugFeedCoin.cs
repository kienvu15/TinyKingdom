using UnityEngine;

/// <summary>
/// CHỈ DÙNG ĐỂ TEST. Gắn tạm lên object "Base" cùng với BuildingConstructionSite.
/// Nhấn phím K để "ném" 1 coin vào building, xem sprite/tiến độ đổi có đúng không.
/// Xong test thì remove component này.
/// </summary>
public class DebugFeedCoin : MonoBehaviour
{
    public BuildingConstructionSite target;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            target.ReceiveCoin(1);
            Debug.Log($"Progress: {target.ProgressPercent * 100f:0}%");
        }
    }
}

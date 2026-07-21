using UnityEngine;

/// <summary>
/// Bất kỳ object nào muốn nhận coin ném vào (building, NPC nhận lương, hòm...) đều implement interface này.
/// PlayerWallet hoặc hệ thống ném xu chỉ cần gọi qua interface, không quan tâm implement bên trong.
/// </summary>
public interface ICoinReceiver
{
    /// <summary>Trả về true nếu vẫn còn nhận coin được (chưa full/xong việc).</summary>
    bool CanReceiveCoin();

    /// <summary>Vị trí để coin bay tới (thường là điểm "miệng" nhận coin).</summary>
    Vector3 GetReceivePoint();

    /// <summary>Nhận 1 lượng coin.</summary>
    void ReceiveCoin(int amount);
}

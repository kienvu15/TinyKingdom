using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("Cài đặt UI")]
    public TextMeshProUGUI coinText;

    public static CoinUI Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (PlayerWallet.Instance != null)
        {
            UpdateCoinDisplay(PlayerWallet.Instance.coinCount);
        }
        else
        {
            UpdateCoinDisplay(0);
        }
    }

    public void UpdateCoinDisplay(int currentCoins)
    {
        if (coinText != null)
        {
            coinText.text = "Coins: " + currentCoins.ToString();
        }
    }
}
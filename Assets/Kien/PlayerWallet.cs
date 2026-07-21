using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Header("Cài đặt Hút Coin")]
    public float magnetRadius = 3f;
    public string coinTag = "Coin";
    public float pullSpeed = 10f;
    public float collectionDistance = 0.2f;
    public float offsetY = 5f;

    [Header("Dữ liệu")]
    public int coinCount = 0;

    public static PlayerWallet Instance;

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

    void Update()
    {
        CheckAndPullCoins();
    }
    void CheckAndPullCoins()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector2)(transform.position + Vector3.up * offsetY), magnetRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(coinTag))
            {
                Transform coinTransform = col.transform;

                coinTransform.position = Vector2.MoveTowards(
                    coinTransform.position,
                    transform.position,
                    pullSpeed * Time.deltaTime
                );

                float distance = Vector2.Distance(coinTransform.position, transform.position);
                if (distance <= collectionDistance)
                {
                    AddCoin(1);
                    Destroy(col.gameObject);
                }
            }
        }
    }

    public void AddCoin(int amount)
    {
        coinCount += amount;
        if (CoinUI.Instance != null)
        {
            CoinUI.Instance.UpdateCoinDisplay(coinCount);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)(transform.position + Vector3.up * offsetY), magnetRadius);
    }
}
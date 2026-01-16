using UnityEngine;

public class BasketTarget : MonoBehaviour
{
    public enum BasketType { Normal, Golden }

    [Header("Type")]
    [SerializeField] private BasketType type = BasketType.Normal;
    [SerializeField] private int normalScore = 1;
    [SerializeField] private int goldenScore = 3;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material goldenMaterial;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;   // Xï˚å¸Ç…ó¨Ç∑
    [SerializeField] private float destroyX = -12f;

    [Header("2.5D plane")]
    [SerializeField] private bool lockZ = true;
    [SerializeField] private float fixedZ = 0f;

    public void SetType(BasketType t)
    {
        type = t;
        ApplyMaterial();
    }

    private void Awake()
    {
        ApplyMaterial();
    }

    private void Update()
    {
        // âEÅ®ç∂Ç÷ó¨Ç∑
        transform.position += Vector3.left * (moveSpeed * Time.deltaTime);

        if (transform.position.x <= destroyX)
            Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (!lockZ) return;
        var p = transform.position;
        p.z = fixedZ;
        transform.position = p;
    }

    private void ApplyMaterial()
    {
        if (meshRenderer == null) return;

        if (type == BasketType.Golden && goldenMaterial != null)
            meshRenderer.material = goldenMaterial;
        else if (normalMaterial != null)
            meshRenderer.material = normalMaterial;
    }

    private int GetScoreValue() => type == BasketType.Golden ? goldenScore : normalScore;

    private void OnTriggerEnter(Collider other)
    {
        var egg = other.GetComponent<EggProjectile>();
        if (egg == null) return;

        if (EggCatcherGameManager.Instance != null)
        {
            EggCatcherGameManager.Instance.AddScore(egg.OwnerPlayerId, GetScoreValue());
        }

        Destroy(egg.gameObject);
        // Ç©Ç≤Ç‡è¡ÇµÇΩÇ¢Ç»ÇÁÅFDestroy(gameObject);
    }
}
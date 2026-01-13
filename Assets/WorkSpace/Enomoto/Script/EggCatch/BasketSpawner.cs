using UnityEngine;

public class BasketSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject basketPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 1.2f;

    [Header("Golden Chance")]
    [Range(0f, 1f)]
    [SerializeField] private float goldenChance = 0.15f;

    private float nextSpawn;

    private void Update()
    {
        if (EggCatcherGameManager.Instance == null) return;
        if (!EggCatcherGameManager.Instance.IsRunning) return;

        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + spawnInterval;
            Spawn();
        }
    }

    private void Spawn()
    {
        if (basketPrefab == null || spawnPoint == null) return;

        var go = Instantiate(basketPrefab, spawnPoint.position, spawnPoint.rotation);

        var basket = go.GetComponent<BasketTarget>();
        if (basket != null)
        {
            bool golden = Random.value < goldenChance;
            basket.SetType(golden ? BasketTarget.BasketType.Golden : BasketTarget.BasketType.Normal);
        }
    }
}

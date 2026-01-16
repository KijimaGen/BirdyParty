using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BirdShooter : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int playerId;

    [Header("Shooting")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private float shootForce = 12f;
    [SerializeField] private float fireCooldown = 0.25f;

    [Header("2.5D Lock")]
    [SerializeField] private float fixedZ = 0f;

    private float nextFireTime;

    public void SetupPlayer(int id) => playerId = id;

    // Åö Send Messages óp
    void OnFire()
    {
        if (EggCatcherGameManager.Instance == null) return;
        if (!EggCatcherGameManager.Instance.IsRunning) return;

        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        Shoot();
    }

    private void Shoot()
    {
        if (muzzle == null || eggPrefab == null) return;

        var go = Instantiate(eggPrefab, muzzle.position, Quaternion.identity);

        var egg = go.GetComponent<EggProjectile>();
        if (egg != null) egg.SetOwner(playerId);

        // Zå≈íË
        var pos = go.transform.position;
        pos.z = fixedZ;
        go.transform.position = pos;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Vector3.down * shootForce, ForceMode.Impulse);
        }
    }

    private void LateUpdate()
    {
        var p = transform.position;
        p.z = fixedZ;
        transform.position = p;
    }
}
using System;
using UnityEngine;

public class DiceActor : MonoBehaviour
{
    public int PlayerId { get; private set; }
    public bool IsRolling { get; private set; }
    public bool IsEliminated { get; private set; }

    public event Action<DiceActor, int> OnRollFinalized;

    [Header("Roll Settings")]
    [SerializeField] private float rollImpulse = 7f;
    [SerializeField] private float rollTorque = 18f;
    [SerializeField] private float settleSpeed = 0.08f;
    [SerializeField] private float settleAngularSpeed = 0.8f;
    [SerializeField] private float stableTimeRequired = 0.35f;

    private Rigidbody rb;
    private float stableTimer;
    private int? groundFace; // 接地している面(=下の面)
    private int lastResolvedFace;

    public void Setup(int playerId, Material mat)
    {
        PlayerId = playerId;
        rb = GetComponent<Rigidbody>();

        // 色（材質）変更
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null && mat != null) renderer.material = mat;

        // FaceSensorを初期化
        foreach (var sensor in GetComponentsInChildren<DiceFaceSensor>())
            sensor.Init(this);
    }

    public void BeginTurn()
    {
        IsRolling = false;
        stableTimer = 0f;
        groundFace = null;
        lastResolvedFace = 0;
    }

    public void Eliminate()
    {
        IsEliminated = true;
        gameObject.SetActive(false); // 非表示（要件どおり）
    }

    // ボタン入力で呼ぶ（または自動ロールでもOK）
    public void RollNow()
    {
        if (IsEliminated || IsRolling) return;

        IsRolling = true;
        stableTimer = 0f;
        groundFace = null;

        // 少し浮かせてから力を加える（床にめり込み事故防止）
        transform.position += Vector3.up * 0.15f;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        var dir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1f, UnityEngine.Random.Range(-1f, 1f)).normalized;
        rb.AddForce(dir * rollImpulse, ForceMode.Impulse);

        var torque = UnityEngine.Random.insideUnitSphere * rollTorque;
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public void ForceRoll()
    {
        // 「まだ振ってない」人のみ強制ロール
        if (IsEliminated) return;
        if (!IsRolling) RollNow();
    }

    public void NotifyFaceTouchingGround(int touchingFaceValue)
    {
        // 直近の接地面を記録
        groundFace = touchingFaceValue;
    }

    private void Update()
    {
        if (!IsRolling || IsEliminated) return;

        // 停止判定
        bool slow =
            rb.velocity.magnitude < settleSpeed &&
            rb.angularVelocity.magnitude < settleAngularSpeed;

        if (slow && groundFace.HasValue)
        {
            stableTimer += Time.deltaTime;
            if (stableTimer >= stableTimeRequired)
            {
                int down = groundFace.Value;          // 下の面
                int up = OppositeFace(down);          // 上の面 = 確定出目
                lastResolvedFace = up;

                IsRolling = false;
                OnRollFinalized?.Invoke(this, up);
            }
        }
        else
        {
            stableTimer = 0f;
        }
    }

    private int OppositeFace(int face)
    {
        // 一般的なダイス（1-6, 2-5, 3-4）
        return face switch
        {
            1 => 6,
            6 => 1,
            2 => 5,
            5 => 2,
            3 => 4,
            4 => 3,
            _ => 0
        };
    }
}

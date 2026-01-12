using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDiceController : MonoBehaviour
{
    [Header("Dice Visual")]
    [SerializeField] private Rigidbody diceRigidbody;
    [SerializeField] private DiceFaceDetector faceDetector;

    [Header("Roll Params")]
    [SerializeField] private Vector3 rollForce = new Vector3(0, 6f, 0);
    [SerializeField] private Vector3 rollTorque = new Vector3(12f, 18f, 10f);
    [SerializeField] private float settleCheckMinSeconds = 1.0f;

    [Header("Material")]
    [SerializeField] private Renderer diceRenderer;
    [SerializeField] private Material[] materials; // 4つ程度
    [SerializeField] private int materialIndexDefault = 0;

    public bool HasRolledThisTurn { get; private set; }
    public int LastFaceValue { get; private set; } = 0;

    private bool rollEnabled = false;
    private Coroutine rollingRoutine;

    private PlayerInfomation ownerInfo;
    private bool isOnline;

    private InputAction rollAction;

    private void OnEnable()
    {
        // PlayerInputを親から探す（プレイヤールートに置いてある前提）
        var playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            // Action名が "Roll" であること
            rollAction = playerInput.actions["Roll"];
            if (rollAction != null)
            {
                rollAction.performed += OnRollPerformed;
            }
        }

        ResetForNewTurn();
    }

    private void OnDisable()
    {
        if (rollAction != null)
        {
            rollAction.performed -= OnRollPerformed;
            rollAction = null;
        }
    }

    private void OnRollPerformed(InputAction.CallbackContext ctx)
    {
        // 既存処理に流す
        OnRoll(ctx);
    }

    private void Awake()
    {
        ownerInfo = GetComponentInParent<PlayerInfomation>(); // dicePlayerの子に居る想定
        isOnline = GameManager.instance != null && GameManager.instance.IsOnline();

        if (diceRigidbody == null) diceRigidbody = GetComponentInChildren<Rigidbody>(true);
        if (faceDetector == null) faceDetector = GetComponentInChildren<DiceFaceDetector>(true);
        if (diceRenderer == null) diceRenderer = GetComponentInChildren<Renderer>(true);

        ApplyMaterialFromPlayerInfo();
    }

    public void SetRollEnabled(bool enabled)
    {
        rollEnabled = enabled;
        if (!enabled)
        {
            // 入力停止時に安定させたいならここで止める等
        }
    }

    public void ResetForNewTurn()
    {
        HasRolledThisTurn = false;
        LastFaceValue = 0;
        if (diceRigidbody != null)
        {
            diceRigidbody.velocity = Vector3.zero;
            diceRigidbody.angularVelocity = Vector3.zero;
        }
        if (faceDetector != null) faceDetector.ClearContact();
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!rollEnabled) return;

        // オンライン：自分のPlayerだけ反応（PhotonViewが上位にある想定）
        if (isOnline)
        {
            var pv = GetComponentInParent<PhotonView>();
            if (pv != null && !pv.IsMine) return;
        }

        TryRoll();
    }

    public void AutoRoll()
    {
        if (!rollEnabled) return;
        // オンライン：自分のPlayerだけ
        if (isOnline)
        {
            var pv = GetComponentInParent<PhotonView>();
            if (pv != null && !pv.IsMine) return;
        }

        TryRoll();
    }

    private void TryRoll()
    {
        if (HasRolledThisTurn) return;
        HasRolledThisTurn = true;

        if (rollingRoutine != null) StopCoroutine(rollingRoutine);
        rollingRoutine = StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        if (diceRigidbody == null || faceDetector == null) yield break;

        faceDetector.ClearContact();
        LastFaceValue = 0;

        diceRigidbody.WakeUp();
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        diceRigidbody.AddForce(rollForce, ForceMode.Impulse);
        Vector3 t = new Vector3(
            Random.Range(-rollTorque.x, rollTorque.x),
            Random.Range(-rollTorque.y, rollTorque.y),
            Random.Range(-rollTorque.z, rollTorque.z)
        );
        diceRigidbody.AddTorque(t, ForceMode.Impulse);

        // 最低ちょっと転がす（早取り防止）
        yield return new WaitForSeconds(0.5f);

        float stableSeconds = 0.25f;   // 0.2～0.35で調整
        float maxWait = 8f;            // ここまでに確定できなければ異常
        float start = Time.time;

        // ★確定できるまで待つ（着地前に確定しない）
        while (Time.time - start < maxWait)
        {
            // “面が安定している” を優先
            if (faceDetector.TryGetUpFaceIfStable(stableSeconds, out int face))
            {
                LastFaceValue = face;
                break;
            }
            yield return null;
        }

        // まだ0なら「接地が取れていない」ので、ここでは確定しない（1を出さない）
        if (LastFaceValue == 0)
        {
            Debug.LogWarning("[Dice] Could not determine face (no stable ground contact). Check FaceSensor/Ground.");
            yield break;
        }

        // ★ここで初めてUIへ反映（着地後）
        if (DiceUIController.Instance != null && ownerInfo != null)
            DiceUIController.Instance.OnSingleRollRevealed(ownerInfo.myNumber, LastFaceValue);

        // Masterへ報告（オンラインのみ）
        if (isOnline && ownerInfo != null && DiceGameManager.Instance != null)
            DiceGameManager.Instance.ReportRollToMaster(ownerInfo.myNumber, LastFaceValue);
    }


    private void ApplyMaterialFromPlayerInfo()
    {
        if (diceRenderer == null || materials == null || materials.Length == 0) return;

        int idx = materialIndexDefault;
        if (ownerInfo != null) idx = ownerInfo.GetMaterialIndex();

        idx = Mathf.Clamp(idx, 0, materials.Length - 1);
        diceRenderer.sharedMaterial = materials[idx];
    }
}

using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class DiceController : MonoBehaviourPun
{
    private Rigidbody rb;
    private bool hasRolled = false; // このターン既に振ったか
    private bool isRolling = false; // 現在転がっている最中か

    [Header("Dice Settings")]
    public float rollForce = 10f;
    public float torqueAmount = 20f;

    // 面判定用のローカル座標定義 (標準的なダイスの場合)
    // 上、前、右、左、後、下 の順に対応する出目を定義
    // Vector3.up との内積で判定します
    private readonly Vector3[] faceVectors = {
        Vector3.forward,// 1 (Z+) ※モデルのUVに合わせて調整してください
        Vector3.up,     // 2 (Y+)
        Vector3.left,   // 3 (X+)
        Vector3.right,  // 4 (X-)
        Vector3.down,   // 5 (Y-)
        Vector3.back    // 6 (Z-)
    };

    // 上記ベクトルに対応する目（モデルに合わせて変更してください）
    private readonly int[] faceNumbers = { 1, 2, 3, 4, 5, 6 };

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 重力を切っておき、振るときに有効化するなど演出はお好みで
    }

    // InputSystemからのコールバック
    // PlayerInfomationなどで PlayerInput経由で呼ばれる、または自身でListenする
    public void OnRollInput(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine) return;
        if (context.performed && !hasRolled)
        {
            RollDice();
        }
    }

    // 外部（GameManagerの時間切れ等）から強制的に振らせる場合
    [PunRPC]
    public void ForceRoll()
    {
        if (!hasRolled)
        {
            RollDice();
        }
    }

    private void RollDice()
    {
        hasRolled = true;
        isRolling = true;

        // 物理挙動でサイコロを振る
        // ランダムな回転と上方向への力を加える
        Vector3 randomDir = Random.onUnitSphere;
        rb.AddForce((Vector3.up + randomDir * 0.5f) * rollForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);

        StartCoroutine(CheckDiceStop());
    }

    private IEnumerator CheckDiceStop()
    {
        // 少し待ってから判定開始（振った直後の静止を防ぐ）
        yield return new WaitForSeconds(0.5f);

        // 速度が十分落ちるまで待機
        while (rb.velocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.1f)
        {
            yield return null;
        }

        isRolling = false;
        DetermineResult();
    }

    private void DetermineResult()
    {
        // 最もワールド座標の「上(Vector3.up)」に近い面を探す
        int resultNumber = 1;
        float maxDot = -1f;

        for (int i = 0; i < faceVectors.Length; i++)
        {
            // ダイスの回転を考慮してローカルの面ベクトルをワールドに変換
            Vector3 worldFaceDir = transform.TransformDirection(faceVectors[i]);

            // WorldのUpとの内積をとる（1に近いほど上を向いている）
            float dot = Vector3.Dot(worldFaceDir, Vector3.up);

            if (dot > maxDot)
            {
                maxDot = dot;
                resultNumber = faceNumbers[i];
            }
        }

        Debug.Log($"My Dice Result: {resultNumber}");

        // 結果をMasterのGameManagerへ報告
        // 自身のActorNumberと出目を送る
        DiceGame_GameManager.instance.photonView.RPC(nameof(DiceGame_GameManager.instance.ReportRollResult), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, resultNumber);
    }

    // 次のターンのためにリセット（GameManagerから呼ばれる想定）
    [PunRPC]
    public void ResetDice()
    {
        hasRolled = false;
        // 必要なら位置をリセットしたりする
    }
}

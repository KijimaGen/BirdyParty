using UnityEngine;
using System.Collections;
using Photon.Pun;

/**
 * @file DiceObject.cs
 * @brief 物理演算と結果判定を行うダイス単体のコンポーネント。
 * DiceGameManagerと連携し、Roll、Reset、Result Reportを行う。
 */
[RequireComponent(typeof(Rigidbody))]
public class DiceObject : MonoBehaviour
{
    // 【★追加】ダイスの初期位置と回転を保持するためのフィールド
    [HideInInspector] public Vector3 InitialPosition;
    [HideInInspector] public Quaternion InitialRotation;

    [Header("ダイス設定")]
    [SerializeField] private float rollForce = 5f; // 適切な値に調整 (例: 5.0f ~ 15.0f)
    [SerializeField] private float rollTorque = 10f; // 適切な値に調整 (例: 10.0f ~ 30.0f)
    [SerializeField] private float stopThreshold = 0.1f; // 停止判定の速度しきい値
    [SerializeField] private float stabilizationTime = 0.5f; // 停止と判定してから結果を報告するまでの待機時間

    // 内部状態
    [HideInInspector] public int actorNumber = 0; // プレイヤーIDまたは特殊ID（BABAは-999など）
    private Rigidbody rb;
    public bool isRolling = false; // 回転中かどうか
    public int resultNumber = -1; // 確定した出目 (1-6)。-1は未確定/リセット状態。
    private bool isInitialized = false;
    private DiceGameManager manager;

    // ダイスの各面（フェース）の情報を格納する構造体（他のロジックが使用しているため省略せず残す）
    [System.Serializable]
    private struct DiceFace
    {
        public Vector3 upDirection; // ローカル座標系での面の法線ベクトル (例: (0, 1, 0))
        public int value;           // その面が示す値 (例: 6)
    }

    [SerializeField] private DiceFace[] faces;

    // 公開プロパティ
    public int GetResultNumber() { return resultNumber; }
    public void ResetDiceState()
    {
        resultNumber = -1;
        isRolling = false;

        // 【★追加】ダイスの物理的位置を初期状態に戻す
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = InitialPosition;
            transform.rotation = InitialRotation;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("DiceObject requires a Rigidbody component!");
            enabled = false;
        }
        manager = FindObjectOfType<DiceGameManager>();
    }

    // 初期化メソッド（DiceGameManagerから呼ばれる）
    public void Initialize(int number, Material mat, int matIndex)
    {
        actorNumber = number;
        // マテリアル設定（RendererがMeshRendererやSkinnedMeshRendererの場合を考慮）
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = mat;
        }

        isInitialized = true;
    }

    // 【ロールメソッド】
    public void Roll()
    {
        if (isRolling) return;
        isRolling = true;
        resultNumber = -1; // ロール開始時はリセット

        // 【★修正点1: 物理状態の完全リセット】
        // ダイスを投げる前に、既存の速度と角速度を完全にリセットする
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // 念のため、現在作用している力をクリア
        rb.ResetInertiaTensor();

        // 【★修正点2: 発生源からのランダムな力とトルクを適用】

        // 1. 投げる方向と強さの計算
        // - Y軸方向(上)への力は一定。
        // - XZ平面(水平)方向への力をランダムに加える。
        Vector3 forceDirection = new Vector3(
            Random.Range(-1f, 1f), // X方向のランダムな傾き
            1.5f,                  // Y方向の固定の上昇力 (必須)
            Random.Range(-1f, 1f)  // Z方向のランダムな傾き
        ).normalized;

        // 2. 適用する力の原点（ダイスの中央から少しずらすと安定しやすい）
        Vector3 forcePosition = transform.position + Random.insideUnitSphere * 0.1f;

        // 3. 力を適用
        // 適用する力の合計を rollForce の範囲に制限
        float actualRollForce = Random.Range(rollForce * 0.8f, rollForce * 1.2f);
        rb.AddForceAtPosition(forceDirection * actualRollForce, forcePosition, ForceMode.Impulse);

        // 4. トルク（回転）を適用
        // ランダムな方向へランダムな強さの回転を加える
        Vector3 randomTorque = Random.insideUnitSphere;
        float actualRollTorque = Random.Range(rollTorque * 0.8f, rollTorque * 1.2f);
        rb.AddTorque(randomTorque.normalized * actualRollTorque, ForceMode.Impulse);

        // 【デバッグログ】適用した力を確認
        Debug.Log($"Dice {gameObject.name} rolled with Force: {forceDirection * actualRollForce}, Torque: {randomTorque.normalized * actualRollTorque}");

        // ロールが完了するのを監視するコルーチンを開始
        StartCoroutine(CheckIfStopped());
    }

    // コルーチン: ダイスの停止を監視し、結果を確定する
    private IEnumerator CheckIfStopped()
    {
        // 物理的な安定を待つ
        yield return new WaitForSeconds(1.0f);

        while (isRolling)
        {
            // 速度と角速度がしきい値以下になるまで待機
            if (rb.velocity.sqrMagnitude < stopThreshold * stopThreshold &&
                rb.angularVelocity.sqrMagnitude < stopThreshold * stopThreshold)
            {
                // 停止と判定された後、さらにstabilizationTimeだけ待機して安定を保証
                yield return new WaitForSeconds(stabilizationTime);

                // 再度チェックし、まだ停止していることを確認
                if (rb.velocity.sqrMagnitude < stopThreshold * stopThreshold &&
                    rb.angularVelocity.sqrMagnitude < stopThreshold * stopThreshold)
                {
                    // 結果を確定
                    DetermineResult();
                    break;
                }
            }
            yield return null;
        }
    }

    // ダイスの出目を判定する
    private void DetermineResult()
    {
        int highestValue = -1;
        float maxDot = -Mathf.Infinity;

        // すべての面をチェック
        for (int i = 0; i < faces.Length; i++)
        {
            // ダイスのローカル座標系における面の方向を、ワールド座標系の真上(Vector3.up)と比較
            Vector3 worldDirection = transform.TransformDirection(faces[i].upDirection);

            // Dot積 (内積) は、二つのベクトルがどれだけ同じ方向を向いているかを示す
            float dot = Vector3.Dot(worldDirection, Vector3.up);

            if (dot > maxDot)
            {
                maxDot = dot;
                highestValue = faces[i].value;
            }
        }

        resultNumber = highestValue;
        isRolling = false;

        // 停止した状態を報告
        if (manager != null && resultNumber != -1)
        {
            Debug.Log($"Dice {gameObject.name} stopped. Result: {resultNumber}");
            manager.ReportDiceResult(actorNumber, resultNumber);
        }
        else if (manager == null)
        {
            Debug.LogError("DiceGameManagerが見つかりません。結果を報告できません。");
        }
        else
        {
            Debug.LogError($"Dice {gameObject.name} stopped, but result is invalid: {resultNumber}");
        }
    }
}
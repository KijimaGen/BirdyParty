using UnityEngine;
using Photon.Pun;
using Cysharp.Threading.Tasks;
using System;

/**
 * @file DiceController.cs
 * @brief 物理ダイスの挙動と出目判定を管理するクラス
 * Photon View, Rigidbody, Colliderが必要です。
 */
public class DiceController : MonoBehaviourPun
{
    public int OwnerPlayerNumber { get; private set; }
    public int ResultValue { get; private set; }

    // ダイスが静止したかどうかの判定用
    private bool isRolling = false;
    public bool IsRolling => isRolling; // DiceGamePlayer.csから参照できるようにする

    private Rigidbody rb;

    // 出目判定用のTransform配列 (Inspectorから設定)
    [SerializeField] private Transform[] faceCheckers = new Transform[6];

    // 転がすための設定 (Inspectorから調整してください。特にForceとTorque)
    [Header("Roll Settings")]
    [SerializeField] private float rollForce = 3500f; // 適用する力の強さ (強力に設定)
    [SerializeField] private float rollTorque = 1500f; // 適用する回転力の強さ (強力に設定)
    [SerializeField] private float stopSpeedThreshold = 0.1f; // 静止判定の速度しきい値
    [SerializeField] private float stopAngularThreshold = 0.1f; // 静止判定の回転速度しきい値
    [SerializeField] private float resetHeightOffset = 0.05f; // Roll前に少し持ち上げる高さ
    [SerializeField] private float resetRandomOffset = 0.1f; // Roll前に水平にずらす最大量

    void Awake()
    {
        // 剛体（Rigidbody）の参照を取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("DiceController: Rigidbody component not found!");
        }
    }

    /// <summary>
    /// ダイスを初期化
    /// </summary>
    /// <param name="playerNum">所有プレイヤー番号</param>
    [PunRPC]
    public void InitializeDice(int playerNum)
    {
        this.OwnerPlayerNumber = playerNum;

        // 生成直後に物理演算を強制的に有効化（落下開始）
        if (rb != null)
        {
            rb.isKinematic = false; // 物理演算を有効にする
            rb.useGravity = true;   // 重力を有効にする
        }

        // DiceGameManagerに自身を登録
        if (DiceGame_GameManager.instance != null)
        {
            DiceGame_GameManager.instance.RegisterDice(playerNum, this);
        }

        // マテリアルの設定
        if (DiceGame_GameManager.instance != null)
        {
            Material playerMat = DiceGame_GameManager.instance.GetPlayerMaterial(playerNum - 1);
            if (playerMat != null)
            {
                GetComponent<MeshRenderer>().material = playerMat;
            }
        }
    }

    /// <summary>
    /// ダイスを振る処理（全クライアントで実行）
    /// </summary>
    [PunRPC]
    public void RollDice()
    {
        if (rb == null) return;

        // 1. 物理演算の準備と位置のリセット
        rb.isKinematic = false;
        isRolling = true;
        ResultValue = 0; // 結果をリセット

        // 既存の速度と回転をリセット
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Roll前にダイスの位置を微調整（床に張り付くのを防ぎ、ランダム性を追加）
        Vector3 resetPosition = transform.position;
        resetPosition.y += resetHeightOffset; // 少し持ち上げる
        resetPosition.x += UnityEngine.Random.Range(-resetRandomOffset, resetRandomOffset); // 水平にランダムにずらす
        resetPosition.z += UnityEngine.Random.Range(-resetRandomOffset, resetRandomOffset);
        transform.position = resetPosition;


        // 2. ランダムな力（Force）を加える
        Vector3 randomForce = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(1.5f, 3.0f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized * rollForce;

        Vector3 randomPoint = transform.position + UnityEngine.Random.insideUnitSphere * 0.5f;

        rb.AddForceAtPosition(randomForce, randomPoint, ForceMode.Impulse);

        // 3. ランダムな回転力（Torque）を加える
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized * rollTorque;

        rb.AddTorque(randomTorque, ForceMode.Impulse);

        Debug.Log($"Dice {OwnerPlayerNumber} Rolled with Force: {randomForce.magnitude}");

        // 転がり始めたら結果確定処理を待つ
        CheckResultLoop().Forget();
    }

    /// <summary>
    /// ダイスが静止するまでループしてチェックする
    /// </summary>
    private async UniTask CheckResultLoop()
    {
        // 物理演算が安定するまで少し待つ (環境によって調整)
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        // 転がっている間チェックを続ける
        while (isRolling)
        {
            if (CheckIfStopped())
            {
                isRolling = false;
                rb.isKinematic = true; // 完全に静止したら物理的な動きを止める（重要）
                DetermineResult();
                break;
            }
            // 次のフレームまで待機
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// ダイスが静止したか確認する
    /// </summary>
    /// <returns>静止しているか否か</returns>
    private bool CheckIfStopped()
    {
        // 速度と回転速度がしきい値以下かチェック
        return rb.velocity.magnitude < stopSpeedThreshold && rb.angularVelocity.magnitude < stopAngularThreshold;
    }

    /// <summary>
    /// 出目を確定させる
    /// </summary>
    private void DetermineResult()
    {
        float maxDot = -1f;
        int result = 0;

        // 上方向 (Vector3.up) と最も内積(Dot product)が大きい面を探す
        for (int i = 0; i < faceCheckers.Length; i++)
        {
            if (faceCheckers[i] == null) continue;

            // 面の法線ベクトルを取得 (面チェッカーからダイスの中心方向へ向かうベクトル)
            Vector3 faceNormal = (transform.position - faceCheckers[i].position).normalized;

            // 上方向ベクトルと面法線の内積を計算
            float dotProduct = Vector3.Dot(faceNormal, Vector3.up);

            if (dotProduct > maxDot)
            {
                maxDot = dotProduct;
                // faceCheckersのインデックスは 0-5 なので、出目は 1-6
                result = i + 1;
            }
        }

        ResultValue = result;
        Debug.Log($"Dice {OwnerPlayerNumber} stopped. Result: {ResultValue}");

        // ★ ここでDiceGameManagerに結果を通知する処理を追加する ★
        // DiceGameManager.instance.ReportDiceResult(OwnerPlayerNumber, ResultValue);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Photon.Pun;

public class DiceRoll : MonoBehaviourPun
{
    private Rigidbody rb;
    public bool isRolling = false;
    private string currentBottomFace = "";
    private string resultFace = "";

    // GameManagerに結果を通知するためのコールバック
    private Action<string> onRollComplete;
    private int lastDiceValue = 0; 

    [Header("表示させるサイコロ画像")]
    // (UI設定は元のコードを維持)
    [SerializeField] private GameObject dice1;
    [SerializeField] private GameObject dice2;
    [SerializeField] private GameObject dice3;
    [SerializeField] private GameObject dice4;
    [SerializeField] private GameObject dice5;
    [SerializeField] private GameObject dice6;

    // 出現位置のGameObject（Inspectorで設定が必要）
    [SerializeField] private GameObject UseDice;

    // Rigidbodyの取得はAwakeで行い、初期化を保証
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("DiceRoll: Rigidbody コンポーネントが見つかりません。", this);
        }
    }

    // GameManagerからこの関数が呼ばれる
    public void StartRoll(Action<string> callback)
    {
        if (isRolling || rb == null) return;

        onRollComplete = callback;
        isRolling = true;
        currentBottomFace = "";
        resultFace = "";
        lastDiceValue = 0;

        transform.rotation = UnityEngine.Random.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ランダムな力とトルクを加えて振る
        Vector3 force = new Vector3(UnityEngine.Random.Range(-2f, 2f), 6f, UnityEngine.Random.Range(-2f, 2f));
        Vector3 torque = new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f));
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);

        Invoke(nameof(CheckIfStopped), 2f);
    }

    void CheckIfStopped()
    {
        if (rb == null) return;

        // ほぼ停止したら
        if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        {
            isRolling = false;

            // --- 修正箇所: 出目決定ロジックの修正と整理 ---
            int topFaceValue = 0;

            // currentBottomFaceは "Face_1", "Face_2" のような文字列を期待
            if (!string.IsNullOrEmpty(currentBottomFace))
            {
                // "Face_1" から "1" の部分を抽出し、intに変換
                string bottomFaceNumberStr = currentBottomFace.Replace("Face_", "");

                if (int.TryParse(bottomFaceNumberStr, out int bottom))
                {
                    // 上面の出目を計算 (1-6)
                    topFaceValue = 7 - bottom;
                    resultFace = topFaceValue.ToString(); // 結果文字列を設定
                    lastDiceValue = topFaceValue; // 内部値を保存
                }
            }

            // もし topFaceValue が 0 のままなら、結果が不正であることを示す
            if (topFaceValue == 0)
            {
                // 結果が取得できなかった場合は、エラーとして "0" を返すようにする
                // これにより、GameManager側で "" ではなく "0" が渡され、より安全になる。
                resultFace = "0";
                Debug.LogError($"[DiceRoll] サイコロの出目決定に失敗しました。currentBottomFace: '{currentBottomFace}'", this);
            }
            // --- 修正箇所終了 ---

            DiceCheck(); // ダイスの見た目（画像/モデル）を更新

            // onRollCompleteに結果文字列 (例: "3") を渡す
            onRollComplete?.Invoke(resultFace);

            if (photonView.IsMine && PhotonNetwork.InRoom)
            {
                // 自分のダイスが止まったら、結果をRPCで全クライアントに通知 (これはDiceGameManagerに送信するRPC)
                DiceGameManager manager = FindObjectOfType<DiceGameManager>();
                if (manager != null && manager.photonView != null)
                {
                    // プレイヤーの識別子（NickName）と結果を送信
                    manager.photonView.RPC(
                        "SyncPlayerDiceResult",
                        RpcTarget.All,
                        PhotonNetwork.LocalPlayer.NickName,
                        lastDiceValue
                    );
                    Debug.Log($"[DiceRoll] {PhotonNetwork.LocalPlayer.NickName} のダイス結果 {lastDiceValue} を全クライアントに送信。");
                }
            }

            onRollComplete = null;
        }
        else
        {
            // 停止していなければ0.5秒後に再チェック
            Invoke(nameof(CheckIfStopped), 0.5f);
        }
    }

    public void SetBottomFace(string faceName)
    {
        currentBottomFace = faceName;
    }

    private void DiceCheck()
    {
        GameObject[] dices = { dice1, dice2, dice3, dice4, dice5, dice6 };
        foreach (var d in dices) d.SetActive(false);

        if (int.TryParse(resultFace, out int result) && result >= 1 && result <= 6)
        {
            dices[result - 1].SetActive(true);
        }
    }

    public int GetCurrentResult()
    {
        return lastDiceValue;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Photon.Pun;
using Photon.Realtime;

public class DiceRoll : MonoBehaviourPun
{
    private Rigidbody rb;
    public bool isRolling = false;
    private string currentBottomFace = "";
    private string resultFace = "";

    // GameManagerに結果を通知するためのコールバック
    private Action<string> onRollComplete;
    private int lastDiceValue = 0;

    private DiceVisualController diceVisuals;

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

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // ネットワークオブジェクト生成直後に一度だけ呼ばれる
        if (photonView.IsMine)
        {
            // ★★★ 修正1: 1フレーム遅延による無限ループ回避を StartCoroutine で実行 ★★★
            StartCoroutine(InitializeVisualsAfterDelay());
        }
    }

    private IEnumerator InitializeVisualsAfterDelay()
    {
        // 1フレーム待機し、Unity/Photonの初期化サイクルを完了させる
        yield return null;

        if (!photonView.IsMine) yield break; // 再度 IsMine チェック

        BABADiceGameManager manager = FindObjectOfType<BABADiceGameManager>();

        if (manager == null)
        {
            Debug.LogError("[DiceRoll] DiceGameManagerが見つかりません。ビジュアル生成をスキップします。");
            yield break;
        }

        // プレイヤーIDの決定 (ActorNumberは1から始まるため、-1で0始まりのインデックスを取得)
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        GameObject correctVisualPrefab = null;

        // 割り当てられたプレハブ配列の範囲チェックとプレハブ取得
        if (playerIndex >= 0 && playerIndex < manager.assignedDicePrefabs.Length)
        {
            correctVisualPrefab = manager.assignedDicePrefabs[playerIndex];
        }

        if (correctVisualPrefab != null)
        {
            // ローカルで見た目のみを生成 (ネットワーク生成ではない)
            GameObject visualClone = Instantiate(correctVisualPrefab, transform.position, transform.rotation);

            // 親を設定し、ローカル位置をリセット
            visualClone.transform.SetParent(this.transform, worldPositionStays: true);
            visualClone.transform.localPosition = Vector3.zero;
            visualClone.transform.localRotation = Quaternion.identity;

            diceVisuals = visualClone.GetComponent<DiceVisualController>();

            if (diceVisuals == null)
            {
                Debug.LogError($"[DiceRoll] ビジュアルプレハブ'{correctVisualPrefab.name}'に DiceVisualController が見つかりません。");
            }

            // ★ 初期状態の見た目（非表示）
            if (diceVisuals != null)
            {
                diceVisuals.DisplayDiceResult(0);
            }

            Debug.Log($"[DiceRoll Debug] {PhotonNetwork.LocalPlayer.NickName} のダイスビジュアルをローカルに生成完了。");
        }
        else
        {
            Debug.LogError($"[DiceRoll] P{playerIndex + 1} の Visual Prefabが見つからないか、Managerの設定が不正です。", this);
        }
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

            // onRollCompleteに結果文字列 (例: "3") を渡す
            onRollComplete?.Invoke(resultFace);

            Debug.Log($"[DiceRoll Debug] Owner: {photonView.Owner.NickName}, IsMine: {photonView.IsMine}, InRoom: {PhotonNetwork.InRoom}");

            if (PhotonNetwork.InRoom && photonView.Owner == PhotonNetwork.LocalPlayer)
            {
                // 自分のダイスが止まったら、結果をRPCで全クライアントに通知 (これはDiceGameManagerに送信するRPC)
                BABADiceGameManager manager = FindObjectOfType<BABADiceGameManager>();

                if (manager == null)
                {
                    Debug.LogError("[DiceRoll Debug] DiceGameManager がシーンから見つかりません。");
                    return; // managerがないため処理を中断
                }
                if (manager.photonView == null)
                {
                    Debug.LogError("[DiceRoll Debug] DiceGameManager に PhotonView がアタッチされていません。");
                    return; // photonViewがないため処理を中断
                }

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

    void DiceCheck()
    {
        // (物理的な出目決定ロジックは維持)
        if (int.TryParse(currentBottomFace.Replace("Face_", ""), out int bottom))
        {
            lastDiceValue = 7 - bottom;
        }
        else
        {
            resultFace = "0";
            lastDiceValue = 0;
            Debug.LogError($"[DiceRoll] サイコロの出目決定に失敗しました。currentBottomFace: '{currentBottomFace}'", this);
        }

        // ★★★ 修正2: DiceVisualController が null でないかチェックしてから呼び出す ★★★
        if (diceVisuals != null)
        {
            diceVisuals.DisplayDiceResult(lastDiceValue);
        }
        else
        {
            Debug.LogWarning($"[DiceRoll] DiceVisualController が null です。見た目の表示をスキップしました。");
        }

        // (RPC送信ロジックは維持)
        onRollComplete?.Invoke(resultFace);
        // ... (RPC送信ロジック) ...
    }

    public void SetVisualController(DiceVisualController visuals)
    {
        diceVisuals = visuals;
    }

    public void SetBottomFace(string faceName)
    {
        currentBottomFace = faceName;
    }

    public int GetCurrentResult()
    {
        return lastDiceValue;
    }
}
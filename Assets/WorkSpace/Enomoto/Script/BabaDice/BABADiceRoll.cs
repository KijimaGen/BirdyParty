using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BABADiceRoll : MonoBehaviour
{
    private Rigidbody rb;
    public bool isRolling = false;
    private string currentBottomFace = "";
    private string babaFace = "";

    // GameManagerに結果を通知するためのコールバック
    private Action<string> onRollComplete;

    [Header("表示させるサイコロ画像")]
    // (UI設定は元のコードを維持)
    [SerializeField] private GameObject dice1;
    [SerializeField] private GameObject dice2;
    [SerializeField] private GameObject dice3;
    [SerializeField] private GameObject dice4;
    [SerializeField] private GameObject dice5;
    [SerializeField] private GameObject dice6;

    // 出現位置のGameObject（Inspectorで設定が必要）
    [SerializeField] private GameObject UseBABADice;

    // Rigidbodyの取得はAwakeで行い、初期化を保証
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("BABADiceRoll: Rigidbody コンポーネントが見つかりません。", this);
        }
    }

    // GameManagerからこの関数が呼ばれる
    public void StartRoll(Action<string> callback)
    {
        if (isRolling || rb == null) return;

        onRollComplete = callback;
        isRolling = true;
        currentBottomFace = "";
        babaFace = "";

        // UseBABADiceがnullでないことを確認
        if (UseBABADice != null)
        {
            transform.position = UseBABADice.transform.position;
        }
        else
        {
            // Nullの場合、安全のために原点より少し高い位置に配置
            transform.position = new Vector3(0, 5, 0);
        }

        transform.rotation = UnityEngine.Random.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ランダムな力とトルクを加えて振る
        Vector3 force = new Vector3(UnityEngine.Random.Range(-2f, 2f), 6f, UnityEngine.Random.Range(-2f, 2f));
        Vector3 torque = new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f));
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);

        // 2秒後に停止判定を開始
        Invoke(nameof(CheckIfStopped), 2f);
    }

    void CheckIfStopped()
    {
        if (rb == null) return;

        // ほぼ停止したら
        if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        {
            isRolling = false;
            if (int.TryParse(currentBottomFace.Replace("Face_", ""), out int bottom))
            {
                int top = 7 - bottom;
                babaFace = top.ToString();
            }

            DiceCheck(); // 画像の更新
            onRollComplete?.Invoke(babaFace); // GameManagerに結果を通知
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
        // ... (元のコードの画像切り替え処理を維持) ...
        GameObject[] dices = { dice1, dice2, dice3, dice4, dice5, dice6 };
        foreach (var d in dices) d.SetActive(false);

        if (int.TryParse(babaFace, out int result) && result >= 1 && result <= 6)
        {
            dices[result - 1].SetActive(true);
        }
    }
}

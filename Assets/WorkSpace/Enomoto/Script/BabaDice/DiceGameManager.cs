using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GameState
{
    Start,          // ゲームスタート
    SetBABA,        // BABAダイスをセット
    PlayerTurn,     // プレイヤーのターン
    DiceRoll,       // プレイヤーがダイスを振る
    CheckResult,    // ダイスチェック
    Won,            // BABAとかぶらなかったので続行
    Lost,           // BABAとかぶったので脱落
}

public class DiceGameManager : MonoBehaviour
{
    public static GameState currentState;

    public int currentTurn = 1;
    public int maxTurns    = 5;

    [Header("ダイスの参照")]
    public BABADiceRoll babaDiceRoll;
    public DiceRoll playerDiceRoll;

    [Header("UI参照")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI babaDiceText;
    public TextMeshProUGUI playerDiceText;
    public TextMeshProUGUI resultText;


    // ゲームで使う内部変数
    private int currentBabaDiceValue;
    private int currentPlayerDiceValue;

    private void Start()
    {
        UpdateGameState(GameState.Start);
    }

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("State変更" + newState);

        switch (newState)
        {
            case GameState.Start:
                currentTurn = 1;
                resultText.text = "ゲーム開始！";

                // BABAダイスを振る
                UpdateGameState(GameState.SetBABA);
                break;

            case GameState.SetBABA:
                // UIを初期化する
                turnText.text = $"ターン {currentTurn} / {maxTurns}";
                babaDiceText.text = "BABAダイス: ?";
                playerDiceText.text = "あなたの出目: ?";
                resultText.text = "BABAダイスを決めています...";

                // BABAダイスのダイスロール
                babaDiceRoll.StartRoll(OnBabaRollComplete);
                break;

            case GameState.PlayerTurn:
                // BABAダイスの結果を表示して、プレイヤーのターンへ（Spaceキーの入力待ち）
                babaDiceText.text = $"BABAダイス: {currentBabaDiceValue}";
                resultText.text = "あなたの番です。\n(Spaceキーでダイスを振る)";
                break;

            case GameState.DiceRoll:
                resultText.text = "ダイスを振っています...";
                // プレイヤーダイスロールを開始。完了したら OnPlayerRollComplete を呼ぶ
                playerDiceRoll.StartRoll(OnPlayerRollComplete);
                break;

            case GameState.CheckResult:
                // プレイヤーの出目を表示
                playerDiceText.text = $"あなたの出目: {currentPlayerDiceValue}";

                // --- ここで脱落判定 ---
                if (currentPlayerDiceValue == currentBabaDiceValue)
                {
                    // 脱落！
                    resultText.text = $"脱落！ (BABAダイス: {currentBabaDiceValue})";
                    UpdateGameState(GameState.Lost);
                }
                else
                {
                    // セーフ！
                    resultText.text = "セーフ！ 次のターンへ...";

                    if (currentTurn == maxTurns)
                    {
                        // 5ターンクリア
                        UpdateGameState(GameState.Won);
                    }
                    else
                    {
                        // 次のターンへ
                        currentTurn++;
                        StartCoroutine(WaitAndNextTurn());
                    }
                }
                break;

            case GameState.Won:
                resultText.text = "勝利！ 5ターン生き残った！";
                break;

            case GameState.Lost:
                resultText.text = "敗北...";
                // プレイヤーのダイスを非表示に
                playerDiceRoll.gameObject.SetActive(false);
                break;
        }
    }

    private void Update()
    {
        // プレイヤーのターンの場合のみ入力を受け付ける
        if (currentState == GameState.PlayerTurn && Input.GetKeyDown(KeyCode.Space))
        {
            UpdateGameState(GameState.DiceRoll);
        }
    }

    void OnBabaRollComplete(string babaFace)
    {
        int.TryParse(babaFace, out currentBabaDiceValue);
        UpdateGameState(GameState.PlayerTurn);
    }

    // プレイヤーダイスロールが完了したときに呼ばれる
    void OnPlayerRollComplete(string playerFace)
    {
        int.TryParse(playerFace, out currentPlayerDiceValue);
        // プレイヤーの値が確定したので、結果判定へ
        UpdateGameState(GameState.CheckResult);
    }

    // 次のターンに移るまでの「間」
    IEnumerator WaitAndNextTurn()
    {
        yield return new WaitForSeconds(2.0f);  // 2秒待つ
        UpdateGameState(GameState.SetBABA);     // 次のターンの準備へ
    }
}
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

// ゲームの状態を定義
public enum GameState
{
    Start,
    SetBabaDice,      // BABAダイスを振っている
    PlayerRolling,    // プレイヤーの入力待ち（Spaceキーで同時ロール）
    PlayersInRoll,    // 全プレイヤーのダイスが転がっている
    CheckResults,     // 全員のダイスが止まった後の結果判定
    GameOverCheck,    // ゲームオーバー後の最終順位付け
    GameFinished      // 結果表示
}

public class DiceGameManager : MonoBehaviour
{

    public static GameState currentState;
    public int currentTurn = 1;
    public int maxTurns = 5;

    [Header("ゲーム設定")]
    public int maxPlayers = 4; // 最大プレイヤー数

    [Header("BABAダイスの参照")]
    public BABADiceRoll babaDiceRoll;

    [Header("UI参照")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI babaDiceText;
    public TextMeshProUGUI resultText;
    public Button rollButton; // プレイヤーに振らせるためのボタン（Spaceキーと兼用可）

    // ゲームで使う内部変数
    private int currentBabaDiceValue;
    private List<PlayerInfo> players;
    private List<PlayerInfo> playerWaitingForRoll;
    private Dictionary<PlayerInfo, DiceRoll> playerDices = new Dictionary<PlayerInfo, DiceRoll>();

    [Header("プレイヤー用ダイス")]
    public GameObject[] assignedDicePrefabs = new GameObject[4];

    [Header("プレイヤー用ダイスの出現位置設定")]
    public Transform[] playerSpawnPoints = new Transform[4];

    [Header("プレイヤースコアの設定")]
    public TextMeshProUGUI[] playerScoreTexts = new TextMeshProUGUI[4];

    void Awake()
    {
        // ゲーム開始時にプレイヤーとダイスを準備
        SetupPlayers();
    }

    void Start()
    {
        UpdateGameState(GameState.Start);
    }

    // プレイヤーリストとダイスオブジェクトを生成
    private void SetupPlayers()
    {
        players = new List<PlayerInfo>();

        if (playerDices.Count > 0)
        {
            foreach (var dice in playerDices.Values)
            {
                if (dice != null && dice.gameObject != null)
                {
                    Destroy(dice.gameObject);
                }
            }
        }
        playerDices.Clear();

        if (assignedDicePrefabs == null || assignedDicePrefabs.Length == 0)
        {
            Debug.LogError("プレイヤーダイスのPrefabがInspectorで設定されていません。");
            return;
        }

        int actualPlayers = 0;

        // プレイヤーとダイスを紐づけて登録
        for (int i = 0; i < assignedDicePrefabs.Length; i++)
        {
            GameObject dicePrefab = assignedDicePrefabs[i];

            if (dicePrefab != null)
            {
                Vector3 spawnPosition = Vector3.zero;
                Quaternion spawnRotation = Quaternion.identity;

                if (i < playerSpawnPoints.Length && playerSpawnPoints[i] != null)
                {
                    spawnPosition = playerSpawnPoints[i].position;
                    spawnRotation = playerSpawnPoints[i].rotation;
                }
                else
                {
                    // 出現位置に関しての設定がない場合は以下の位置に出現
                    spawnPosition = new Vector3(i * 0.1f, 10f, 5f);
                }

                // GameManagerの子要素として生成 (Scene整理のため)
                GameObject diceObj = Instantiate(dicePrefab, spawnPosition, spawnRotation, this.transform);

                DiceRoll currentDiceScript = diceObj.GetComponent<DiceRoll>();

                

                if (currentDiceScript != null)
                {
                    actualPlayers++;

                    PlayerInfo newPlayer = new PlayerInfo(actualPlayers, $"Player {actualPlayers}");
                    players.Add(newPlayer);

                    playerDices.Add(newPlayer, currentDiceScript);

                    PlayerInputHandler handler = diceObj.AddComponent<PlayerInputHandler>();
                    handler.PlayerData = newPlayer;
                    handler.GameManager = this;
                }
                else
                {
                    Debug.LogError($"Prefab: {dicePrefab.name} に DiceRoll コンポーネントが見つかりません。");
                    Destroy(diceObj); // 生成したオブジェクトを破棄
                }
            }
        }

        if (players.Count == 0)
        {
            Debug.LogError("有効なプレイヤーダイスが一つも設定されていません。");
        }

        UpdateScoreUIs();
    }

    // ゲーム状態を切り替えるメインの関数
    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("新しい状態: " + newState);

        // ボタン制御 (PlayerRolling状態以外は無効化)
        if (rollButton != null)
        {
            rollButton.interactable = (newState == GameState.PlayerRolling);
        }

        switch (newState)
        {
            case GameState.Start:
            currentTurn = 1;
            resultText.text = "ゲーム開始！";
            // 最初のBABAダイスを振る
            UpdateGameState(GameState.SetBabaDice);
            break;

            case GameState.SetBabaDice:
            // UI初期化
            turnText.text = $"ターン {currentTurn} / {maxTurns}";
            babaDiceText.text = "BABAダイス: ?";
            resultText.text = "BABAダイスを決めています...";

            // BABAダイスロールを開始。完了したら OnBabaRollComplete を呼ぶ
            babaDiceRoll.StartRoll(OnBabaRollComplete);
            break;

            case GameState.PlayerRolling:
            // BABAダイスの結果を表示
            babaDiceText.text = $"BABAダイス: {currentBabaDiceValue}";
            resultText.text = "Spaceキーかボタンで全員一斉ロール！";
            break;

            case GameState.PlayersInRoll:
            resultText.text = "全員一斉ロール中...";
            // ここでStartAllPlayerRolls()が呼ばれる
            break;

            case GameState.CheckResults:
            // 全員のダイスが止まった。ここから判定ルーチン開始
            StartCoroutine(CheckAllResultsRoutine());
            break;

            case GameState.GameOverCheck:
            // 最終判定
            DisplayFinalRanking();
            UpdateGameState(GameState.GameFinished);
            break;

            case GameState.GameFinished:
            // 結果表示完了。リスタートボタンなどを有効化
            break;
        }
    }

    // スコアUIをすべて更新する関数
    private void UpdateScoreUIs()
    {
        // プレイヤーの数と設定されたUIの数のうち、少ない方までループ
        int count = Mathf.Min(players.Count, playerScoreTexts.Length);

        for (int i = 0; i < count; i++)
        {
            // UI TextがInspectorで割り当てられているか確認
            if (playerScoreTexts[i] == null) continue;

            PlayerInfo p = players[i];

            // 生存者はスコア、脱落者は「脱落」を表示
            if (p.IsEliminated)
            {
                playerScoreTexts[i].text = $"脱落";
                playerScoreTexts[i].color = Color.gray;
            }
            else
            {
                playerScoreTexts[i].text = $"{p.TotalScore}";
                playerScoreTexts[i].color = Color.black;
            }
        }
    }

    // GameManagerのUpdateは「入力受付」だけを行う
    void Update()
    {
        // "PlayerRolling" 状態の時だけ Space キーを受け付ける
        if (currentState == GameState.PlayerRolling && Input.GetKeyDown(KeyCode.Space))
        {
            StartAllPlayerRolls();
        }
    }

    // UIボタンから呼び出すための関数
    public void OnRollButtonClicked()
    {
        if (currentState == GameState.PlayerRolling)
        {
            StartAllPlayerRolls();
        }
    }

    // BABAダイスロールが完了したときに呼ばれる
    void OnBabaRollComplete(string babaFace)
    {
        int.TryParse(babaFace, out currentBabaDiceValue);
        // BABAダイスの値が確定したので、プレイヤーの入力待ちへ
        UpdateGameState(GameState.PlayerRolling);
    }

    // 全員分のダイスを同時に振る関数
    private void StartAllPlayerRolls()
    {
        UpdateGameState(GameState.PlayersInRoll);

        // 脱落していないプレイヤーのダイスだけを振る
        var activePlayers = players.Where(p => !p.IsEliminated).ToList();

        if (activePlayers.Count == 0)
        {
            UpdateGameState(GameState.GameOverCheck);
            return;
        }

        // コールバックが呼ばれた回数を追跡するカウンター
        int rollsCompleted = 0;

        // 各プレイヤーのダイスを StartRoll
        foreach (PlayerInfo player in activePlayers)
        {
            DiceRoll dice = playerDices[player];
            dice.gameObject.SetActive(true); // ダイスを表示

            // プレイヤーごとのコールバック
            dice.StartRoll((resultFace) =>
            {
                int diceValue;
                if (int.TryParse(resultFace, out diceValue))
                {
                    // プレイヤー情報に出目を一時保存
                    player.CurrentDiceResult = diceValue;
                }

                rollsCompleted++;

                // 全員分のダイスが止まったら、次の状態へ移行
                if (rollsCompleted >= activePlayers.Count)
                {
                    UpdateGameState(GameState.CheckResults);
                }
            });
        }
    }

    // 全員のダイスが止まった後の判定ルーチン
    IEnumerator CheckAllResultsRoutine()
    {
        yield return new WaitForSeconds(1.0f); // 判定開始前の待ち時間

        // プレイヤーの順番（ID順）に判定
        foreach (PlayerInfo player in players.OrderBy(p => p.PlayerID))
        {
            if (player.IsEliminated) continue;

            DiceRoll dice = playerDices[player];

            // 1. スコア加算
            player.TotalScore += player.CurrentDiceResult;

            // ここでスコアUIを更新
            UpdateScoreUIs();

            // 2. 脱落判定
            if (player.CurrentDiceResult == currentBabaDiceValue)
            {
                // 脱落処理
                player.IsEliminated = true;
                player.EliminationTurn = currentTurn;
                dice.gameObject.SetActive(false);

                resultText.text = $"{player.PlayerName} が脱落！ (出目: {player.CurrentDiceResult} = BABA: {currentBabaDiceValue})";

                // 脱落したらUIを再更新して「脱落」表示に切り替え
                UpdateScoreUIs();

                yield return new WaitForSeconds(3.0f);
            }
            else
            {
                // セーフ！
                resultText.text = $"{player.PlayerName} はセーフ！";
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 3. ターン終了チェック
        int aliveCount = players.Count(p => !p.IsEliminated);

        if (aliveCount <= 1 || currentTurn >= maxTurns)
        {
            // 1人以下になった、または5ターン目に到達した
            UpdateGameState(GameState.GameOverCheck);
        }
        else
        {
            // 次のターンへ
            currentTurn++;
            UpdateGameState(GameState.SetBabaDice);
        }
    }

    // 最終順位付けと結果表示
    void DisplayFinalRanking()
    {
        // 順位付けロジック
        // 1. 生存者優先 (IsEliminated = false)
        // 2. 脱落者は、脱落ターンが遅い方が上位 (EliminationTurnが大きい方が上位)
        // 3. スコアが高い方が上位
        var finalRanking = players
            .OrderBy(p => p.IsEliminated)
            .ThenByDescending(p => p.EliminationTurn)
            .ThenByDescending(p => p.TotalScore)
            .ToList();

        string rankString = "--- 最終結果 ---\n";
        for (int i = 0; i < finalRanking.Count; i++)
        {
            PlayerInfo p = finalRanking[i];
            string status = p.IsEliminated
                            ? $"(脱落: {p.EliminationTurn}T)"
                            : "(生存)";

            rankString += $"{i + 1}位: {p.PlayerName} | スコア: {p.TotalScore} {status}\n";
        }
        resultText.text = rankString;
    }

    // PlayerInputHandlerより呼びだされる
    public void HandlePlayerRollInput(PlayerInfo player)
    {
        //if (currentState == GameState.PlayerRolling && playerWaitingForRoll.)
    }
}
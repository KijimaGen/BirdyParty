using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; 

// ゲームの状態を定義
public enum GameState
{
    Start,
    WaitingForPlayers,  // プレイヤー参加待ち
    SetBabaDice,        // BABAダイスを振っている
    PlayerRolling,      // プレイヤーの入力待ち (Input System)
    CheckResults,       // 全員のダイスが止まった後の結果判定
    GameOverCheck,      // ゲームオーバー後の最終順位付け
    GameFinished        // 結果表示
}

public class DiceGameManager : MonoBehaviour
{

    public static GameState currentState;
    public int currentTurn = 1;
    public int maxTurns = 5;

    [Header("ゲーム設定")]
    public int maxPlayers = 4;

    [Header("プレイヤーダイス (Prefab手動設定)")]
    // Player Input ManagerがPrefabを使用するため、Inspectorでの設定は必須ではありませんが、
    // 互換性のため残します。
    public GameObject[] assignedDicePrefabs;

    [Header("ダイス出現位置 (手動設定)")]
    public Transform[] playerSpawnPoints = new Transform[4];

    [Header("Input System 設定")]
    // Inspectorで.inputactionsファイルを割り当てる
    public InputActionAsset inputActionAsset;

    [Header("BABAダイスへの参照")]
    public BABADiceRoll babaDiceRoll;

    [Header("UI参照")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI babaDiceText;
    public TextMeshProUGUI resultText;
    public Button rollButton;

    [Header("スコア表示UI")]
    // TextMeshProUGUIを使用
    public TextMeshProUGUI[] playerScoreTexts = new TextMeshProUGUI[4];

    private int currentBabaDiceValue;
    private List<PlayerInfo> players;
    private Dictionary<PlayerInfo, DiceRoll> playerDices;
    private List<PlayerInfo> playersWaitingForRoll;


    void Awake()
    {
        SetupPlayers();
    }

    void Start()
    {
        UpdateGameState(GameState.Start);
    }

    // PlayerInputHandlerから呼ばれる、プレイヤーの登録処理
    public void RegisterNewPlayerDice(PlayerInput input, DiceRoll diceScript, PlayerInputHandler handler)
    {
        // 最大プレイヤー数を超えていたら登録しない
        if (players.Count >= maxPlayers)
        {
            Debug.LogWarning("最大プレイヤー数に達しました。");
            return;
        }

        // プレイヤー数を元に新しいIDを割り当てる
        int newPlayerId = players.Count + 1;
        string newPlayerName = $"Player {newPlayerId}";

        // 1. PlayerInfoを作成し、リストに追加
        PlayerInfo newPlayer = new PlayerInfo(newPlayerId, newPlayerName);
        players.Add(newPlayer);
        playerDices.Add(newPlayer, diceScript);

        // 2. PlayerInputHandlerにPlayerInfoとManagerの参照を設定
        handler.PlayerData = newPlayer;
        handler.GameManager = this;

        // 3. PlayerInputの初期設定
        if (inputActionAsset != null)
        {
            input.actions = inputActionAsset;
            input.defaultActionMap = "DiceGame";
        }

        // 4. ダイスの出現位置を設定
        if (newPlayerId - 1 < playerSpawnPoints.Length && playerSpawnPoints[newPlayerId - 1] != null)
        {
            diceScript.transform.position = playerSpawnPoints[newPlayerId - 1].position;
            diceScript.transform.rotation = playerSpawnPoints[newPlayerId - 1].rotation;
        }

        // 5. 生成直後はダイスを非表示にしておく 
        diceScript.gameObject.SetActive(false);

        UpdateScoreUIs();

        Debug.Log($"[Input System] 新しいプレイヤーが参加しました: {newPlayerName}");

        // 6. 最初のプレイヤーが参加したらゲームの最初のターンに進む
        if (currentState == GameState.WaitingForPlayers && players.Count >= 1)
        {
            UpdateGameState(GameState.SetBabaDice);
        }
    }

    private void SetupPlayers()
    {

        players = new List<PlayerInfo>();
        playerDices = new Dictionary<PlayerInfo, DiceRoll>();
        playersWaitingForRoll = new List<PlayerInfo>();

        if (playerDices.Count > 0)
        {
            foreach (var dice in playerDices.Values)
            {
                if (dice != null && dice.gameObject != null) Destroy(dice.gameObject);
            }
        }
        playerDices.Clear();

        UpdateScoreUIs();
    }

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("新しい状態: " + newState);

        if (rollButton != null)
        {
            rollButton.interactable = false;
        }

        switch (newState)
        {
            case GameState.Start:
            currentTurn = 1;
            UpdateGameState(GameState.WaitingForPlayers);
            break;

            case GameState.WaitingForPlayers: 
            if (resultText != null)
            {
                resultText.text = "ゲーム開始！ Spaceキーかコントローラーを押して参加してください。";
            }
            break;

            case GameState.SetBabaDice:
            // プレイヤーが誰も参加していない場合は待機状態に戻す（起こらないはずだが安全策）
            if (players.Count == 0)
            {
                UpdateGameState(GameState.WaitingForPlayers);
                return;
            }

            turnText.text = $"ターン {currentTurn} / {maxTurns}";
            babaDiceText.text = "BABAダイス: ?";
            resultText.text = "BABAダイスを決めています...";
            babaDiceRoll.StartRoll(OnBabaRollComplete);
            break;

            case GameState.PlayerRolling:
            babaDiceText.text = $"BABAダイス: {currentBabaDiceValue}";
            resultText.text = "各プレイヤーは対応する操作ボタンを押してサイコロを振ってください。";
            break;

            case GameState.CheckResults:
            resultText.text = "結果を判定中です...";
            StartCoroutine(CheckAllResultsRoutine());
            break;

            case GameState.GameOverCheck:
            DisplayFinalRanking();
            UpdateGameState(GameState.GameFinished);
            break;

            case GameState.GameFinished:
            break;
        }
    }

    // PlayerInputHandlerから「プレイヤーがボタンを押した」という通知を受け取る
    public void HandlePlayerRollInput(PlayerInfo player)
    {
        Debug.Log($"ロール試行: {player.PlayerName} | 現在の状態: {currentState} | 待機リストに含まれるか: {playersWaitingForRoll.Contains(player)}");

        if (currentState == GameState.PlayerRolling && playersWaitingForRoll.Contains(player))
        {
            Debug.Log($"【ロール開始】: {player.PlayerName}");

            StartSinglePlayerRoll(player);
        }
        // WaitingForPlayers中に押された場合は、RegisterNewPlayerDiceで処理されているためここでは無視
    }

    void OnBabaRollComplete(string babaFace)
    {
        int.TryParse(babaFace, out currentBabaDiceValue);

        // 脱落していないプレイヤー全員を今回のロール待ちリストに入れる
        playersWaitingForRoll = players.Where(p => !p.IsEliminated).ToList();

        Debug.Log($"OnBabaRollComplete: 待機リストに {playersWaitingForRoll.Count} 人のプレイヤーを追加しました。");

        if (playersWaitingForRoll.Count <= 0)
        {
            UpdateGameState(GameState.GameOverCheck);
            return;
        }

        UpdateGameState(GameState.PlayerRolling);
    }

    private void StartSinglePlayerRoll(PlayerInfo player)
    {
        if (player.IsEliminated) return;

        DiceRoll dice = playerDices[player];
        dice.gameObject.SetActive(true);

        resultText.text = $"{player.PlayerName} がサイコロを振ります！";

        playersWaitingForRoll.Remove(player);

        dice.StartRoll((resultFace) =>
        {
            int diceValue;
            if (int.TryParse(resultFace, out diceValue))
            {
                player.CurrentDiceResult = diceValue;
            }

            // ロールが完了したら、サイコロを非表示に戻す
            dice.gameObject.SetActive(false);

            if (playersWaitingForRoll.Count == 0)
            {
                UpdateGameState(GameState.CheckResults);
            }
            else
            {
                resultText.text = $"待機中... 次のプレイヤーの操作を待っています。";
            }
        });
    }

    IEnumerator CheckAllResultsRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        // 判定フェーズでは、全てのプレイヤーの出目を確定させる (念のため)
        foreach (PlayerInfo player in players.OrderBy(p => p.PlayerID))
        {
            // Rollが実行されていないプレイヤーがいたらスキップ (ありえないはずだが安全策)
            if (player.IsEliminated || player.CurrentDiceResult == 0) continue;

            DiceRoll dice = playerDices[player];

            // 1. スコア加算
            player.TotalScore += player.CurrentDiceResult;
            UpdateScoreUIs();

            // 2. 脱落判定
            if (player.CurrentDiceResult == currentBabaDiceValue)
            {
                player.IsEliminated = true;
                player.EliminationTurn = currentTurn;
                // dice.gameObject.SetActive(false); // Roll完了時に非表示済み

                resultText.text = $"{player.PlayerName} が脱落！ (出目: {player.CurrentDiceResult} = BABA: {currentBabaDiceValue})";
                UpdateScoreUIs();
                yield return new WaitForSeconds(3.0f);
            }
            else
            {
                resultText.text = $"{player.PlayerName} はセーフ！";
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 3. ターン終了チェック
        int aliveCount = players.Count(p => !p.IsEliminated);

        if (aliveCount <= 1 || currentTurn >= maxTurns)
        {
            UpdateGameState(GameState.GameOverCheck);
        }
        else
        {
            currentTurn++;
            UpdateGameState(GameState.SetBabaDice);
        }
    }

    private void UpdateScoreUIs()
    {
        int count = Mathf.Min(players.Count, playerScoreTexts.Length);

        for (int i = 0; i < count; i++)
        {
            if (playerScoreTexts[i] == null) continue;

            PlayerInfo p = players[i];

            if (p.IsEliminated)
            {
                playerScoreTexts[i].text = "脱落";
                playerScoreTexts[i].color = Color.gray;
            }
            else
            {
                playerScoreTexts[i].text = $"{p.TotalScore}";
                playerScoreTexts[i].color = Color.black;
            }
        }
    }

    void DisplayFinalRanking()
    {
        // ... (省略: 順位付けロジックは変更なし) ...
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
}
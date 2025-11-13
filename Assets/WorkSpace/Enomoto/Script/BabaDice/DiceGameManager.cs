using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Microsoft.Win32.SafeHandles;
using Photon.Realtime;

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

    [Header("プレイヤーダイスのPrefab")]
    public GameObject[] assignedDicePrefabs;

    [Header("ダイス出現位置 (手動設定)")]
    public Transform[] playerSpawnPoints = new Transform[4];

    [Header("Input System 設定")]
    public InputActionAsset inputActionAsset;

    [Header("BABAダイスへの参照")]
    public BABADiceRoll babaDiceRoll;

    [Header("UI参照")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI babaDiceText;
    public TextMeshProUGUI resultText;
    public Button rollButton;

    [Header("スコア表示UI")]
    public TextMeshProUGUI[] playerScoreTexts = new TextMeshProUGUI[4];

    private int currentBabaDiceValue;
    private List<PlayerInfo> players;
    private Dictionary<PlayerInfo, DiceRoll> playerDices;
    private List<PlayerInfo> playersWaitingForRoll;

    private int playersFinishedRoll = 0;
    private Coroutine handleResultsRoutine;

    void Awake(){
        SetupPlayers();
    }

    void Start()
    {
        UpdateGameState(GameState.Start);
    }

    // PlayerInputHandlerから呼ばれる、プレイヤーの登録処理
    public void TryRegisterNewPlayer(PlayerInput input, DiceRoll diceScript, PlayerInputHandler handler)
    {
        // 最大プレイヤー数を超えていたら登録しない
        if (players.Count >= maxPlayers)
        {
            Debug.LogWarning("最大プレイヤー数に達しました。");
            Destroy(input.gameObject); 
            return;
        }

        int newPlayerId = players.Count + 1;
        int prefabIndex = newPlayerId - 1;
        string newPlayerName = $"Player {newPlayerId}";

        // 1. 新しいダイスとして使用するPrefab (実体) を取得
        if (prefabIndex >= assignedDicePrefabs.Length || assignedDicePrefabs[prefabIndex] == null)
        {
            Debug.LogError($"プレイヤー {newPlayerId} のダイスPrefabが設定されていません (Index: {prefabIndex})。");
            Destroy(input.gameObject);
            return;
        }
        GameObject actualDicePrefab = assignedDicePrefabs[prefabIndex];

        // PlayerInputHandlerはOnRollイベント受付に必要なので残す
        if (diceScript != null) Destroy(diceScript);

        // コンテナ（親）の名前を設定
        input.gameObject.name = $"{newPlayerName}_Container";

        // 2. 割り当てられたPrefab (実体) をコンテナの子として生成
        GameObject actualDiceObject = Instantiate(actualDicePrefab, input.transform);
        actualDiceObject.name = $"{newPlayerName}_Dice_Actual";

        actualDiceObject.transform.position = input.transform.position;
        actualDiceObject.transform.rotation = input.transform.rotation;

        // 3. 生成したダイスオブジェクトからDiceRollを取得
        DiceRoll actualDiceScript = actualDiceObject.GetComponent<DiceRoll>();

        if (actualDiceScript == null)
        {
            Debug.LogError($"【致命的エラー】生成されたPrefab '{actualDicePrefab.name}' に DiceRoll コンポーネントがありません。", actualDiceObject);
            Destroy(input.gameObject);
            return;
        }

        // 4. PlayerInfoを作成し、Handlerに設定
        PlayerInfo newPlayer = new PlayerInfo(newPlayerId, newPlayerName);
        handler.PlayerData = newPlayer;
        handler.GameManager = this;

        players.Add(newPlayer);
        playerDices.Add(newPlayer, actualDiceScript);

        // 5. 出現位置設定 (コンテナ（親）を移動させる)
        if (newPlayerId - 1 < playerSpawnPoints.Length && playerSpawnPoints[newPlayerId - 1] != null)
        {
            input.transform.position = playerSpawnPoints[newPlayerId - 1].position;
            input.transform.rotation = playerSpawnPoints[newPlayerId - 1].rotation;
        }

        UpdateScoreUIs();

        Debug.Log($"[カスタム生成成功] {newPlayerName} が参加しました。");

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

    private void StartPlayerTurn()
    {
        playersWaitingForRoll.Clear();

        foreach (var p in players.Where(p => !p.IsEliminated))
        {
            playersWaitingForRoll.Add(p);
            p.CurrentDiceResult = 0;
        }

        playersFinishedRoll = 0;

        UpdateScoreUIs();
        
        if (playersWaitingForRoll.Count > 0)
        {
            PlayerInfo firstPlayer = playersWaitingForRoll.First();
            resultText.text = $"ターン {currentTurn} 開始! {firstPlayer.PlayerName} の操作待ちです。";
        }
        else
        {
            UpdateGameState(GameState.GameOverCheck);
        }
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
            StartPlayerTurn();
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

        if (currentState != GameState.PlayerRolling) return;

        if (!playersWaitingForRoll.Contains(player))
        {
            resultText.text = $"{player.PlayerName} は既にロール済みか、脱落しています。";
            return;
        }

        Debug.Log($"【ロール開始】: {player.PlayerName}");

        StartSinglePlayerRoll(player);
    }

    void OnBabaRollComplete(string babaFace)
    {
        int.TryParse(babaFace, out currentBabaDiceValue);

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

        Transform parentTransform = dice.transform.parent;
        dice.transform.position = parentTransform.position;
        dice.transform.rotation = parentTransform.rotation;

        resultText.text = $"{player.PlayerName} のダイスロール！";

        playersWaitingForRoll.Remove(player);

        dice.StartRoll((resultFace) =>
        {
            int diceValue;
            if (int.TryParse(resultFace, out diceValue))
            {
                player.CurrentDiceResult = diceValue;
            }

            UpdateScoreUIs();

            playersFinishedRoll++;

            int totalActivePlayers = players.Count(p => !p.IsEliminated);

            if (playersFinishedRoll >= totalActivePlayers)
            {
                if (handleResultsRoutine == null)
                {
                    Debug.Log($"[Turn Manager] 全員完了 ({playersFinishedRoll}/{totalActivePlayers})。コルーチンを起動。");
                    // 処理を次のフレームまで遅延させるコルーチンを開始
                    handleResultsRoutine = StartCoroutine(HandleResultsCoroutine());
                }
                else
                {
                    Debug.LogWarning("[Turn Manager] 既にコルーチン起動済み。多重呼び出しを阻止。");
                }
            }
            else
            {
                Debug.Log($"[Turn Manager] ロール完了: {playersFinishedRoll}/{totalActivePlayers}");
                PlayerInfo nextPlayer = playersWaitingForRoll.FirstOrDefault();
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
            if (GameManager.instance.IsOnline())
            {
                assignedDicePrefabs[player.PlayerID].GetComponent<DiceScoreManager>().AddScore(player.CurrentDiceResult);
                UpdateScoreUIs();
            }
            else
            {
                player.TotalScore += player.CurrentDiceResult;
                UpdateScoreUIs();
            }

            // 2. 脱落判定
            if (player.CurrentDiceResult == currentBabaDiceValue)
            {
                player.IsEliminated = true;
                player.EliminationTurn = currentTurn;
                dice.gameObject.SetActive(false);

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

    private IEnumerator HandleResultsCoroutine()
    {
        // 処理を次のフレームまで待機 (多重起動の阻止)
        yield return null;

        Debug.Log($"[Turn Manager] コルーチン実行開始 (Turn: {currentTurn})");

        // スコア加算とBABA判定
        int babaValue = babaDiceRoll.LastDiceValue;

        // スコア加算
        foreach (var p in players.Where(p => !p.IsEliminated))
        {
            if (p.CurrentDiceResult > 0)
            {
                p.TotalScore += p.CurrentDiceResult;
            }
        }

        // BABA判定
        if (babaValue > 0)
        {
            List<PlayerInfo> eliminatedPlayers = new List<PlayerInfo>();

            foreach (var p in players.Where(p => !p.IsEliminated && p.CurrentDiceResult == babaValue))
            {
                p.IsEliminated = true;
                p.EliminationTurn = currentTurn;
                p.TotalScore = 0;

                eliminatedPlayers.Add(p);
            }

            foreach (var p in eliminatedPlayers)
            {
                if (playerDices.ContainsKey(p) && playerDices[p] != null)
                {
                    playerDices[p].gameObject.SetActive(false);
                    Debug.Log($"[Elimination] {p.PlayerName} が脱落しました。ダイスを非表示にしました。");
                }
            }
        }

        UpdateScoreUIs();

        // ターン進行とリザルトチェック
        bool gameOver = players.Count(p => !p.IsEliminated) <= 1 || currentTurn >= maxTurns;

        if (gameOver)
        {
            UpdateGameState(GameState.GameOverCheck);
        }
        else
        {
            currentTurn++;
            Debug.Log($"[Turn Manager] 次のターンへ移行: {currentTurn}T");

            UpdateGameState(GameState.SetBabaDice);
        }

        // コルーチン参照をクリアしてロックを解除
        handleResultsRoutine = null;
        Debug.Log($"[Turn Manager] コルーチン実行終了。ロック解除 (Next Turn: {currentTurn})");
    }
}
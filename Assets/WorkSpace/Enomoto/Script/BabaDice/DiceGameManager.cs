using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections; // ★ System.Collections.Hashtable の参照
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using ExitGames.Client.Photon; // ★ ExitGames.Client.Photon.Hashtable の参照
using Photon.Pun;
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

public class DiceGameManager : MonoBehaviourPunCallbacks
{
    public static GameState currentState;

    public int currentTurn = 1;
    public int maxTurns = 5;

    [Header("ゲーム設定")]
    public int maxPlayers = 4;

    [Header("プレイヤーダイスのPrefab")]
    // プレイヤーの数だけDiceRollコンポーネントを持つPrefabをセット
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
    public TextMeshProUGUI[] playerScoreTexts = new TextMeshProUGUI[4];

    // DiceScoreManagerへの参照を保持する辞書フィールド
    private Dictionary<PlayerInfo, DiceScoreManager> playerDiceScoreManagers = new Dictionary<PlayerInfo, DiceScoreManager>();

    private List<PlayerInput> playerInputs = new List<PlayerInput>();
    private Dictionary<PlayerInfo, DiceRoll> playerDices = new Dictionary<PlayerInfo, DiceRoll>();
    private List<PlayerInfo> players = new List<PlayerInfo>();

    // ダイスが振られるのを待っているプレイヤーの数
    private int playersWaitingForRoll = 0;

    // 現在までにダイスを振ったプレイヤーの数を追跡
    private int playersFinishedRolling = 0;

    void Awake()
    {
        // リストの初期化
        players.Clear();
        playerInputs.Clear();
        playerDices.Clear();
        playerDiceScoreManagers.Clear();

        // UIを初期状態にリセット
        if (turnText != null) turnText.text = $"Turn: {currentTurn} / {maxTurns}";
        if (babaDiceText != null) babaDiceText.text = "BABA: ?";
        if (resultText != null) resultText.text = "";
        UpdateScoreUIs();

        Debug.Log("");
    }

    void Start()
    {
        UpdateGameState(GameState.Start);

        if (currentState == GameState.Start)
        {
            UpdateGameState(GameState.WaitingForPlayers);
        }
    }

    /// <summary>
    /// GameManagerのシングルトンを利用してオンライン状態をチェックします。
    /// </summary>
    public bool IsOnline()
    {
        // GameManagerシングルトンが存在し、かつオンラインモードが有効な場合をチェック
        if (GameManager.instance != null)
        {
            return GameManager.instance.IsOnline();
        }

        // GameManagerが存在しない場合はオフラインと見なします。
        // Debug.LogWarning("GameManager.instance が見つかりません。IsOnline() が False を返します。");
        return false;
    }

    // ゲーム状態の更新
    public void UpdateGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameState] 状態遷移: {currentState}");

        // オンライン時はMaster ClientがRoom CustomPropertiesを更新し、全クライアントに同期させる
        if (IsOnline() && PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash["GameState"] = (int) newState;
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

            ExitGames.Client.Photon.Hashtable turnHash = new ExitGames.Client.Photon.Hashtable();
            turnHash["CurrentTurn"] = currentTurn;
            PhotonNetwork.CurrentRoom.SetCustomProperties(turnHash);
        }

        switch (currentState)
        {
            case GameState.Start:
            // オフライン/オンラインで初期設定を分ける
            if (IsOnline())
            {
                // オンライン：プレイヤーの参加を待つ
                UpdateGameState(GameState.WaitingForPlayers);
            }
            else
            {
                // オフライン用：ローカルプレイヤーの数に応じてすぐに開始
                if (players.Count >= maxPlayers)
                {
                    UpdateGameState(GameState.SetBabaDice);
                }
                else if (players.Count > 0)
                {
                    // プレイヤー登録が完了しているなら開始
                    UpdateGameState(GameState.SetBabaDice);
                }
                // プレイヤーがまだ一人もいない場合は、PlayerInputHandlerからの登録を待つ
            }
            break;
            case GameState.WaitingForPlayers:
            // オンライン時: プレイヤーが揃うのを待つ（OnPlayerEnteredRoomなどでチェック）
            CheckOnlinePlayers();
            break;

            case GameState.SetBabaDice:
            // ★★★ 修正箇所1: プレイヤーリストをPlayerID順にソートする（プレイヤー1からの開始を保証） ★★★
            players = players.OrderBy(p => p.PlayerID).ToList();

            Debug.Log("[Game Flow] BABAダイスを振ります。");

            // --- プレイヤーのスコア/出目リセット処理 ---
            foreach (var p in players)
            {
                p.ResetTurnResult();
            }

            // --- BABAダイスのロール開始 (nullチェックを追加) ---
            if (babaDiceRoll != null)
            {
                // BABAダイスのゲームオブジェクトをアクティブにする
                babaDiceRoll.gameObject.SetActive(true);

                babaDiceRoll.StartRoll((result) =>
                {
                    // ロール完了後
                    // BABADiceRoll.csでLastDiceValueに結果が設定されていることを利用
                    int babaResult = babaDiceRoll.LastDiceValue;

                    if (babaDiceText != null)
                    {
                        babaDiceText.text = $"BABA: {babaResult}";
                    }

                    // 次のフェーズへ
                    UpdateGameState(GameState.PlayerRolling);
                });
            }
            else
            {
                Debug.LogError("BABADiceRollが設定されていません。Inspectorで参照がセットされているか確認してください。");
                // BABAダイスがない場合は処理を進めるために次のフェーズへ（応急処置）
                UpdateGameState(GameState.PlayerRolling);
            }
            SetBabaDice();
            break;

            case GameState.PlayerRolling:
            playersFinishedRolling = 0; // ロール済みのプレイヤー数をリセット
                                        // 脱落していないプレイヤーの数
            playersWaitingForRoll = players.Count(p => !p.IsEliminated);
            Debug.Log($"[PlayerRolling] ロール待ちプレイヤー数: {playersWaitingForRoll}");
            break;
            case GameState.CheckResults:
            // 結果判定は HandleResults() で処理される
            break;
            case GameState.GameOverCheck:
            // 全てのプレイヤーがダイスを振り終わった後の終了判定
            DisplayFinalRanking();
            UpdateGameState(GameState.GameFinished);
            break;
            case GameState.GameFinished:
            Debug.Log("ゲーム終了");
            break;
        }
    }

    /// <summary>
    /// BABAダイスのロールを開始します。
    /// </summary>
    void SetBabaDice()
    {
        if (babaDiceRoll == null)
        {
            Debug.LogError("BABAダイスが見つかりません。");
            return;
        }

        if (IsOnline() && !PhotonNetwork.IsMasterClient)
        {
            // オンラインでマスタークライアントでなければ何もしない
            return;
        }

        if (babaDiceText != null) babaDiceText.text = "BABA: Rolling...";
        babaDiceRoll.StartRoll(OnBabaDiceRollComplete);
    }

    /// <summary>
    /// BABAダイスのロールが完了した時にDiceRollからコールバックされます。
    /// </summary>
    /// <param name="result"></param>
    void OnBabaDiceRollComplete(string result)
    {
        // BABAダイスの結果をUIに表示
        if (babaDiceText != null) babaDiceText.text = $"BABA: {result}";
        Debug.Log($"[BABA] BABAダイス結果: {result}");

        // プレイヤーのロールフェーズへ移行
        UpdateGameState(GameState.PlayerRolling);
    }

    /// <summary>
    /// プレイヤー登録（PlayerInputHandlerから呼ばれる）
    /// </summary>
    public void TryRegisterNewPlayer(PlayerInput playerInput, DiceRoll diceRoll, PlayerInputHandler handler)
    {
        // 既に登録されているInputをチェック
        if (playerInputs.Any(p => p.playerIndex == playerInput.playerIndex))
        {
            return;
        }

        int playerID;
        string playerName;

        if (IsOnline() && playerInput.GetComponent<PhotonView>() != null)
        {
            // ★★★ 修正箇所1: オンライン時のPlayerID決定ロジック ★★★
            Player targetPhotonPlayer = playerInput.GetComponent<PhotonView>().Owner;
            if (targetPhotonPlayer != null)
            {
                // Room内の全プレイヤーをActorID順（＝参加順）にソート
                // ActorNumberはPhotonが保証する一意で参加順の番号です。
                var sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();

                // 現在のターゲットプレイヤーがソートリストの何番目かを探す
                int photonIndex = sortedPlayers.IndexOf(targetPhotonPlayer);

                playerID = photonIndex + 1; // PlayerIDを1から始まる連番に設定 (1, 2, 3...)
                playerName = targetPhotonPlayer.NickName;

                Debug.Log($"[Online Player ID] Actor: {targetPhotonPlayer.ActorNumber}, Index: {photonIndex}, PlayerID: {playerID}, Name: {playerName}");
            }
            else
            {
                // PhotonViewのOwnerがnullの場合（稀なケース）
                playerID = playerInput.playerIndex + 1;
                playerName = $"Player {playerID} (Online-Fallback)";
            }
            // ★★★ 修正箇所1 終了 ★★★
        }
        else
        {
            // ★★★ 修正箇所: オフライン時のPlayerID決定ロジック ★★★
            if (players.Count == 0)
            {
                // 最初のプレイヤーは必ず PlayerID = 1 とする
                playerID = 1;
                playerName = $"Player 1 (Local)";
            }
            else
            {
                // 2人目以降は、現在の最大ID+1とする
                playerID = players.Max(p => p.PlayerID) + 1;
                playerName = $"Player {playerID} (Local)";
            }
            // ★★★ 修正箇所終了 ★★★
        }

        // PlayerInfoの作成
        PlayerInfo newPlayerInfo = new PlayerInfo(playerID, playerName);

        playerInputs.Add(playerInput);
        players.Add(newPlayerInfo);

        // ★★★ 修正箇所2: DiceRollの動的生成ロジック（前回の修正を維持） ★★★
        DiceRoll targetDiceRoll = diceRoll;

        if (targetDiceRoll == null)
        {
            // ここでの playerIndex は Input System のインデックスだが、ダイスPrefab配列の参照に使う
            int dicePrefabIndex = playerID - 1; // プレイヤーIDを0始まりに変換

            if (dicePrefabIndex >= assignedDicePrefabs.Length || dicePrefabIndex >= playerSpawnPoints.Length)
            {
                Debug.LogError($"プレイヤーID {playerID} はダイスPrefabまたは出現位置の範囲外です。ダイスを生成できません。");
            }
            else
            {
                GameObject dicePrefab = assignedDicePrefabs[dicePrefabIndex];
                Transform spawnPoint = playerSpawnPoints[dicePrefabIndex];

                if (dicePrefab != null && spawnPoint != null)
                {
                    // プレイヤーのダイスを生成
                    GameObject diceInstance = Instantiate(dicePrefab, spawnPoint.position, Quaternion.identity);
                    diceInstance.transform.SetParent(playerInput.transform);

                    targetDiceRoll = diceInstance.GetComponentInChildren<DiceRoll>();

                    if (targetDiceRoll == null)
                    {
                        Debug.LogError($"生成されたダイスPrefab '{dicePrefab.name}' に DiceRoll コンポーネントが見つかりません。", diceInstance);
                    }
                    else
                    {
                        diceInstance.SetActive(true);
                        Debug.Log($"[Dice Spawn] {newPlayerInfo.PlayerName} (ID:{playerID}) のダイスを生成しました: {diceInstance.name}");
                    }
                }
            }
        }

        if (targetDiceRoll != null)
        {
            playerDices.Add(newPlayerInfo, targetDiceRoll);
            targetDiceRoll.gameObject.SetActive(true);
        }
        // ★★★ 修正箇所2 終了 ★★★

        // PlayerInputHandlerにPlayerInfoとGameManagerの参照をセット
        handler.PlayerData = newPlayerInfo;
        handler.GameManager = this;

        // DiceScoreManagerの参照を辞書に登録
        DiceScoreManager scoreManager = handler.GetComponent<DiceScoreManager>();
        if (scoreManager != null)
        {
            if (!playerDiceScoreManagers.ContainsKey(newPlayerInfo))
            {
                playerDiceScoreManagers.Add(newPlayerInfo, scoreManager);
            }
        }

        Debug.Log($"プレイヤー登録完了: {newPlayerInfo.PlayerName} (ID: {playerID})");

        UpdateScoreUIs();

        Debug.Log($"[Registration Check] 現在のプレイヤー数: {players.Count} / 最大人数: {maxPlayers}");

        // ★★★ 修正箇所3: Master Clientによるゲーム開始ロジックの強化 ★★★
        if (IsOnline() && PhotonNetwork.IsMasterClient)
        {
            // Master Client のみ、ゲーム開始条件を満たしたら状態遷移を行う
            if (currentState == GameState.WaitingForPlayers && players.Count >= 1)
            {
                // オンライン環境で最初のプレイヤーが登録されたとき
                Debug.Log("[Master Start] 最初のプレイヤーが参加したため、BABAダイス設定へ移行します。");
                UpdateGameState(GameState.SetBabaDice);
            }
            else if (players.Count >= maxPlayers) // 最大人数に達したら開始
            {
                Debug.Log("[Master Start] 最大人数に達したため、BABAダイス設定へ移行します。");
                UpdateGameState(GameState.SetBabaDice);
            }
        }
        else if (!IsOnline())
        {
            // ローカル時のゲーム開始ロジック（以前の修正を維持）
            if (currentState == GameState.WaitingForPlayers && players.Count >= 1)
            {
                Debug.Log("[Local Start] 最初のプレイヤーが参加しました。BABAダイス設定へ移行します。");
                UpdateGameState(GameState.SetBabaDice);
            }
            else if (players.Count >= maxPlayers)
            {
                UpdateGameState(GameState.SetBabaDice);
            }
        }
        // ★★★ 修正箇所3 終了 ★★★
    }

    /// <summary>
    /// プレイヤーのロール入力処理 (PlayerInputHandlerから呼ばれる)
    /// </summary>
    public void HandlePlayerRollInput(PlayerInfo playerInfo)
    {
        if (currentState != GameState.PlayerRolling)
        {
            Debug.Log($"ロール入力拒否: 現在の状態は {currentState} です。");
            return;
        }

        if (playerInfo.IsEliminated)
        {
            Debug.Log($"{playerInfo.PlayerName} は脱落しているためロールできません。");
            return;
        }

        if (playerDices.TryGetValue(playerInfo, out DiceRoll diceRoll))
        {
            if (diceRoll.isRolling) return;

            // オンラインでは、自分のダイスだけがロールできる
            if (IsOnline() && (diceRoll.photonView == null || !diceRoll.photonView.IsMine))
            {
                Debug.Log($"ロール入力拒否: {playerInfo.PlayerName} は自分のダイスではありません。");
                return;
            }

            Debug.Log($"[Roll] {playerInfo.PlayerName} がダイスを振ります。");
            diceRoll.StartRoll((result) => OnDiceRollComplete(playerInfo, result));
        }
        else
        {
            Debug.LogWarning($"プレイヤー {playerInfo.PlayerName} に対応するDiceRollが見つかりません。");
        }

        if (playerInfo.CurrentDiceResult != 0)
        {
            // プレイヤーがダイスを振り直すことを許可しない場合はこのチェックを維持
            Debug.LogWarning($"[Input Rejected] {playerInfo.PlayerName} は既に今ターン振っています。(Result: {playerInfo.CurrentDiceResult})");
            return;
        }
    }

    /// <summary>
    /// プレイヤーダイスのロールが完了した時にDiceRollからコールバックされます。（主にオフライン用）
    /// </summary>
    private void OnDiceRollComplete(PlayerInfo playerInfo, string result)
    {
        // ★★★ 修正箇所: int.TryParse で安全に文字列を整数に変換する ★★★
        if (int.TryParse(result, out int diceResult))
        {
            // 正常に数字に変換できた場合
            playerInfo.CurrentDiceResult = diceResult;

            Debug.Log($"[Roll Complete] {playerInfo.PlayerName} の出目: {diceResult}");
        }
        else
        {
            // 数字に変換できなかった場合 (FormatExceptionの回避)
            Debug.LogError($"[Roll Error] {playerInfo.PlayerName} の出目が無効です: '{result}'。この出目を0として処理を継続します。", this);
            playerInfo.CurrentDiceResult = 0; // 0として処理を継続
        }
        // ★★★ 修正箇所終了 ★★★

        // 全プレイヤーの出目が出揃ったかチェック
        CheckAllPlayersRolled();
    }

    /// <summary>
    /// オンライン時、DiceRollからRPCでダイス結果が同期された時に呼ばれる（DiceRoll.csからRPCターゲットとして呼ばれることを想定）
    /// </summary>
    [PunRPC]
    public void SyncPlayerDiceResult(string nickName, int resultValue)
    {
        if (!IsOnline()) return;

        // PlayerInfoリストからNickNameでプレイヤーを探す
        // オンラインの場合はPlayerNameにNickNameを含めていることを前提とする
        PlayerInfo targetPlayer = players.FirstOrDefault(p => p.PlayerName == nickName);

        if (targetPlayer != null)
        {
            targetPlayer.CurrentDiceResult = resultValue;
            Debug.Log($"[RPC Sync] {targetPlayer.PlayerName} のダイス結果を同期: {resultValue}");

            // ロール完了したプレイヤー数をカウント
            playersFinishedRolling++;

            // 全員がロールを終えたかチェック
            if (playersFinishedRolling >= players.Count(p => !p.IsEliminated))
            {
                Debug.Log("[RPC Sync] 全プレイヤーの結果同期が完了しました。HandleResultsを実行。");
                // マスタークライアントのみ結果判定処理を行う
                if (PhotonNetwork.IsMasterClient)
                {
                    HandleResults();
                }
            }
        }
        else
        {
            Debug.LogWarning($"[RPC Sync] NickName: {nickName} に一致する PlayerInfo が見つかりませんでした。");
        }
    }

    private void SyncScoresFromDiceManagers()
    {
        // playersリスト内の全てのプレイヤーについてスコアを同期
        foreach (var p in players)
        {
            // プレイヤーに対応する DiceRoll を見つける
            if (playerDices.TryGetValue(p, out DiceRoll diceRoll))
            {
                // DiceRollの親（Dice Container）から DiceScoreManager を取得
                Transform diceContainer = diceRoll.transform.parent;
                if (diceContainer != null)
                {
                    DiceScoreManager scoreManager = diceContainer.GetComponent<DiceScoreManager>();

                    if (scoreManager != null)
                    {
                        // DiceScoreManagerから最新のスコア（Custom Propertiesからの値）を取得
                        int latestScore = scoreManager.GetMyScore();

                        if (p.TotalScore != latestScore)
                        {
                            // PlayerInfoのTotalScoreを更新
                            p.TotalScore = latestScore;
                            // Debug.Log($"[Score Sync] {p.PlayerName} のスコアを {latestScore} に同期しました。");
                        }
                    }
                }
            }
        }
    }

    // ターン結果の処理（Master Clientまたはオフラインで実行）
    void HandleResults()
    {
        // 既に判定フェーズ以降であれば無視
        if (currentState == GameState.CheckResults ||
            currentState == GameState.GameOverCheck ||
            currentState == GameState.GameFinished)
        {
            return;
        }

        // マスタークライアントまたはオフライン以外は処理しない
        bool isMasterOrOffline = !IsOnline() || (IsOnline() && PhotonNetwork.IsMasterClient);
        if (!isMasterOrOffline) return;

        // 状態を結果判定へ更新（Master Client/オフラインのみ）
        UpdateGameState(GameState.CheckResults);

        // ★★★ スコア加算処理を最初に実行 ★★★
        // 1. 生存者全員のスコアを加算
        Debug.Log("[HandleResults] スコア加算処理を開始します。");

        foreach (var p in players.Where(p => !p.IsEliminated))
        {
            // 今回の出目
            int scoreToAdd = p.CurrentDiceResult;

            if (!IsOnline())
            {
                // オフライン時: PlayerInfoのTotalScoreを直接更新
                p.TotalScore += scoreToAdd;
                Debug.Log($"[Local Score] {p.PlayerName} のスコア: +{scoreToAdd} -> {p.TotalScore}");
            }
            else // オンライン時: Master ClientがDiceScoreManagerへのRPCを指示
            {
                if (playerDiceScoreManagers.TryGetValue(p, out DiceScoreManager scoreManager))
                {
                    // DiceScoreManagerに実装されている AddScore RPC を呼び出す
                    // DiceScoreManagerは自分のスコアを CustomProperties で更新する
                    if (scoreManager.photonView != null)
                    {
                        // ★ ここで AddScore RPCを呼ぶことが重要
                        scoreManager.photonView.RPC(
                            "ReceiveScoreAddition", // DiceScoreManagerに実装されているRPC名を使う
                            scoreManager.photonView.Owner, // スコアのOwnerに送信
                            scoreToAdd
                        );
                        Debug.Log($"[Online Score] Masterが {p.PlayerName} のDiceScoreManagerへスコア +{scoreToAdd} を指示。");
                    }
                }
            }
        }
        Debug.Log("[HandleResults] 生存者全員のスコア加算処理を完了。");
        // ★★★ スコア加算処理 終了 ★★★

        int babaValue = babaDiceRoll.LastDiceValue;

        // BABA判定と脱落処理
        if (babaValue > 0)
        {
            List<PlayerInfo> eliminatedPlayers = new List<PlayerInfo>();

            foreach (var p in players.Where(p => !p.IsEliminated && p.CurrentDiceResult == babaValue))
            {
                p.IsEliminated = true;
                p.EliminationTurn = currentTurn;

                // ★ 脱落者のスコアを0にリセット
                p.TotalScore = 0;

                // ★ オンライン時：脱落者はスコアを0にリセットするRPCを送信
                if (IsOnline() && PhotonNetwork.IsMasterClient)
                {
                    if (playerDiceScoreManagers.TryGetValue(p, out DiceScoreManager scoreManager))
                    {
                        // RPCでスコアマネージャーの所有者へリセットを指示
                        scoreManager.photonView.RPC(
                            "ReceiveScoreAddition",
                            scoreManager.photonView.Owner,
                            -scoreManager.GetMyScore() // 現在のスコア分マイナスして0にする
                        );
                        Debug.Log($"[Online Reset] {p.PlayerName} が脱落。スコアを0にリセット指示。");
                    }
                }

                eliminatedPlayers.Add(p);
            }

            foreach (var p in eliminatedPlayers)
            {
                if (playerDices.ContainsKey(p) && playerDices[p] != null)
                {
                    // ダイスを非表示
                    Transform diceContainer = playerDices[p].transform.parent;
                    if (diceContainer != null)
                    {
                        diceContainer.gameObject.SetActive(false);
                    }
                    Debug.Log($"[Elimination] {p.PlayerName} が脱落しました。ダイスを非表示にしました。");
                }
            }
        }

        // スコア表示の更新
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

            // ターン毎のダイス結果をリセット
            foreach (var p in players) p.ResetTurnResult();

            UpdateGameState(GameState.SetBabaDice);
        }
    }

    /// <summary>
    /// スコアボードのUI更新
    /// </summary>
    public void UpdateScoreUIs()
    {
        // ターン情報とBABAダイス結果の更新
        // babaDiceRollがnullでないか、LastDiceValueが設定されているかを確認
        string babaResult = (babaDiceRoll != null && babaDiceRoll.LastDiceValue > 0) ? babaDiceRoll.LastDiceValue.ToString() : "N/A";
        turnText.text = $"Turn: {currentTurn} / {maxTurns}";
        babaDiceText.text = $"BABA: {babaResult}";

        // プレイヤーのスコア表示の更新
        for (int i = 0; i < maxPlayers; i++)
        {

            if (IsOnline())
            {
                SyncScoresFromDiceManagers();
            }

            // UI参照のチェック
            if (i >= playerScoreTexts.Length || playerScoreTexts[i] == null)
            {
                continue;
            }

            if (i < players.Count)
            {
                PlayerInfo p = players[i];

                string statusDetail = "";

                if (p.IsEliminated)
                {
                    // スコアは0（修正1でリセットされる）
                    playerScoreTexts[i].text = "脱落";
                    playerScoreTexts[i].color = Color.gray; // 脱落者は灰色
                }
                else
                {
                    if (p.CurrentDiceResult > 0)
                    {
                        // ロール完了後（次のターンが始まる前）
                        statusDetail = $"{p.CurrentDiceResult}";
                    }

                    // ★ 修正：TotalScoreを常に表示し、ステータスを追記
                    playerScoreTexts[i].text = $"{p.TotalScore}";

                    playerScoreTexts[i].color = Color.black; // 生存者は黒
                }
            }
            else
            {
                // 未参加のプレイヤー枠
                playerScoreTexts[i].text = "???";
                playerScoreTexts[i].color = Color.gray;
            }
        }
    }

    /// <summary>
    /// 最終順位の表示
    /// </summary>
    void DisplayFinalRanking()
    {
        // 順位付けロジック: 1. 生存者優先 2. 脱落ターンが遅い順 3. スコアが高い順
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

            // オンラインの場合はDiceScoreManagerから最終スコアを取得
            int finalScore = p.TotalScore;
            if (IsOnline() && playerDiceScoreManagers.TryGetValue(p, out DiceScoreManager scoreManager))
            {
                finalScore = scoreManager.GetMyScore();
            }

            rankString += $"{i + 1}位: {p.PlayerName} | スコア: {finalScore} {status}\n";
        }
        if (resultText != null)
        {
            resultText.text = rankString;
        }
    }

    /// <summary>
    /// オンライン時: 部屋の人数をチェックし、ゲームを開始するかどうか判断（Master Clientのみ実行）
    /// </summary>
    void CheckOnlinePlayers()
    {
        if (!IsOnline() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;

        // 参加しているプレイヤー数をチェック
        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {
            // プレイヤーが揃ったらゲーム開始
            UpdateGameState(GameState.SetBabaDice);
        }
    }

    // --- Photon コールバック ---

    /// <summary>
    /// Room CustomPropertiesが更新されたときに呼ばれる
    /// </summary>
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        // オフライン時は無視
        if (!IsOnline()) return;

        // ターンが更新された場合
        if (changedProps.ContainsKey("CurrentTurn"))
        {
            currentTurn = (int) changedProps["CurrentTurn"];
            if (turnText != null)
            {
                turnText.text = $"Turn: {currentTurn} / {maxTurns}";
            }
        }

        // GameStateが更新された場合
        if (changedProps.ContainsKey("GameState"))
        {
            GameState newState = (GameState) ((int) changedProps["GameState"]);
            // ローカルでゲーム状態を更新
            currentState = newState;
            Debug.Log($"[GameState Sync] 状態遷移: {currentState} (Room Prop)");
        }
    }

    /// <summary>
    /// プレイヤーが入室したときに呼ばれる
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!IsOnline()) return;

        Debug.Log($"[Photon] {newPlayer.NickName} が入室しました。");

        // マスタークライアントならプレイヤーが揃ったかチェック
        CheckOnlinePlayers();
    }

    /// <summary>
    /// プレイヤーが退室したときに呼ばれる
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!IsOnline()) return;

        Debug.Log($"[Photon] {otherPlayer.NickName} が退室しました。");

        // (注: 退室したプレイヤーのPlayerInfoをリストから削除する処理を実装する必要がありますが、ここでは省略しています)

        // プレイヤー数チェック
        if (PhotonNetwork.IsMasterClient)
        {
            // プレイヤーが一人になったらゲーム終了などの処理を行う
        }
    }

    /// <summary>
    /// 現在のターンで全プレイヤーがダイスを振ったかチェックする
    /// </summary>
    private void CheckAllPlayersRolled()
    {
        // 現在のターンで脱落していないプレイヤー全員がダイスを振ったかチェック
        // CurrentDiceResult > 0 であれば振ったとみなす
        bool allRolled = players
            .Where(p => !p.IsEliminated) // 脱落者を除外
            .All(p => p.CurrentDiceResult > 0);

        // 全員が振り終わり、かつ現在の状態がPlayerRollingである場合
        if (allRolled && currentState == GameState.PlayerRolling)
        {
            Debug.Log("[Game Flow] 全プレイヤーのロールが完了しました。結果判定へ移行します。");

            // オンラインの場合はMaster Clientのみが状態遷移を指示する
            if (IsOnline() && PhotonNetwork.IsMasterClient)
            {
                UpdateGameState(GameState.CheckResults);
            }
            else if (!IsOnline())
            {
                // ローカルプレイ時は直接状態遷移
                UpdateGameState(GameState.CheckResults);
            }
        }
    }
}
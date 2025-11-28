using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用する場合
using UnityEngine.InputSystem;
using System.Linq;
using System.Text;

/**
 * @file DiceGameManager.cs
 * @brief ダイスゲーム全体の進行管理。オンライン・オフライン両対応。
 */
public class DiceGameManager : MonoBehaviourPunCallbacks
{
    public static DiceGameManager instance;

    // 新しいゲーム状態の定義
    private enum GameState
    {
        Initializing,
        Lobby, // ロビー状態（プレイヤー待ち/開始待ち）
        Rolling,
        ProcessingResult,
        Finished
    }
    private GameState currentGameState = GameState.Initializing;


    [Header("ゲーム設定")]
    [SerializeField] private int maxTurns = 5;
    [SerializeField] private float timeLimit = 5.0f; // ダイスを振る制限時間

    [Header("ロビー設定")]
    [SerializeField] private float lobbyDuration = 60f; // ロビー制限時間 (秒)
    [SerializeField] private int minPlayersToStart = 2; // 最小開始人数
    [SerializeField] private int maxPlayers = 4; // 最大開始人数 (4人揃ったら自動開始)
    private float currentLobbyTimer = 0f;


    [Header("参照")]
    [SerializeField] private GameObject dicePrefab; // プレイヤーダイスのPrefab（オフライン時はPlayerInfomation内の既存オブジェクトを優先）
    [SerializeField] private GameObject playerInfo;
    [SerializeField] private Transform[] spawnPoints; // プレイヤーごとのダイス生成位置 (Size=4)
    [SerializeField] private Material[] playerMaterials; // プレイヤー識別用マテリアル (Size=4)
    [SerializeField] private Material babaMaterial; // BABAダイス用マテリアル (UI表示用として残す)

    [Header("UI")]
    [SerializeField] private Image[] playerResultImages;          // プレイヤーの出目表示用UI (Size=4)
    [SerializeField] private Image babaResultImage;               // BABAダイスの出目表示用UI
    [SerializeField] private Sprite[] diceSprites;                // 1~6のサイコロ画像
    [SerializeField] private TextMeshProUGUI infoText;            // ゲーム状態表示テキスト
    [SerializeField] private TextMeshProUGUI timerText;           // タイマー表示
    [SerializeField] private TextMeshProUGUI[] playerScoreTexts;  // スコア表示用テキスト

    // 内部変数
    private int currentTurn = 0;
    private int activePlayerCount = 0;
    private float currentTimer = 0f;
    private bool waitingForRoll = false;
    private int currentActivePlayerIndex = 0;

    // キー: ActorNumber(オフライン時はPlayerIndex), 値: DiceObject
    private Dictionary<int, DiceObject> playerDiceObjects = new Dictionary<int, DiceObject>();

    // キー: ActorNumber, 値: 今回の出目
    private Dictionary<int, int> currentTurnResults = new Dictionary<int, int>();

    // BABAの数字
    private int currentBabaNumber = -1;

    // プレイヤー情報リスト（脱落していない人）
    private List<PlayerInfomation> activePlayers = new List<PlayerInfomation>();

    // 初期化完了フラグ
    private bool isInitialized = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeGameFlow());
    }

    private IEnumerator InitializeGameFlow()
    {
        // プレイヤー情報取得のために少し待つ
        yield return new WaitForSeconds(1.5f);

        activePlayers.AddRange(FindObjectsOfType<PlayerInfomation>());
        activePlayers.Sort((a, b) => a.myNumber.CompareTo(b.myNumber));

        activePlayerCount = activePlayers.Count;

        // オフラインではプレイヤー数が固定
        if (!GameManager.instance.IsOnline() && activePlayerCount < minPlayersToStart)
        {
            Debug.LogError($"オフラインモード: プレイヤー数が最低開始人数({minPlayersToStart}人)に満たないため開始できません。");
            if (infoText) infoText.text = "Error: Not enough players in Offline Mode";
            yield break;
        }

        if (GameManager.instance.IsOnline())
        {
            // マスタークライアントのみがダイスを生成
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnDiceOnline();
            }

            currentGameState = GameState.Lobby;
            if (CheckAuthority())
            {
                currentLobbyTimer = lobbyDuration;
                // マスターはロビー状態の開始を全クライアントに通知
                photonView.RPC(nameof(SyncGameState), RpcTarget.All, (int) currentGameState, currentLobbyTimer);
            }
        }
        else // オフライン
        {
            SpawnDiceOffline();
            SetupOfflineInputs();
            currentGameState = GameState.Lobby;
            currentLobbyTimer = lobbyDuration; // オフラインでも待機時間と手動開始を許可
        }

        yield return new WaitForEndOfFrame();

        isInitialized = true;
        Debug.Log("Game Initialization Complete. Entering Lobby State.");
    }

    private bool CheckAuthority()
    {
        return !GameManager.instance.IsOnline() || PhotonNetwork.IsMasterClient;
    }

    #region 生成処理 (変更なし - BABAダイス関連の修正は前回実施済み)

    private void SpawnDiceOnline()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerInfomation p = activePlayers[i];
            int actorNum = p.GetComponent<PhotonView>().OwnerActorNr;
            Vector3 pos = spawnPoints[i % spawnPoints.Length].position;
            Quaternion rot = spawnPoints[i % spawnPoints.Length].rotation;

            object[] data = new object[] { actorNum };

            GameObject diceObj = PhotonNetwork.InstantiateRoomObject(dicePrefab.name, pos, rot, 0, data);

            // セットアップRPC
            diceObj.GetComponent<PhotonView>().RPC(nameof(SetupDiceRPC), RpcTarget.AllBuffered, diceObj.GetComponent<PhotonView>().ViewID, actorNum, i, pos, rot);
        }
    }

    private void SpawnDiceOffline()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerInfomation p = activePlayers[i];
            int id = p.myNumber;

            GameObject diceObj = p.dicePlayer;
            DiceObject diceScript = null;

            if (diceObj == null)
            {
                Debug.LogError($"Player {id}: dicePlayer is null. Skipping dice setup for this player. Ensure dicePlayer is assigned in the Inspector.");
                continue;
            }

            diceScript = diceObj.GetComponent<DiceObject>();
            if (diceScript == null)
            {
                diceScript = diceObj.AddComponent<DiceObject>();
            }

            // プレイヤーダイスもスポーンポイントの位置・回転を使用
            Transform sp = spawnPoints[i % spawnPoints.Length];
            diceObj.transform.position = sp.position;
            diceObj.transform.rotation = sp.rotation;

            // DiceObjectに初期位置を設定
            diceScript.InitialPosition = sp.position;
            diceScript.InitialRotation = sp.rotation;

            diceScript.Initialize(id, playerMaterials[i % playerMaterials.Length], i);

            playerDiceObjects.Add(id, diceScript);
        }
    }

    private void SetupOfflineInputs()
    {
        foreach (var player in activePlayers)
        {
            PlayerInput pInput = player.GetComponent<PlayerInput>();
            if (pInput == null) pInput = player.GetComponentInChildren<PlayerInput>();

            if (pInput != null)
            {
                pInput.SwitchCurrentActionMap("DiceGame");

                InputAction rollAction = pInput.actions.FindAction("Roll");
                if (rollAction != null)
                {
                    int myNum = player.myNumber;
                    rollAction.performed += ctx => OnLocalRoll(myNum);
                }
            }
        }
    }

    [PunRPC]
    public void SetupDiceRPC(int viewID, int actorNumber, int materialIndex, Vector3 pos, Quaternion rot)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null)
        {
            DiceObject d = view.GetComponent<DiceObject>();
            Material mat = playerMaterials[materialIndex % playerMaterials.Length];

            d.InitialPosition = pos;
            d.InitialRotation = rot;

            d.Initialize(actorNumber, mat, materialIndex);

            if (!playerDiceObjects.ContainsKey(actorNumber))
            {
                playerDiceObjects.Add(actorNumber, d);
            }
        }
    }

    [PunRPC]
    public void SetupBabaDiceRPC(int viewID, Vector3 pos, Quaternion rot)
    {
        // BABAダイスは物理オブジェクトとして生成しなくなるため、このRPCは使用されません。
    }

    public Material GetPlayerMaterial(int index)
    {
        if (index >= 0 && index < playerMaterials.Length) return playerMaterials[index];
        return null;
    }

    public Transform GetSpawnPoint(int actorNumber)
    {
        // プレイヤー情報オブジェクト（PlayerInfomation）を検索
        // activePlayersリストがmyNumberまたはActorNumber順にソートされていることが理想
        PlayerInfomation targetPlayer = activePlayers.Find(p =>
            (GameManager.instance.IsOnline()
                ? p.GetComponent<PhotonView>().OwnerActorNr
                : p.myNumber) == actorNumber);

        if (targetPlayer != null)
        {
            // activePlayersリスト内のインデックスを取得
            // activePlayersがプレイヤー順にソートされていれば、インデックスはスポーン位置のインデックスと一致します
            int playerIndex = activePlayers.IndexOf(targetPlayer);

            // 配列の範囲チェック
            if (spawnPoints != null && spawnPoints.Length > playerIndex)
            {
                return spawnPoints[playerIndex];
            }
            else
            {
                Debug.LogError($"SpawnPointが未設定、またはプレイヤーインデックス({playerIndex})が範囲外です。SpawnPoints Length: {spawnPoints?.Length ?? 0}");
                return null;
            }
        }

        Debug.LogError($"ActorNumber {actorNumber} に対応するPlayerInfomationが見つかりません。");
        return null;
    }

    #endregion

    #region ゲームループ/状態管理

    // 状態同期RPC（ロビー状態とゲーム開始を同期）
    [PunRPC]
    private void SyncGameState(int state, float timer)
    {
        currentGameState = (GameState) state;
        if (currentGameState == GameState.Lobby)
        {
            currentLobbyTimer = timer;
            UpdateLobbyUI(); // UIを即時更新
        }
        else if (currentGameState == GameState.Rolling)
        {
            // ロビーからローリング状態への遷移
            if (CheckAuthority())
            {
                StartTurn(); // マスターはターン開始ロジックを実行
            }
            else
            {
                // 非マスタークライアントはUIをクリアし、マスターからのSyncTurnStateを待つ
                if (infoText) infoText.text = "ゲーム開始！";
                if (timerText) timerText.text = "";
            }
        }
    }

    // ロビータイマー同期RPC（マスターから非マスターへ）
    [PunRPC]
    private void SyncLobbyTimer(float timer)
    {
        if (currentGameState == GameState.Lobby)
        {
            currentLobbyTimer = timer;
        }
    }

    private void Update()
    {
        // 初期化が終わるまで待機
        if (!isInitialized) return;

        if (currentGameState == GameState.Lobby)
        {
            HandleLobbyState();
        }

        if (currentGameState == GameState.Rolling)
        {
            // 【重要】マスタークライアントのみがタイマーを減算し、0になったらターンを進める
            if (PhotonNetwork.IsMasterClient)
            {
                currentTimer -= Time.deltaTime;

                // 全クライアントにタイマー値を同期するRPC (頻繁に呼ばれるため、この方法のパフォーマンスに注意)
                // もし頻繁なRPCを避けたい場合は、マスタークライアントのカスタムプロパティで同期します。
                photonView.RPC(nameof(SyncTurnTimer), RpcTarget.Others, currentTimer);

                // タイムオーバー処理
                if (currentTimer <= 0f)
                {
                    // ここでタイマーをリセットし、自動ロール＆次のターンへ進めるRPCを呼ぶ
                    currentTimer = timeLimit; // 次のターンのためにリセット

                    // 【重要】自動ロール処理を呼び出す
                    photonView.RPC(nameof(AutoRollDice), RpcTarget.MasterClient);
                }
            }
            else
            {
                // 非マスタークライアントは、マスタークライアントから送られてきた値でタイマーを更新します
                // ローカルな減算は停止してください。
            }

            // UIの更新 (ローカルで実行)
            UpdateTimerUI(currentTimer);
        }
    }

    /// <summary>
    /// 制限時間のUIを更新します。
    /// </summary>
    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            // 残り時間を秒単位でフォーマットして表示
            // Mathf.Max(0f, timeRemaining) でマイナス値が表示されないようにします。
            timerText.text = $"Time: {Mathf.Max(0f, timeRemaining):F1}s";
        }
    }

    [PunRPC]
    public void SyncTurnTimer(float timerValue)
    {
        // 非マスタークライアントはこのRPCでタイマー値を受け取る
        if (!PhotonNetwork.IsMasterClient)
        {
            currentTimer = timerValue;
        }
    }

    [PunRPC]
    private void AutoRollDice()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 現在のプレイヤーのダイスを強制的にロールするロジック
        // ... 例：ForceRollCurrentPlayerDice();

        // ロールが完了したら、StartTurn()の最後に進むロジック（次のプレイヤーへ）を実行
        StartTurn();
    }

    private void HandleLobbyState()
    {
        // UIは全てのクライアントで更新
        UpdateLobbyUI();

        if (!CheckAuthority()) return;

        // --- Master Client / Offline Host Logic ---

        // 1. Timer countdown
        currentLobbyTimer -= Time.deltaTime;

        int currentPlayers = GameManager.instance.IsOnline() ? PhotonNetwork.CurrentRoom.PlayerCount : activePlayers.Count;

        // 2. 自動開始チェック (最大人数到達 or 時間切れ)
        bool autoStartCondition = currentPlayers >= maxPlayers || currentLobbyTimer <= 0;

        // 3. 手動開始チェック (Enter/Spaceキー)
        bool manualStartInput = Keyboard.current != null &&
                                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

        // 4. 開始条件判定
        if (currentPlayers >= minPlayersToStart && (autoStartCondition || manualStartInput))
        {
            ManualStartGame();
            return;
        }

        // 5. タイマーの同期 (オンライン時のみ)
        if (GameManager.instance.IsOnline())
        {
            // タイマーが整数値を跨いだときに同期 (RPCの削減)
            if (Mathf.Floor(currentLobbyTimer + Time.deltaTime) != Mathf.Floor(currentLobbyTimer) && currentLobbyTimer > 0)
            {
                photonView.RPC(nameof(SyncLobbyTimer), RpcTarget.Others, currentLobbyTimer);
            }
        }
    }

    private void UpdateLobbyUI()
    {
        int currentPlayers = GameManager.instance.IsOnline() ? PhotonNetwork.CurrentRoom.PlayerCount : activePlayers.Count;

        string statusText;
        string timerDisplay = currentLobbyTimer > 0 ? currentLobbyTimer.ToString("F0") : "待機中";

        if (currentPlayers < minPlayersToStart)
        {
            statusText = $"プレイヤー待ち... ({currentPlayers}/{minPlayersToStart}人)\n最低{minPlayersToStart}人必要です。";
        }
        else
        {
            statusText = $"参加者: {currentPlayers}人\n";
            if (CheckAuthority())
            {
                statusText += $"[Enter/Space]キーで開始\nまたは制限時間まで待機: ";
            }
            else
            {
                statusText += "ホストの開始操作、または時間切れを待っています: ";
            }

        }

        if (infoText) infoText.text = statusText;
        if (timerText) timerText.text = timerDisplay;
    }

    private void ManualStartGame()
    {
        if (!CheckAuthority() || currentGameState != GameState.Lobby) return;

        Debug.Log("Game manually started or started by condition.");

        if (GameManager.instance.IsOnline())
        {
            // Master Client calls RPC to synchronize game start
            photonView.RPC(nameof(SyncGameState), RpcTarget.All, (int) GameState.Rolling, 0f);
        }
        else
        {
            // Offline direct transition
            currentGameState = GameState.Rolling;
            StartTurn();
        }
    }

    // Photonのコールバックで、プレイヤーが入室したことを検知し、自動開始条件をチェック (オンライン時のみ)
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (CheckAuthority() && currentGameState == GameState.Lobby)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
            {
                ManualStartGame();
            }
        }
    }

    private void StartTurn()
    {
        if (!CheckAuthority() || currentGameState != GameState.Rolling) return;

        if (currentTurn > 0 && activePlayerCount <= 1)
        {
            EndGame();
            return;
        }

        if (currentTurn >= maxTurns)
        {
            EndGame();
            return;
        }

        currentTurn++;

        if (GameManager.instance.IsOnline())
            photonView.RPC(nameof(SyncTurnState), RpcTarget.All, currentTurn, "ROLL_START");
        else
            SyncTurnState(currentTurn, "ROLL_START");
    }

    [PunRPC]
    private void SyncTurnState(int turn, string state)
    {
        currentTurn = turn;
        currentTimer = timeLimit;
        waitingForRoll = true;
        currentTurnResults.Clear();

        foreach (var img in playerResultImages) img.gameObject.SetActive(false);
        babaResultImage.gameObject.SetActive(false);

        ResetDiceStates();

        if (infoText) infoText.text = $"TURN {turn} START!\nダイスを振れ！";
        if (timerText) timerText.text = currentTimer.ToString("F0"); // ターン開始時にもタイマー表示

        // BABAダイスはマスターが自動で振る (乱数生成)
        if (CheckAuthority())
        {
            // 1から6のランダムな整数を生成
            int newBabaNumber = Random.Range(1, 7); // UnityのRandom.Range(int min, int max)はmax排他的なので7を指定

            // 結果を全員に同期 (マスタークライアントからRPCを送信)
            if (GameManager.instance.IsOnline())
            {
                photonView.RPC(nameof(SyncBabaResult), RpcTarget.All, newBabaNumber);
            }
            else
            {
                // オフライン時は直接反映
                SyncBabaResult(newBabaNumber);
            }

            Debug.Log($"BABA Dice Result Generated: {newBabaNumber}");
        }
    }

    /// <summary>
    /// BABAダイスの結果を同期し、UIを更新するRPC
    /// </summary>
    [PunRPC]
    private void SyncBabaResult(int number)
    {
        currentBabaNumber = number;
        // BABAの結果UIを即座に表示
        ShowDiceUI(-999, currentBabaNumber);
    }

    /// <summary>
    /// ターン開始時に全ダイスの状態をリセットし、再ロール可能にする。
    /// </summary>
    private void ResetDiceStates()
    {
        foreach (var kvp in playerDiceObjects)
        {
            if (kvp.Value != null) kvp.Value.ResetDiceState();
        }

        currentBabaNumber = -1;
    }

    // 自分のダイスを振る入力（変更なし）
    public void OnRollInput()
    {
        // ★ 1. ゲーム状態と待機状態のチェック
        if (currentGameState != GameState.Rolling || !waitingForRoll) return;

        if (GameManager.instance.IsOnline())
        {
            // ★ 2. ターン所有権のチェックを追加
            if (!IsMyTurn())
            {
                Debug.LogWarning("Roll input ignored: It's not your turn.");
                return;
            }

            int myID = PhotonNetwork.LocalPlayer.ActorNumber;
            // RPC名を前回の提案と合わせるなら RequestRoll ではなく RollDiceRequest が推奨
            // photonView.RPC(nameof(RollDiceRequest), RpcTarget.MasterClient, myID);

            // 現在のコードに合わせて RequestRoll を使用
            photonView.RPC(nameof(RequestRoll), RpcTarget.MasterClient, myID);
        }
        else
        {
            Debug.LogWarning("Offline mode: OnRollInput() called without specific player context. Use SetupOfflineInputs event.");
        }
    }

    // オフラインで特定のプレイヤーが入力したときの処理（変更なし）
    private void OnLocalRoll(int playerNumber)
    {
        if (currentGameState != GameState.Rolling || !waitingForRoll) return;
        RequestRoll(playerNumber);
    }

    // マスタークライアントが受け取るロール要求（変更なし）
    [PunRPC]
    private void RequestRoll(int actorNumber)
    {
        if (!CheckAuthority()) return;

        if (playerDiceObjects.ContainsKey(actorNumber))
        {
            DiceObject d = playerDiceObjects[actorNumber];

            if (!d.isRolling && d.GetResultNumber() == -1)
            {
                d.Roll();

                // ★ 修正 1: 入力待ち状態を解除
                waitingForRoll = false;

                // ★ 修正 2: ダイスオブジェクトに結果報告を要求
                // Rollが完了したら、DiceObjectがDiceGameManagerに結果を通知するように設定
                StartCoroutine(d.WaitForRollCompletionAndReport(actorNumber)); // 新しいコルーチンを呼び出す
            }
        }
    }

    

    private void ForceRollAll()
    {
        waitingForRoll = false;

        foreach (var kvp in playerDiceObjects)
        {
            if (!kvp.Value.isRolling && kvp.Value.GetResultNumber() == -1)
            {
                kvp.Value.Roll();
            }
        }
    }

    /// <summary>
    /// 現在のターンがローカルクライアントのターンであるか判定します。
    /// </summary>
    private bool IsMyTurn()
    {
        if (activePlayers.Count == 0) return false;

        // 現在のターンプレイヤーIDとローカルプレイヤーIDを比較
        // currentTurnPlayerID は現在ターンを持っているプレイヤーのID（myNumber または ActorNumber）である必要があります。
        // 例:
        int localPlayerID = PhotonNetwork.LocalPlayer.ActorNumber;
        int currentActivePlayerID = GameManager.instance.IsOnline()
            ? activePlayers[currentActivePlayerIndex].GetComponent<PhotonView>().OwnerActorNr
            : activePlayers[currentActivePlayerIndex].myNumber;

        return localPlayerID == currentActivePlayerID;
    }

    #endregion

    #region 結果処理

    public void ReportDiceResult(int actorNumber, int number)
    {
        // BABAダイス(-999)のReportは乱数生成に切り替えたため、このコードパスは通らない想定です。
        if (actorNumber == -999)
        {
            Debug.LogWarning("BABA Dice is reporting a physical result, but it should be using random generation now.");
        }
        else
        {
            // プレイヤーダイスの場合
            if (!currentTurnResults.ContainsKey(actorNumber))
            {
                currentTurnResults.Add(actorNumber, number);
            }
            // UI表示同期
            int playerIndex = activePlayers.FindIndex(p =>
                (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == actorNumber);

            if (playerIndex != -1)
            {
                if (GameManager.instance.IsOnline())
                    photonView.RPC(nameof(ShowDiceUI), RpcTarget.All, playerIndex, number);
                else
                    ShowDiceUI(playerIndex, number);
            }
        }

        CheckAllResults();
    }

    [PunRPC]
    private void ShowDiceUI(int index, int number)
    {
        Sprite sprite = (number >= 1 && number <= 6) ? diceSprites[number - 1] : null;

        if (index == -999)
        {
            if (babaResultImage)
            {
                babaResultImage.sprite = sprite;
                babaResultImage.gameObject.SetActive(true);
            }
        }
        else
        {
            if (index >= 0 && index < playerResultImages.Length)
            {
                playerResultImages[index].sprite = sprite;
                playerResultImages[index].gameObject.SetActive(true);
            }
        }
    }

    private void CheckAllResults()
    {
        if (!CheckAuthority() || currentGameState != GameState.Rolling) return;

        // BABAが決まっていて、かつ生き残っている全プレイヤーの結果が出ているか
        int survivors = 0;
        int reportedSurvivors = 0;

        foreach (var p in activePlayers)
        {
            survivors++;
            int id = GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber;
            if (currentTurnResults.ContainsKey(id)) reportedSurvivors++;
        }

        // BABAの結果（currentBabaNumber）が確定済みであること
        if (currentBabaNumber != -1 && reportedSurvivors >= survivors)
        {
            StartCoroutine(ProcessTurnResult());
        }
    }

    private IEnumerator ProcessTurnResult()
    {
        currentGameState = GameState.ProcessingResult;
        yield return new WaitForSeconds(2.0f);

        List<int> droppedPlayersIds = new List<int>();

        // 現在のターンで脱落していない、生き残っているプレイヤーのリスト
        List<PlayerInfomation> survivorsThisTurn = new List<PlayerInfomation>(activePlayers);

        // --- 1. スコア加算と脱落プレイヤーの識別 ---
        foreach (var player in survivorsThisTurn) // activePlayersのコピーに対して処理
        {
            int id = GameManager.instance.IsOnline() ? player.GetComponent<PhotonView>().OwnerActorNr : player.myNumber;

            if (currentTurnResults.TryGetValue(id, out int roll))
            {
                if (roll == currentBabaNumber)
                {
                    // **脱落！**
                    droppedPlayersIds.Add(id);
                    Debug.Log($"Player {id} Dropped! (BABA: {currentBabaNumber})");
                }
                else
                {
                    // **スコア加算**
                    int newScore = player.GetPoint() + roll; // GetPoint()がCurrentScoreを返す前提

                    // ここで player.SetPoint(newScore) を呼ぶのではなく、
                    // RPCでスコア同期を行う方が安全です。
                    if (GameManager.instance.IsOnline())
                    {
                        photonView.RPC(nameof(SyncPlayerScoreAndUI), RpcTarget.All, id, newScore);
                    }
                    else
                    {
                        // オフラインでは直接反映とUI更新
                        player.SetPoint(newScore);
                        UpdateScoreUI(id, newScore);
                    }
                }
            }
        }

        // --- 2. 脱落プレイヤーのリストからの削除と順位確定 ---

        // 現在の低い順位（4位、3位、...）を取得するために、残りの空き順位をカウント
        int nextLowestRank = 4; // maxPlayersを動的に取得するのが理想的

        for (int i = activePlayers.Count - 1; i >= 0; i--)
        {
            int id = GameManager.instance.IsOnline() ? activePlayers[i].GetComponent<PhotonView>().OwnerActorNr : activePlayers[i].myNumber;

            if (droppedPlayersIds.Contains(id))
            {
                // 脱落処理と順位確定
                PlayerInfomation droppedPlayer = activePlayers[i];

                // 順位を確定し、全員に同期（オンライン時）
                if (GameManager.instance.IsOnline())
                {
                    photonView.RPC(nameof(SyncPlayerRank), RpcTarget.All, id, nextLowestRank);
                }
                else
                {
                    // オフライン時の順位設定メソッド（PlayerInfomationに実装されている必要があります）
                    droppedPlayer.SetRank(nextLowestRank);
                }

                activePlayers.RemoveAt(i); // アクティブリストから削除
                nextLowestRank--;          // 次の脱落者は一つ上の順位
            }
        }

        activePlayerCount = activePlayers.Count;

        // 処理後に状態をRollingに戻して次のターンを開始
        currentGameState = GameState.Rolling;
        StartTurn();
    }

    /// <summary>
    /// プレイヤーのスコアを同期し、UIを更新するRPC (変更)
    /// </summary>
    [PunRPC]
    public void SyncPlayerScoreAndUI(int targetId, int score)
    {
        // スコアの更新
        PlayerInfomation targetPlayer = activePlayers.Find(p =>
            (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == targetId);

        if (targetPlayer != null)
        {
            targetPlayer.SetPoint(score); // SetPointはCurrentScoreを更新する

            // UIの更新
            UpdateScoreUI(targetId, score);
        }
    }

    /// <summary>
    /// プレイヤーの確定順位を同期するRPC
    /// </summary>
    [PunRPC]
    public void SyncPlayerRank(int targetId, int rank)
    {
        PlayerInfomation targetPlayer = FindPlayerInfo(targetId);
        if (targetPlayer != null)
        {
            targetPlayer.SetRank(rank); // PlayerInfomationに SetFinalRank メソッドが必要です

            // 順位確定時の特別なUI更新（例：脱落表示など）
            Debug.Log($"Player {targetId} final rank set to {rank}");
        }
    }

    /// <summary>
    /// スコアUIを更新する
    /// </summary>
    private void UpdateScoreUI(int actorNumber, int score)
    {
        // UI配列のインデックスを決定
        int playerIndex = FindPlayerIndex(actorNumber);

        if (playerIndex != -1 && playerIndex < playerScoreTexts.Length)
        {
            if (playerScoreTexts[playerIndex] != null)
            {
                playerScoreTexts[playerIndex].text = $"{score}";
            }
        }
    }

    /// <summary>
    /// ActorNumberまたはmyNumberからプレイヤー配列のインデックスを取得するヘルパー
    /// </summary>
    private int FindPlayerIndex(int actorNumber)
    {
        // activePlayersリストは常に myNumber または ActorNumber 順にソートされていることが望ましい
        for (int i = 0; i < activePlayers.Count; i++)
        {
            int id = GameManager.instance.IsOnline() ? activePlayers[i].GetComponent<PhotonView>().OwnerActorNr : activePlayers[i].myNumber;
            if (id == actorNumber) return i;
        }

        // もしアクティブリストから削除されたプレイヤーの場合、全プレイヤー情報から探す
        PlayerInfomation targetPlayer = FindObjectsOfType<PlayerInfomation>(true)
            .Where(p => (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == actorNumber)
            .FirstOrDefault();

        if (targetPlayer != null)
        {
            // 全プレイヤーリスト（activePlayersのベースとなったもの）からインデックスを取得
            List<PlayerInfomation> allPlayers = FindObjectsOfType<PlayerInfomation>().ToList();
            allPlayers.Sort((a, b) => a.myNumber.CompareTo(b.myNumber));
            return allPlayers.IndexOf(targetPlayer);
        }

        return -1;
    }

    // 共通でプレイヤー情報を見つけるヘルパー関数
    public PlayerInfomation FindPlayerInfo(int targetId)
    {
        return FindObjectsOfType<PlayerInfomation>(true).ToList().Find(p =>
            (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == targetId);
    }

    private void EndGame()
    {
        if (infoText) infoText.text = "GAME FINISHED! Calculating Final Ranks...";
        currentGameState = GameState.Finished;

        // 既にマスタークライアント（またはオフラインホスト）でのみ実行されているはず
        if (CheckAuthority())
        {
            CalculateFinalRanks();
        }
    }

    private void CalculateFinalRanks()
    {
        // 1. 生き残っているプレイヤーをスコア降順でソート
        // (同点の場合は、後からそのスコアになった順 => myNumber/ActorNumberの小さい順で暫定的に決定)
        activePlayers.Sort((a, b) =>
        {
            int scoreComparison = b.GetPoint().CompareTo(a.GetPoint());
            if (scoreComparison != 0) return scoreComparison; // スコアが違う場合はスコアで比較

            // スコアが同じ場合は、PlayerInfomationの識別番号(myNumber/ActorNumber)で比較
            int aId = GameManager.instance.IsOnline() ? a.GetComponent<PhotonView>().OwnerActorNr : a.myNumber;
            int bId = GameManager.instance.IsOnline() ? b.GetComponent<PhotonView>().OwnerActorNr : b.myNumber;
            return aId.CompareTo(bId); // IDが小さいプレイヤーを上位と見なす
        });

        // 2. 確定していない最高順位を取得 (脱落者によってスキップされている可能性がある)
        int highestRankTaken = 0;

        // 全プレイヤー情報から、既に確定している最高順位を探す
        foreach (var player in FindObjectsOfType<PlayerInfomation>())
        {
            // PlayerInfomationに GetFinalRank メソッドが必要です
            int rank = player.GetRank();
            if (rank != 0) // 0は未確定順位と仮定
            {
                if (highestRankTaken == 0 || rank < highestRankTaken)
                {
                    highestRankTaken = rank;
                }
            }
        }

        // 生き残ったプレイヤーの順位を、未確定の最高の順位から順に割り当てる
        int currentRank = (highestRankTaken > 1) ? 1 : 1; // 常に1位から始める

        // 既に確定した順位を避ける
        List<int> usedRanks = FindObjectsOfType<PlayerInfomation>().Select(p => p.GetRank()).ToList();

        foreach (var player in activePlayers)
        {
            while (usedRanks.Contains(currentRank))
            {
                currentRank++;
            }

            // 最終順位を確定し、全員に同期
            if (GameManager.instance.IsOnline())
            {
                int id = GameManager.instance.IsOnline() ? player.GetComponent<PhotonView>().OwnerActorNr : player.myNumber;
                photonView.RPC(nameof(SyncPlayerRank), RpcTarget.All, id, currentRank);
            }
            else
            {
                player.SetRank(currentRank);
            }

            currentRank++;
        }

        // 最終結果表示ロジックへ
        DisplayFinalResults();
    }

    /// <summary>
    /// 最終的な順位とスコアをUIに表示する
    /// </summary>
    private void DisplayFinalResults()
    {
        // すべてのプレイヤー情報を取得（脱落者を含む）
        List<PlayerInfomation> allPlayers = FindObjectsOfType<PlayerInfomation>(true).ToList();

        // 確定順位（GetFinalRank()）に基づいてソート
        allPlayers.Sort((a, b) =>
        {
            // 順位が未確定(0)の場合は最後に、確定済みの順位(1, 2, 3...)でソート
            int rankA = a.GetRank();
            int rankB = b.GetRank();

            if (rankA == 0 && rankB == 0) return 0; // 両方未確定なら順序変更なし
            if (rankA == 0) return 1;              // Aが未確定ならBより後
            if (rankB == 0) return -1;             // Bが未確定ならAより後

            return rankA.CompareTo(rankB); // 確定順位が若い順にソート
        });

        // 順位表示用の文字列を作成
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== FINAL RESULTS ===");

        foreach (var player in allPlayers)
        {
            // PlayerInfomationに GetFinalRank()とGetPoint() が実装されている前提
            sb.AppendLine($"{player.GetRank()}位: Player {player.myNumber} - Score: {player.GetPoint()}");
        }

        // infoText（または専用の最終結果UI）に表示
        if (infoText != null)
        {
            infoText.text = sb.ToString();
        }
    }

    // Photonのコールバックで、プレイヤーが退出したときにアクティブプレイヤーリストから削除（同期を容易にするため、マスターのみ実行）
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (CheckAuthority())
        {
            // 脱落処理ロジックの簡略化のため、ここでは activePlayers からは削除せず、
            // ターン開始時の CheckAuthority() のみで人数をチェックするのが最も安全です。
            // ただし、オンラインの activePlayers はマスタークライアントが管理するリストと一致している必要があります。
            // 複雑な状態管理を避けるため、ここでは特別な処理を行わず、そのまま進行させます。
            // 必要であれば、人数が減ったことによるゲーム終了判定を OnPlayerLeftRoom で行うことも可能です。
            if (currentGameState == GameState.Rolling && PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            {
                EndGame();
            }
        }
    }

    #endregion
}
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用する場合
using UnityEngine.InputSystem;
using System.Linq;

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
    [SerializeField] public Transform[] spawnPoints; // プレイヤーごとのダイス生成位置 (Size=4)
    [SerializeField] private Material[] playerMaterials; // プレイヤー識別用マテリアル (Size=4)
    [SerializeField] private Material babaMaterial; // BABAダイス用マテリアル (UI表示用として残す)

    [Header("UI")]
    [SerializeField] private Image[] playerResultImages; // プレイヤーの出目表示用UI (Size=4)
    [SerializeField] private Image babaResultImage; // BABAダイスの出目表示用UI
    [SerializeField] private Sprite[] diceSprites; // 1~6のサイコロ画像
    [SerializeField] private TextMeshProUGUI infoText; // ゲーム状態表示テキスト
    [SerializeField] private TextMeshProUGUI timerText; // タイマー表示
    [SerializeField] private TextMeshProUGUI[] playerScoreTexts; 

    // 内部変数
    private int currentTurn = 0;
    private int activePlayerCount = 0;
    private float currentTimer = 0f;
    private bool waitingForRoll = false;

    // キー: ActorNumber(オフライン時はPlayerIndex), 値: DiceObject
    public Dictionary<int, DiceObject> playerDiceObjects = new Dictionary<int, DiceObject>();

    // キー: ActorNumber, 値: 今回の出目
    private Dictionary<int, int> currentTurnResults = new Dictionary<int, int>();

    // BABAの数字
    private int currentBabaNumber = -1;

    // プレイヤー情報リスト（脱落していない人）
    public List<PlayerInfomation> activePlayers = new List<PlayerInfomation>();

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

        if (!GameManager.instance.IsOnline())
        {
            // 既存の activePlayers リストをクリア
            activePlayers.Clear();

            // ダミーの PlayerInfomation を必要な数だけ生成・設定
            for (int i = 0; i < maxPlayers; i++)
            {
                // ここで PlayerInfomation のインスタンスを生成し、activePlayers に追加
                // 例: シーン上のプレハブから生成、または新しいGameObjectにコンポーネントを追加

                GameObject playerGO = new GameObject($"Player {i + 1} (Offline)");
                PlayerInfomation pi = playerGO.AddComponent<PlayerInfomation>();
                pi.myNumber = i + 1; // 1から順にIDを設定
                                     // オフラインなので Destroy を確実にするためにシーンを汚染しないように親オブジェクトを設定するなど

                activePlayers.Add(pi);
            }
        }

        if (GameManager.instance.IsOnline())
        {
            // マスタークライアントのみがダイスを生成
            if (PhotonNetwork.IsMasterClient)
            {
                
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

        if (GameManager.instance.IsOnline())
        {
            SpawnLocalDiceOnline();
        }
        else
        {
            SpawnLocalDiceOffline();
        }

        Debug.Log("Game Initialization Complete. Entering Lobby State.");
    }

    /// <summary>
    /// オンラインモードでローカルプレイヤーのダイスを生成し初期設定する。
    /// </summary>
    public void SpawnLocalDiceOnline()
    {
        if (!GameManager.instance.IsOnline() || PhotonNetwork.LocalPlayer == null) return;

        int myActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        // 自分の PlayerInfomation を見つける
        PlayerInfomation p = activePlayers.Find(ap => ap.GetComponent<PhotonView>().OwnerActorNr == myActorNum);

        if (p == null)
        {
            Debug.LogError($"PlayerInfomation not found for ActorNumber: {myActorNum}");
            return;
        }

        int playerIndex = activePlayers.IndexOf(p);

        // スポーン位置・マテリアルの決定
        Vector3 pos = spawnPoints[playerIndex % spawnPoints.Length].position;
        Quaternion rot = spawnPoints[playerIndex % spawnPoints.Length].rotation;
        int materialIndex = playerIndex % playerMaterials.Length;

        object[] data = new object[] { myActorNum, materialIndex };

        // 各クライアントが自身のダイスを生成する (オーナーシップは自動的に自分になる)
        GameObject diceObj = PhotonNetwork.Instantiate(dicePrefab.name, pos, rot, 0, data);

        // 生成されたダイスに初期情報を設定
        DiceObject diceScript = diceObj.GetComponent<DiceObject>();

        // SetupDiceRPC の内容を直接実行 (自分自身で設定するため RPC は不要)
        Material mat = playerMaterials[materialIndex];

        diceScript.InitialPosition = pos;
        diceScript.InitialRotation = rot;

        // ローカルで即座に初期化
        diceScript.Initialize(myActorNum, mat, materialIndex);

        if (!playerDiceObjects.ContainsKey(myActorNum))
        {
            playerDiceObjects.Add(myActorNum, diceScript);
        }

        Debug.Log($"Local Dice Spawned and Initialized for Actor: {myActorNum}");
    }

    /// <summary>
    /// オフラインモードでローカルプレイヤー（全員）のダイスを生成し初期設定する。
    /// </summary>
    public void SpawnLocalDiceOffline()
    {
        if (GameManager.instance.IsOnline()) return; // オンライン時は実行しない

        // 既存のダイスをクリア (再開時などのために)
        foreach (var kvp in playerDiceObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        playerDiceObjects.Clear();


        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerInfomation p = activePlayers[i];
            int myNumber = p.myNumber; // オフライン時のプレイヤー識別番号
            int playerIndex = i;

            // スポーン位置・マテリアルの決定
            Vector3 pos = spawnPoints[playerIndex % spawnPoints.Length].position;
            Quaternion rot = spawnPoints[playerIndex % spawnPoints.Length].rotation;
            int materialIndex = playerIndex % playerMaterials.Length;

            // ローカルでの生成 (Instantiate)
            GameObject diceObj = Instantiate(dicePrefab, pos, rot);

            // 生成されたダイスに初期情報を設定
            DiceObject diceScript = diceObj.GetComponent<DiceObject>();

            Material mat = playerMaterials[materialIndex];

            diceScript.InitialPosition = pos;
            diceScript.InitialRotation = rot;

            // 初期化を実行 (オフラインでは ViewID や ActorNumber は不要だが、PlayerIDとして myNumber を渡す)
            // DiceObject.Initialize メソッドが引数を受け取れるように調整が必要です。
            diceScript.Initialize(myNumber, mat, materialIndex);

            if (!playerDiceObjects.ContainsKey(myNumber))
            {
                playerDiceObjects.Add(myNumber, diceScript);
            }

            Debug.Log($"Offline Dice Spawned and Initialized for Player Number: {myNumber}");
        }
    }

    private bool CheckAuthority()
    {
        return !GameManager.instance.IsOnline() || PhotonNetwork.IsMasterClient;
    }

    #region 生成処理

    private void SpawnDiceOnline()
    {
        // このメソッドはマスタークライアントのみが呼び出すが、
        // ここではダイス生成は行わず、全てのプレイヤーに生成を促すRPCを呼び出す役割に変更します。
        // ※ 現在のコードでは、このメソッドが呼ばれるのは InitializeGameFlow() 内で、
        //    activePlayers の情報がまだ同期されていないため、この RPC は使用しません。

        // 従来のマスタークライアントによる一括生成ロジックを削除し、
        // 代わりに各クライアントの Start() または OnJoinedRoom() で実行するように変更します。

        // --- 変更点: ここでは何も行わないか、または削除する ---
        // 後の手順で、InitializeGameFlow から SpawnDiceOnline() の呼び出しを削除します。
    }

    private void SpawnDiceOffline()
    {
        // 1. 既存のダイスをクリア（リセット時などに備えて）
        // オフラインなので、辞書に登録されているオブジェクトを破棄します。
        foreach (var kvp in playerDiceObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        playerDiceObjects.Clear();

        // 2. プレハブが割り当てられているかチェック
        if (dicePrefab == null)
        {
            Debug.LogError("Dice Prefab is NOT assigned in the Inspector! Cannot spawn dice offline.");
            return;
        }


        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerInfomation p = activePlayers[i];
            int id = p.myNumber; // オフライン時のプレイヤー識別番号
            int playerIndex = i;

            // スポーン位置・マテリアルの決定
            Vector3 pos = spawnPoints[playerIndex % spawnPoints.Length].position;
            Quaternion rot = spawnPoints[playerIndex % spawnPoints.Length].rotation;
            int materialIndex = playerIndex % playerMaterials.Length;

            // 👇 ローカルでの生成 (Instantiate)
            // Scene上のオブジェクトではなく、dicePrefabから新しく生成します。
            GameObject diceObj = Instantiate(dicePrefab, pos, rot);

            // 生成されたダイスに初期情報を設定
            DiceObject diceScript = diceObj.GetComponent<DiceObject>();

            // DiceObjectコンポーネントがない場合は追加
            if (diceScript == null)
            {
                diceScript = diceObj.AddComponent<DiceObject>();
            }

            Material mat = playerMaterials[materialIndex];

            diceScript.InitialPosition = pos;
            diceScript.InitialRotation = rot;

            // 初期化を実行
            diceScript.Initialize(id, mat, materialIndex);

            if (!playerDiceObjects.ContainsKey(id))
            {
                playerDiceObjects.Add(id, diceScript);
            }

            Debug.Log($"Offline Dice Spawned and Initialized for Player Number: {id}");
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

    public Material GetPlayerMaterial(int index)
    {
        if (index >= 0 && index < playerMaterials.Length) return playerMaterials[index];
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
        else if (currentGameState == GameState.Rolling)
        {
            // ダイスを振る時間制限の処理
            if (waitingForRoll)
            {
                currentTimer -= Time.deltaTime;
                if (timerText) timerText.text = currentTimer.ToString("F1");

                // 時間切れで自動ロール
                if (CheckAuthority() && currentTimer <= 0)
                {
                    ForceRollAll();
                }
            }
        }
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

        // 2. 自動開始チェック (最大人数到達) - 時間切れによる自動開始は任意開始のため削除
        // MaxPlayers に達したら強制開始するロジックは残します。
        bool autoStartCondition = currentPlayers >= maxPlayers;

        // 3. 手動開始チェック (Enter/Spaceキー)
        bool manualStartInput = Keyboard.current != null &&
                                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

        // 4. 開始条件判定:
        // **最低開始人数は無視し、プレイヤーが1人以上いる場合** に、
        // (最大人数到達 OR ホストによる手動入力) で開始を許可します。
        if (currentPlayers >= 1 && (autoStartCondition || manualStartInput))
        {
            // ただし、ゲーム開始時のチェック (StartTurn) で人数不足により即終了する可能性はあるため、
            // ロビーでは最低開始人数に満たない場合の警告表示を UI に出す程度にとどめます。

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
        // タイマー表示は、ロビー制限時間を撤廃するなら削除しても良いですが、今回はそのままにしておきます。
        string timerDisplay = "";

        statusText = $"参加者: {currentPlayers}人 / 最大: {maxPlayers}人\n";

        if (currentPlayers < minPlayersToStart)
        {
            statusText += $"⚠️ 推奨: 最低 {minPlayersToStart} 人\n";
        }

        if (CheckAuthority())
        {
            if (currentPlayers >= 1)
            {
                statusText += $"[Enter/Space]キーでゲームを開始できます。";
            }
            else
            {
                statusText += "プレイヤーを待機中...";
            }
        }
        else
        {
            statusText += "ホストのゲーム開始操作を待っています...";
        }

        if (infoText) infoText.text = statusText;
        if (timerText) timerText.text = timerDisplay; // タイマー表示は空にするか、ロビータイマーの機能自体を削除しても良いです。
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
        // activePlayersリストを更新する必要がある場合は、ここで更新ロジックを追加する必要がありますが、
        // 現在のコードでは StartCoroutine(InitializeGameFlow()) でしか初期化されていません。
        // 途中参加プレイヤーのダイス生成・activePlayersへの追加ロジックは、このスコープでは複雑になるため、
        // 一旦「途中参加はロビーでのみ可能で、ゲームが始まると途中参加は不可」という前提で進めます。

        if (CheckAuthority() && currentGameState == GameState.Lobby)
        {
            // 最大人数に達した場合のみ、自動でゲームを開始します
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
            {
                ManualStartGame();
            }
        }
    }

    private void StartTurn()
    {
        if (!CheckAuthority() || currentGameState != GameState.Rolling) return;

        if (currentTurn == 0) // 最初のターン開始前
        {
            int currentPlayers = GameManager.instance.IsOnline() ? PhotonNetwork.CurrentRoom.PlayerCount : activePlayers.Count;
            if (currentPlayers < minPlayersToStart)
            {
                // プレイヤー数が不足しているためゲーム終了を通知
                if (infoText) infoText.text = "Error: Not enough players to start the game (Min: " + minPlayersToStart + ")";
                EndGame();
                return;
            }
        }

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
        if (currentGameState != GameState.Rolling || !waitingForRoll) return;

        if (GameManager.instance.IsOnline())
        {
            int myID = PhotonNetwork.LocalPlayer.ActorNumber;
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
        if (!CheckAuthority()) return; // マスタークライアントのみ実行

        if (playerDiceObjects.ContainsKey(actorNumber))
        {
            DiceObject d = playerDiceObjects[actorNumber];

            if (!d.isRolling && d.GetResultNumber() == -1)
            {
                // マスタークライアントがダイスオブジェクトの所有権を要求
                // これにより、マスタークライアントが物理演算を実行できるようになる
                d.GetComponent<PhotonView>().RequestOwnership();

                // 権限の取得は即座ではないため、少し待ってから実行するか、
                // DiceObject内でOnOwnershipTransferedコールバックを使用するのが理想ですが、
                // シンプルなゲームプレイのためにここでは直後に実行を試みます。

                d.Roll();
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
        // 状態をProcessingResultに一時的に変更して、誤ったロールを防止
        currentGameState = GameState.ProcessingResult;

        yield return new WaitForSeconds(2.0f);

        List<int> droppedPlayers = new List<int>();

        // 👇 脱落していないプレイヤーのIDとスコアを一時的に保持
        Dictionary<int, int> turnScoreUpdates = new Dictionary<int, int>();

        foreach (var player in activePlayers)
        {
            int id = GameManager.instance.IsOnline() ? player.GetComponent<PhotonView>().OwnerActorNr : player.myNumber;

            if (currentTurnResults.TryGetValue(id, out int roll))
            {
                if (roll == currentBabaNumber)
                {
                    // 脱落！
                    droppedPlayers.Add(id);
                    // 脱落したプレイヤーはスコア加算なし
                    Debug.Log($"Player {id} Dropped! (BABA: {currentBabaNumber})");
                }
                else
                {
                    // セーフ！点数加算
                    int currentPoint = player.GetPoint();
                    int newScore = currentPoint + roll;

                    // スコアを PlayerInfomation に一時保存し、ターン終了時に全員に同期
                    player.SetPoint(newScore);
                    turnScoreUpdates.Add(id, newScore);
                }
            }
        }

        // ターン終了後のスコア変更を全クライアントに同期
        if (CheckAuthority())
        {
            foreach (var kvp in turnScoreUpdates)
            {
                // オンラインならRPCで、オフラインなら直接反映
                if (GameManager.instance.IsOnline())
                {
                    photonView.RPC(nameof(SyncPlayerScore), RpcTarget.All, kvp.Key, kvp.Value);
                }
                else
                {
                    SyncPlayerScore(kvp.Key, kvp.Value);
                }
            }
        }

        // 脱落プレイヤーを activePlayers リストから削除
        for (int i = activePlayers.Count - 1; i >= 0; i--)
        {
            int id = GameManager.instance.IsOnline() ? activePlayers[i].GetComponent<PhotonView>().OwnerActorNr : activePlayers[i].myNumber;
            if (droppedPlayers.Contains(id))
            {
                activePlayers.RemoveAt(i);
            }
        }

        activePlayerCount = activePlayers.Count;

        // 処理後に状態をRollingに戻して次のターンを開始
        currentGameState = GameState.Rolling;
        StartTurn();
    }

    /// <summary>
    /// プレイヤーのスコアを同期し、UIを更新するRPC
    /// </summary>
    [PunRPC]
    public void SyncPlayerScore(int targetId, int score)
    {
        PlayerInfomation targetPlayer = activePlayers.Find(p =>
            (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == targetId);

        if (targetPlayer != null)
        {
            // PlayerInfomation のスコアを更新
            targetPlayer.SetPoint(score);

            // スコアUIを更新
            int playerIndex = activePlayers.IndexOf(targetPlayer);

            if (playerIndex >= 0 && playerIndex < playerScoreTexts.Length && playerScoreTexts[playerIndex] != null)
            {
                playerScoreTexts[playerIndex].text = score.ToString();
            }
        }
    }

    private void EndGame()
    {
        if (infoText) infoText.text = "GAME FINISHED!";
        currentGameState = GameState.Finished;

        if (!CheckAuthority()) return; // 順位決定はマスタークライアント/オフラインホストのみで行う

        // **順位決定ロジック**

        // 1. 全プレイヤーの情報を取得（脱落者を含む）
        // activePlayers は脱落者を既に除外しているので、FindObjectsOfTypeで全プレイヤーを取得し直す
        PlayerInfomation[] allPlayers = FindObjectsOfType<PlayerInfomation>();

        // 2. プレイヤーをスコアに基づいて並び替える
        // スコア降順 (高いスコアが上)、同点の場合は myNumber 昇順 (後からそのスコアになった順の代用)
        // ※厳密な「後からそのスコアになった順」の実現には、スコア到達時間の記録が必要ですが、ここでは myNumber で代用します。
        var rankedPlayers = new List<PlayerInfomation>(allPlayers);

        // 脱落者は最下位から順位を付ける
        // myNumberはPlayerInfomationの識別番号（オフライン時）、ActorNumber（オンライン時）として使われています。
        // ここでは便宜的に myNumber (PlayerInfomationに付与された番号) を比較に使用します。

        // 厳密な順位付けロジック
        // 1. まず、脱落していないプレイヤーをスコア降順で並び替える
        // 2. 次に、脱落したプレイヤーを（今回は脱落時の低い順位という条件がないため）残りの順位を埋める

        // 実際に残ったプレイヤーのリスト: activePlayers (脱落者以外)
        // 全プレイヤーのリスト: allPlayers

        // 脱落していないプレイヤーをスコアでソート (降順)
        var nonDroppedPlayers = activePlayers.OrderByDescending(p => p.GetPoint()).ToList();

        // 脱落したプレイヤーのリスト (全プレイヤーから残ったプレイヤーを除いたもの)
        var droppedPlayers = allPlayers.Except(activePlayers).ToList();

        // 最終的な順位リストを構築
        List<PlayerInfomation> finalRankings = new List<PlayerInfomation>();
        finalRankings.AddRange(nonDroppedPlayers);

        // 脱落者は残りの順位を埋める (脱落時の順位変動がないため、今回は myNumber 昇順で順位を埋めます)
        droppedPlayers.Sort((a, b) => a.myNumber.CompareTo(b.myNumber)); // 小さい myNumber を優先
        finalRankings.AddRange(droppedPlayers);

        // 順位表示文字列の作成
        string rankingText = "--- FINAL RANKING ---\n";
        for (int i = 0; i < finalRankings.Count; i++)
        {
            string status = activePlayers.Contains(finalRankings[i]) ? " (SAFE)" : " (DROPPED)";
            rankingText += $"{i + 1}位: Player {finalRankings[i].myNumber} - Score: {finalRankings[i].GetPoint()}{status}\n";
        }

        if (infoText) infoText.text += "\n" + rankingText;

        // オンラインの場合は、この結果を全クライアントに同期するRPCを呼び出す
        if (GameManager.instance.IsOnline())
        {
            // シンプルな同期のために文字列で送信
            photonView.RPC(nameof(SyncEndGameResult), RpcTarget.All, rankingText);
        }
    }

    /// <summary>
    /// ゲーム終了時の順位結果を同期し、UIを更新するRPC
    /// </summary>
    [PunRPC]
    private void SyncEndGameResult(string resultText)
    {
        if (infoText)
        {
            // ゲーム終了メッセージに結果を追加
            if (!infoText.text.Contains("FINAL RANKING"))
            {
                infoText.text += "\n" + resultText;
            }
        }
        currentGameState = GameState.Finished;
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
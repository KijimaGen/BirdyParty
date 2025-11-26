using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用する場合
using UnityEngine.InputSystem;

/**
 * @file DiceGameManager.cs
 * @brief ダイスゲーム全体の進行管理。オンライン・オフライン両対応。
 */
public class DiceGameManager : MonoBehaviourPunCallbacks
{
    public static DiceGameManager instance;

    [Header("ゲーム設定")]
    [SerializeField] private int maxTurns = 5;
    [SerializeField] private float timeLimit = 5.0f; // ダイスを振る制限時間

    [Header("参照")]
    [SerializeField] private GameObject dicePrefab; // プレイヤーダイスのPrefab（オフライン時はPlayerInfomation内の既存オブジェクトを優先）
    [SerializeField] private Transform[] spawnPoints; // プレイヤーごとのダイス生成位置 (Size=4)
    // [SerializeField] private Transform babaDiceSpawnPoint; // BABAダイスの生成位置を削除
    [SerializeField] private Material[] playerMaterials; // プレイヤー識別用マテリアル (Size=4)
    [SerializeField] private Material babaMaterial; // BABAダイス用マテリアル (UI表示用として残す)

    [Header("UI")]
    [SerializeField] private Image[] playerResultImages; // プレイヤーの出目表示用UI (Size=4)
    [SerializeField] private Image babaResultImage; // BABAダイスの出目表示用UI
    [SerializeField] private Sprite[] diceSprites; // 1~6のサイコロ画像
    [SerializeField] private TextMeshProUGUI infoText; // ゲーム状態表示テキスト
    [SerializeField] private TextMeshProUGUI timerText; // タイマー表示

    // 内部変数
    private int currentTurn = 0;
    private int activePlayerCount = 0;
    private bool isGameRunning = false;
    private float currentTimer = 0f;
    private bool waitingForRoll = false;

    // キー: ActorNumber(オフライン時はPlayerIndex), 値: DiceObject
    private Dictionary<int, DiceObject> playerDiceObjects = new Dictionary<int, DiceObject>();
    // private DiceObject babaDiceObject; // BABAダイスオブジェクトの参照を削除

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
        // Nullチェックを追加
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
        yield return new WaitForSeconds(1.5f);

        activePlayers.AddRange(FindObjectsOfType<PlayerInfomation>());
        activePlayers.Sort((a, b) => a.myNumber.CompareTo(b.myNumber));

        activePlayerCount = activePlayers.Count;

        if (GameManager.instance.IsOnline() && PhotonNetwork.IsMasterClient)
        {
            SpawnDiceOnline();
        }
        else if (!GameManager.instance.IsOnline())
        {
            SpawnDiceOffline();
            SetupOfflineInputs();
        }

        yield return new WaitForEndOfFrame();

        isInitialized = true;

        if (CheckAuthority() && activePlayerCount > 0)
        {
            StartTurn();
        }
        else if (activePlayerCount <= 0)
        {
            Debug.LogError("PlayerInfomationが見つかりません。ゲームを続行できません。");
            if (infoText) infoText.text = "Error: No Players Found";
        }
    }

    private bool CheckAuthority()
    {
        return !GameManager.instance.IsOnline() || PhotonNetwork.IsMasterClient;
    }

    #region 生成処理

    // BABAダイスの生成処理を完全に削除し、プレイヤーダイスのみを扱うように変更

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

        // BABAダイスの生成ロジックは削除
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

            diceScript.InitialPosition = sp.position;
            diceScript.InitialRotation = sp.rotation;

            diceScript.Initialize(id, playerMaterials[i % playerMaterials.Length], i);

            playerDiceObjects.Add(id, diceScript);
        }

        // BABAダイスのInstantiateロジックを削除
    }

    // オフライン時の入力紐づけ処理 (変更なし)
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
                else
                {
                    Debug.LogWarning($"Player {player.myNumber} does not have 'Roll' action in current map.");
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
        // もし残すとしても、既存のコードでは処理を継続しないようにしておくのが安全です。
        // 今後のためにメソッドは残しますが、BABAは乱数生成に切り替えたため、このコードパスは実行されないはずです。
        PhotonView view = PhotonView.Find(viewID);
        if (view != null)
        {
            DiceObject d = view.GetComponent<DiceObject>();

            d.InitialPosition = pos;
            d.InitialRotation = rot;

            d.Initialize(-999, babaMaterial, -1);
            // babaDiceObject = d; // 参照を保持しない
        }
    }

    // マテリアル取得用ヘルパー (変更なし)
    public Material GetPlayerMaterial(int index)
    {
        if (index >= 0 && index < playerMaterials.Length) return playerMaterials[index];
        return null;
    }

    #endregion

    #region ゲームループ

    private void StartTurn()
    {
        if (!isInitialized) return;

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

        // BABAダイスはマスターが自動で振る (★修正: 乱数生成に切り替え)
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

        // BABAダイスは乱数生成のため、Roll()を呼び出す必要がなくなりました。
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

        // BABAダイスオブジェクトがないため、この処理は不要
        // if (babaDiceObject != null)
        // {
        //     babaDiceObject.ResetDiceState();
        // }
        currentBabaNumber = -1;
    }

    private void Update()
    {
        // 初期化が終わるまで待機
        if (!isInitialized) return;

        // タイマー処理
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

    // 自分のダイスを振る入力（変更なし）
    public void OnRollInput()
    {
        if (!waitingForRoll) return;

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
        if (!waitingForRoll) return;
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

            if (!d.isRolling && d.resultNumber == -1)
            {
                d.Roll();
            }
        }
    }

    private void ForceRollAll()
    {
        waitingForRoll = false;

        foreach (var kvp in playerDiceObjects)
        {
            if (!kvp.Value.isRolling && kvp.Value.resultNumber == -1)
            {
                kvp.Value.Roll();
            }
        }

        // BABAダイスは既に結果が出ているため、ここはプレイヤーダイスのみ
    }

    #endregion

    #region 結果処理

    // DiceObjectから呼ばれる (変更なし)
    public void ReportDiceResult(int actorNumber, int number)
    {
        // BABAダイス(-999)のReportは乱数生成に切り替えたため、このコードパスは通らない想定です。
        if (actorNumber == -999)
        {
            Debug.LogWarning("BABA Dice is reporting a physical result, but it should be using random generation now.");
            // 物理ダイスが残っていても、乱数結果を優先するため処理しない
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
        if (!CheckAuthority()) return;

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

    // ProcessTurnResult, SyncPlayerScore, EndGame (変更なし)
    private IEnumerator ProcessTurnResult()
    {
        yield return new WaitForSeconds(2.0f);

        List<int> droppedPlayers = new List<int>();

        foreach (var player in activePlayers)
        {
            int id = GameManager.instance.IsOnline() ? player.GetComponent<PhotonView>().OwnerActorNr : player.myNumber;

            if (currentTurnResults.TryGetValue(id, out int roll))
            {
                if (roll == currentBabaNumber)
                {
                    // 脱落！
                    droppedPlayers.Add(id);
                    Debug.Log($"Player {id} Dropped! (BABA: {currentBabaNumber})");
                }
                else
                {
                    // セーフ！点数加算
                    int currentPoint = player.GetPoint();
                    int newScore = currentPoint + roll;
                    player.SetPoint(newScore);

                    if (GameManager.instance.IsOnline())
                    {
                        int targetId = GameManager.instance.IsOnline() ? player.GetComponent<PhotonView>().OwnerActorNr : player.myNumber;
                        photonView.RPC(nameof(SyncPlayerScore), RpcTarget.All, targetId, newScore);
                    }
                }
            }
        }

        for (int i = activePlayers.Count - 1; i >= 0; i--)
        {
            int id = GameManager.instance.IsOnline() ? activePlayers[i].GetComponent<PhotonView>().OwnerActorNr : activePlayers[i].myNumber;
            if (droppedPlayers.Contains(id))
            {
                activePlayers.RemoveAt(i);
            }
        }

        activePlayerCount = activePlayers.Count;

        StartTurn();
    }

    [PunRPC]
    public void SyncPlayerScore(int targetId, int score)
    {
        PlayerInfomation targetPlayer = activePlayers.Find(p =>
            (GameManager.instance.IsOnline() ? p.GetComponent<PhotonView>().OwnerActorNr : p.myNumber) == targetId);

        if (targetPlayer != null)
        {
            targetPlayer.SetPoint(score);
        }
    }

    private void EndGame()
    {
        if (infoText) infoText.text = "GAME FINISHED!";
        isGameRunning = false;
        waitingForRoll = false;
    }
    // End ProcessTurnResult, SyncPlayerScore, EndGame

    #endregion
}
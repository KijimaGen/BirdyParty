using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System.Xml.Serialization;

/*
    BABADice全体の進行管理
    マスターが主導権を持つように（オフラインは自身を）
*/
public class DiceGame_GameManager : MonoBehaviourPunCallbacks
{
    [Header("Prefabs & Resources")]
    [SerializeField] private string dicePrefabName = "DicePrefab"; // Resourcesフォルダ内のプレハブ名
    [SerializeField] private Material[] playerMaterials;           // プレイヤーごとのマテリアル(4つ)

    [Header("Game Settings")]
    [SerializeField] private float turnLimitTime = 10.0f; // 1ターンの制限時間
    [SerializeField] private Transform[] spawnPoints; // プレイヤー1~4のダイススポーン位置

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI babaText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI[] scoreTexts; // プレイヤーごとのスコア表示

    // 内部パラメータ
    private int currentTurn = 1;
    private const int MaxTurns = 5;
    private int currentBabaNumber = 0;
    private float currentTimer = 0f;
    private bool isTurnActive = false;

    // プレイヤーデータ管理
    private Dictionary<int, DiceObject> playerDiceMap = new Dictionary<int, DiceObject>();
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private List<int> droppedOutPlayers = new List<int>(); // 脱落したプレイヤー番号

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        // 少し待機して接続安定を待つ
        yield return new WaitForSeconds(0.5f);

        // 初期化
        currentTurn = 1;
        droppedOutPlayers.Clear();
        playerScores.Clear();
        for (int i = 0; i < 4; i++) playerScores[i] = 0;

        UpdateUI();

        // ダイスの生成
        SpawnDice();

        // 少し待ってからゲーム開始
        yield return new WaitForSeconds(1.0f);

        if (IsMaster())
        {
            StartNewTurn();
        }
    }

    /// <summary>
    /// ダイスの生成処理
    /// オンライン：マスターがRoomObjectとして生成
    /// オフライン：ローカルで生成
    /// </summary>
    private void SpawnDice()
    {
        bool isOnline = GameManager.instance.IsOnline();

        // オンラインの場合、マスタークライアントのみが生成を担当
        if (isOnline && !PhotonNetwork.IsMasterClient) return;

        // 最大4人分生成
        int playerCount = isOnline ? PhotonNetwork.CurrentRoom.PlayerCount : 1; // オフライン時はとりあえず1人or4人設定に合わせて調整
        // ※要件に合わせてオフラインでも4つ出すならループを固定
        int loopCount = 4;

        for (int i = 0; i < loopCount; i++)
        {
            GameObject diceObj = null;
            Vector3 pos = spawnPoints[i % spawnPoints.Length].position;

            if (isOnline)
            {
                // Photon経由でRoomObjectとして生成
                diceObj = PhotonNetwork.InstantiateRoomObject(dicePrefabName, pos, Quaternion.identity);
            }
            else
            {
                // ローカル生成
                var prefab = Resources.Load<GameObject>(dicePrefabName);
                diceObj = Instantiate(prefab, pos, Quaternion.identity);
            }

            if (diceObj != null)
            {
                DiceObject diceScript = diceObj.GetComponent<DiceObject>();
                // 全員に初期化情報を送る
                if (isOnline)
                {
                    photonView.RPC(nameof(RPC_InitializeDice), RpcTarget.AllBuffered, diceScript.GetComponent<PhotonView>().ViewID, i);
                }
                else
                {
                    InitializeDiceLocal(diceScript, i);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_InitializeDice(int viewID, int playerNum)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            DiceObject dice = targetView.GetComponent<DiceObject>();
            InitializeDiceLocal(dice, playerNum);
        }
    }

    private void InitializeDiceLocal(DiceObject dice, int playerNum)
    {
        Material mat = (playerNum < playerMaterials.Length) ? playerMaterials[playerNum] : null;
        dice.Initialize(playerNum, mat);

        if (!playerDiceMap.ContainsKey(playerNum))
        {
            playerDiceMap.Add(playerNum, dice);
        }
    }

    // --- ターン管理 ---

    private void StartNewTurn()
    {
        if (currentTurn > MaxTurns || GetActivePlayerCount() <= 1)
        {
            EndGame();
            return;
        }

        // BABA決定 (マスターのみ)
        int newBaba = Random.Range(1, 7); // 1-6

        if (GameManager.instance.IsOnline())
        {
            photonView.RPC(nameof(RPC_SyncTurnStart), RpcTarget.All, currentTurn, newBaba);
        }
        else
        {
            RPC_SyncTurnStart(currentTurn, newBaba);
        }
    }

    [PunRPC]
    private void RPC_SyncTurnStart(int turn, int baba)
    {
        currentTurn = turn;
        currentBabaNumber = baba;
        currentTimer = turnLimitTime;
        isTurnActive = true;

        // UI更新
        infoText.text = $"TURN {currentTurn} START!";
        babaText.text = $"BABA: {currentBabaNumber}";

        // BABA演出などをここで呼ぶ
    }

    private void Update()
    {
        if (!isTurnActive) return;

        // タイマー処理 (表示は全員、判定はマスター)
        currentTimer -= Time.deltaTime;
        timerText.text = $"Time: {currentTimer:F1}";

        // ローカル入力処理 (自分のダイスを振る)
        HandleInput();

        // マスターのみが時間切れなどを監視
        if (IsMaster())
        {
            if (currentTimer <= 0)
            {
                // 時間切れなら強制ロール
                ForceRollAll();
            }

            // 全員が振り終わり、かつダイスが止まったかチェック
            CheckTurnResultCondition();
        }
    }

    /// <summary>
    /// 入力処理
    /// Input Systemを使って入力を検知し、自分のダイスを振る
    /// </summary>
    private void HandleInput()
    {
        // すでに脱落していたら操作不可
        int myNumber = GetMyPlayerNumber();
        if (droppedOutPlayers.Contains(myNumber)) return;

        // InputSystemの判定 (例: SpaceキーやGamepad Southボタン)
        bool inputTriggered = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) inputTriggered = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) inputTriggered = true;

        if (inputTriggered)
        {
            // ダイスを振るリクエストを送る
            if (GameManager.instance.IsOnline())
            {
                photonView.RPC(nameof(RPC_RequestRoll), RpcTarget.MasterClient, myNumber);
            }
            else
            {
                // オフラインなら直接振る（デバッグ用に全ダイス振る、あるいは1Pのみなど仕様による）
                // ここではオフラインでも「自分の番号」のダイスを振る
                RollDiceLogic(myNumber);
            }
        }
    }

    [PunRPC]
    private void RPC_RequestRoll(int playerNumber)
    {
        // マスターが受け取って物理力を加える
        RollDiceLogic(playerNumber);
    }

    private void RollDiceLogic(int playerNumber)
    {
        if (playerDiceMap.ContainsKey(playerNumber))
        {
            playerDiceMap[playerNumber].RollDice();
        }
    }

    private void ForceRollAll()
    {
        foreach (var kvp in playerDiceMap)
        {
            // まだ脱落していないプレイヤーのみ
            if (!droppedOutPlayers.Contains(kvp.Key))
            {
                kvp.Value.RollDice();
            }
        }
    }

    /// <summary>
    /// ターンの結果判定が可能かチェック（全員のダイスが止まっているか）
    /// </summary>
    private void CheckTurnResultCondition()
    {
        // 制限時間内でも全員振り終わって静止したら判定へ
        bool allStopped = true;
        foreach (var kvp in playerDiceMap)
        {
            if (droppedOutPlayers.Contains(kvp.Key)) continue; // 脱落者は無視

            // まだ静止していないダイスがあれば待機
            if (!kvp.Value.IsSleeping())
            {
                allStopped = false;
                break;
            }
        }

        // 時間切れ または 全員静止
        if (currentTimer <= 0 || allStopped)
        {
            // 判定処理へ（重複実行防止のためフラグ管理が必要）
            StartCoroutine(ProcessTurnResult());
        }
    }

    private bool isProcessingResult = false;
    private IEnumerator ProcessTurnResult()
    {
        if (isProcessingResult) yield break;
        isProcessingResult = true;
        isTurnActive = false; // 入力受付終了

        // 少し物理的な落ち着きを待つ
        yield return new WaitForSeconds(1.5f);

        List<int> eliminatedThisTurn = new List<int>();
        Dictionary<int, int> roundScores = new Dictionary<int, int>();

        // 結果集計
        foreach (var kvp in playerDiceMap)
        {
            int pNum = kvp.Key;
            if (droppedOutPlayers.Contains(pNum)) continue;

            int roll = kvp.Value.GetResult();

            if (roll == currentBabaNumber)
            {
                eliminatedThisTurn.Add(pNum);
                roundScores[pNum] = 0; // 脱落者は0点加算（あるいは没収などのルールによる）
            }
            else
            {
                roundScores[pNum] = roll;
            }
        }

        // 結果を共有
        if (GameManager.instance.IsOnline())
        {
            // DictionaryはRPCで送れないため配列に変換して送るなどの工夫が必要
            // ここでは簡易的に送信
            int[] pNums = roundScores.Keys.ToArray();
            int[] pScores = roundScores.Values.ToArray();
            int[] elims = eliminatedThisTurn.ToArray();

            photonView.RPC(nameof(RPC_TurnResult), RpcTarget.All, pNums, pScores, elims);
        }
        else
        {
            RPC_TurnResult(roundScores.Keys.ToArray(), roundScores.Values.ToArray(), eliminatedThisTurn.ToArray());
        }

        yield return new WaitForSeconds(3.0f); // 結果表示待機

        isProcessingResult = false;

        // 次のターンへ
        if (IsMaster())
        {
            currentTurn++;
            StartNewTurn();
        }
    }

    [PunRPC]
    private void RPC_TurnResult(int[] playerNums, int[] scores, int[] eliminated)
    {
        string resultLog = "Result:\n";

        for (int i = 0; i < playerNums.Length; i++)
        {
            int pNum = playerNums[i];
            int score = scores[i];

            // スコア加算
            if (!droppedOutPlayers.Contains(pNum))
            {
                playerScores[pNum] += score;
            }
        }

        // 脱落者処理
        foreach (int eNum in eliminated)
        {
            if (!droppedOutPlayers.Contains(eNum))
            {
                droppedOutPlayers.Add(eNum);
                resultLog += $"Player {eNum} is OUT (Rolled {currentBabaNumber})!\n";
                // 脱落演出（ダイスを消す、爆発させる等）
                if (playerDiceMap.ContainsKey(eNum))
                {
                    playerDiceMap[eNum].gameObject.SetActive(false);
                }
            }
        }

        UpdateUI();
        infoText.text = resultLog;
    }

    private void EndGame()
    {
        // 最終順位計算
        var sortedRanking = playerScores.OrderByDescending(x => x.Value).ToList();
        string rankText = "GAME OVER\nRanking:\n";
        for (int i = 0; i < sortedRanking.Count; i++)
        {
            rankText += $"{i + 1}. Player {sortedRanking[i].Key} : {sortedRanking[i].Value}pts\n";

            // PlayerInformationに順位を書き込む
            // PlayerInfomation info = PlayerManager.instance.GetPlayer(sortedRanking[i].Key);
            // if(info) info.SetRank(i+1);
        }

        infoText.text = rankText;

        // 終了後の処理（タイトルへ戻るボタン表示など）
        // 今回は簡易的にログ出しのみ
    }

    // --- ユーティリティ ---

    private void UpdateUI()
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < scoreTexts.Length)
            {
                string status = droppedOutPlayers.Contains(i) ? "DEAD" : "ALIVE";
                scoreTexts[i].text = $"P{i}: {playerScores[i]} ({status})";
            }
        }
    }

    private bool IsMaster()
    {
        return !GameManager.instance.IsOnline() || PhotonNetwork.IsMasterClient;
    }

    private int GetMyPlayerNumber()
    {
        if (!GameManager.instance.IsOnline()) return 0; // オフライン時はP1扱い

        // PlayerInfomationから取得するのが正攻法
        // ここでは既存のPlayerInfomationを探して自分の番号を返す処理を想定
        var myInfo = FindObjectsOfType<PlayerInfomation>().FirstOrDefault(p => p.GetComponent<PhotonView>().IsMine);
        return myInfo != null ? myInfo.myNumber : -1;
    }

    private int GetActivePlayerCount()
    {
        return 4 - droppedOutPlayers.Count; // 最大4人固定の場合
    }
}
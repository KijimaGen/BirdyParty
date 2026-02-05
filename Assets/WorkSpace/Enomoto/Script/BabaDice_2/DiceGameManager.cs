using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// BABA Dice Game main manager.
/// - Master controls turn/timer/baba/score/elimination and broadcasts results.
/// - Clients simulate local physics dice and report only the final face value to Master.
/// - Offline mode runs everything locally.
/// 
/// Start condition:
/// - Continuously watches for PlayerInfomation instances until enough players are ready.
/// </summary>
public class DiceGameManager : MonoBehaviourPunCallbacks
{
    public static DiceGameManager Instance { get; private set; }

    [Header("Rule")]
    [SerializeField] private int maxTurns = 5;
    [SerializeField] private float rollWindowSeconds = 8f;
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    [Header("Start Watch (Important)")]
    [Tooltip("0 = unused. If set to 4, game waits until exactly 4 players are ready (offline/online).")]
    [SerializeField] private int expectedPlayers = 0; // 0なら未使用、4にすると4人揃うまで待つ
    [SerializeField] private float watchInterval = 0.25f;

    [Header("Network Room Object (Online)")]
    [SerializeField] private string diceNetTokenPrefabName = "DiceNetToken"; // Resources/ に置く

    [Header("Refs")]
    [SerializeField] private DiceUIController ui;

    // runtime
    private bool isOnline;
    private bool gameStarted = false;

    private int currentTurn = 0;
    private int currentBaba = 1;

    // player state
    private readonly Dictionary<int, PlayerInfomation> players = new();   // key: myNumber
    private readonly Dictionary<int, int> totalPoints = new();            // key: myNumber
    private readonly HashSet<int> eliminated = new();                     // eliminated player numbers

    // roll collection (per turn) - MASTER only
    private readonly Dictionary<int, int> turnRolls = new();              // key: myNumber -> face value
    private double rollDeadlineServerTime = 0;                            // PhotonNetwork.Time based (online)

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        isOnline = GameManager.instance != null && GameManager.instance.IsOnline();

        if (ui == null)
            ui = FindFirstObjectByType<DiceUIController>(FindObjectsInactive.Include);

        // 開始条件を常時監視
        StartCoroutine(WaitPlayersAndStartLoop());
    }

    // ---------------------------
    // Start Watch / Player Cache
    // ---------------------------

    /// <summary>
    /// Rebuild player cache every time to avoid stale references and duplicates.
    /// </summary>
    private void RebuildPlayersCache()
    {
        players.Clear();

        var infos = FindObjectsOfType<PlayerInfomation>(true);
        foreach (var p in infos)
        {
            if (p == null) continue;

            // 0.3運用想定（必要なら外してOK）
            if (p.myNumber < 0 || p.myNumber >= maxPlayers) continue;

            if (players.ContainsKey(p.myNumber))
            {
                Debug.LogWarning($"[DiceGame] Duplicate myNumber detected: {p.myNumber} -> overwrite");
            }

            players[p.myNumber] = p;
        }
    }

    private IEnumerator WaitPlayersAndStartLoop()
    {
        var wait = new WaitForSeconds(watchInterval);

        while (!gameStarted)
        {
            RebuildPlayersCache();

            // totalPoints dictionary init for newcomers
            foreach (var n in players.Keys)
            {
                if (!totalPoints.ContainsKey(n))
                    totalPoints[n] = 0;
            }

            // デバッグログ（必要ならONのままでOK）
            Debug.Log($"[DiceGame] waiting... online={isOnline} foundInfos={players.Count} " +
                      $"expected={expectedPlayers} min={minPlayers} " +
                      $"roomCount={(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : -1)}");

            if (IsReadyToStart_Strong())
            {
                gameStarted = true;

                // 初期化
                currentTurn = 0;
                currentBaba = 1;
                eliminated.Clear();
                foreach (var k in totalPoints.Keys.ToList()) totalPoints[k] = 0;

                if (isOnline)
                {
                    // Online: Master only starts the loop
                    if (PhotonNetwork.IsMasterClient)
                    {
                        CreateRoomTokensForJoinedPlayers();
                        StartCoroutine(MasterGameLoop());
                    }
                    // Non-master: wait for RPC start from master
                }
                else
                {
                    StartCoroutine(OfflineGameLoop());
                }

                yield break;
            }

            yield return wait;
        }
    }

    private bool IsReadyToStart_Strong()
    {
        // Offline / Online 共通：PlayerInfomation数で待つ
        int required = expectedPlayers > 0 ? expectedPlayers : minPlayers;

        if (players.Count < required) return false;

        // Online: Photon room countも一致させたい場合
        if (isOnline)
        {
            if (PhotonNetwork.CurrentRoom == null) return false;

            // expectedPlayersがあるならそれを満たすまで待つ（4人揃うまで等）
            if (expectedPlayers > 0)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount < expectedPlayers) return false;
            }
            else
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount < minPlayers) return false;
            }
        }

        // 参照が揃っているか（null事故防止）
        foreach (var p in players.Values)
        {
            if (p == null) return false;
            if (p.dicePlayer == null) return false;
        }

        return true;
    }

    private int AliveCount()
    {
        return players.Keys.Count(n => !eliminated.Contains(n));
    }

    private bool AllAliveReported_MasterOnly()
    {
        foreach (var n in players.Keys)
        {
            if (eliminated.Contains(n)) continue;
            if (!turnRolls.ContainsKey(n)) return false;
        }
        return true;
    }

    // ---------------------------
    // Online: Room Token
    // ---------------------------

    private void CreateRoomTokensForJoinedPlayers()
    {
        // 参加者ごとに “ルームオブジェクト（トークン）” を生成して識別に使う（任意）
        foreach (var p in players.Values)
        {
            GameObject token = PhotonNetwork.InstantiateRoomObject(diceNetTokenPrefabName, Vector3.zero, Quaternion.identity);
            var net = token.GetComponent<DiceNetToken>();
            if (net != null)
            {
                net.SetOwnerNumberLocal(p.myNumber);
                net.SetMaterialIndexLocal(p.GetMaterialIndex());
            }
        }

        photonView.RPC(nameof(RPC_SyncParticipants), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void RPC_SyncParticipants()
    {
        // ルーム内の DiceNetToken を読み取って UI 等に反映可能（ここでは何もしない）
    }

    // ---------------------------
    // Online: Master Loop
    // ---------------------------

    private IEnumerator MasterGameLoop()
    {
        if (players.Count < minPlayers)
        {
            Debug.LogWarning("[DiceGame] Not enough players.");
            yield break;
        }

        photonView.RPC(nameof(RPC_GameStart), RpcTarget.All, maxTurns, rollWindowSeconds);

        while (currentTurn < maxTurns && AliveCount() > 1)
        {
            currentTurn++;

            // 1) decide baba (master)
            currentBaba = UnityEngine.Random.Range(1, 7);

            // 2) start roll window
            turnRolls.Clear();
            rollDeadlineServerTime = PhotonNetwork.Time + rollWindowSeconds;

            photonView.RPC(nameof(RPC_TurnStart), RpcTarget.All, currentTurn, currentBaba, rollDeadlineServerTime);

            // 3) wait for reports or deadline
            while (PhotonNetwork.Time < rollDeadlineServerTime)
            {
                if (AllAliveReported_MasterOnly()) break;
                yield return null;
            }

            // 4) force auto-roll for missing
            foreach (var n in players.Keys)
            {
                if (eliminated.Contains(n)) continue;
                if (!turnRolls.ContainsKey(n))
                {
                    photonView.RPC(nameof(RPC_ForceAutoRoll), RpcTarget.All, n);
                }
            }

            // 5) wait short for forced reports
            double hardCap = PhotonNetwork.Time + 3.0;
            while (PhotonNetwork.Time < hardCap)
            {
                if (AllAliveReported_MasterOnly()) break;
                yield return null;
            }

            // 6) still missing => random fallback
            foreach (var n in players.Keys)
            {
                if (eliminated.Contains(n)) continue;
                if (!turnRolls.ContainsKey(n))
                {
                    int fallback = UnityEngine.Random.Range(1, 7);
                    turnRolls[n] = fallback;
                }
            }

            // 7) judge elimination & add points
            List<int> eliminatedThisTurn = new();
            foreach (var n in players.Keys)
            {
                if (eliminated.Contains(n)) continue;

                int face = turnRolls[n];
                if (face == currentBaba)
                {
                    eliminated.Add(n);
                    eliminatedThisTurn.Add(n);
                }
                else
                {
                    totalPoints[n] += face;
                }
            }

            // 8) broadcast results
            int[] nums = turnRolls.Keys.ToArray();
            int[] faces = turnRolls.Values.ToArray();
            photonView.RPC(nameof(RPC_TurnResult), RpcTarget.All, currentTurn, currentBaba, nums, faces, eliminatedThisTurn.ToArray());

            // 9) small wait for UI
            yield return new WaitForSeconds(2.0f);
        }

        // finish
        int[] finalNums = totalPoints.Keys.ToArray();
        int[] finalPoints = totalPoints.Values.ToArray();
        photonView.RPC(nameof(RPC_GameEnd), RpcTarget.All, finalNums, finalPoints, eliminated.ToArray());
    }

    // ---------------------------
    // RPCs
    // ---------------------------

    [PunRPC]
    private void RPC_GameStart(int maxTurn, float rollSeconds)
    {
        if (ui != null) ui.OnGameStart(maxTurn, rollSeconds);
    }

    [PunRPC]
    private void RPC_TurnStart(int turn, int baba, double deadlineServerTime)
    {
        currentTurn = turn;
        currentBaba = baba;

        if (ui != null) ui.OnTurnStart(turn, baba, deadlineServerTime, isOnline);

        // enable input for alive players
        foreach (var p in players.Values)
        {
            var ctrl = p.dicePlayer != null
                ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                : null;

            if (ctrl != null)
            {
                ctrl.ResetForNewTurn();
                ctrl.SetRollEnabled(!eliminated.Contains(p.myNumber));
            }
        }
    }

    [PunRPC]
    private void RPC_ForceAutoRoll(int playerNumber)
    {
        // 指定番号の人だけ auto-roll させる
        if (!players.TryGetValue(playerNumber, out var info)) return;

        var ctrl = info.dicePlayer != null
            ? info.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
            : null;

        if (ctrl != null && !ctrl.HasRolledThisTurn)
        {
            ctrl.AutoRoll();
        }
    }

    [PunRPC]
    private void RPC_TurnResult(int turn, int baba, int[] nums, int[] faces, int[] eliminatedNums)
    {
        var map = new Dictionary<int, int>();
        for (int i = 0; i < nums.Length; i++)
            map[nums[i]] = faces[i];

        foreach (var n in eliminatedNums)
            eliminated.Add(n);

        if (ui != null)
            ui.OnTurnResult(turn, baba, map, eliminatedNums, totalPoints, eliminated);

        // disable input
        foreach (var p in players.Values)
        {
            var ctrl = p.dicePlayer != null
                ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                : null;

            if (ctrl != null) ctrl.SetRollEnabled(false);
        }
    }

    [PunRPC]
    private void RPC_GameEnd(int[] nums, int[] points, int[] eliminatedNums)
    {
        var final = new Dictionary<int, int>();
        for (int i = 0; i < nums.Length; i++) final[nums[i]] = points[i];

        if (ui != null) ui.OnGameEnd(final, eliminatedNums);
    }

    [PunRPC]
    private void RPC_RevealSingleRoll(int playerNumber, int faceValue)
    {
        if (ui != null) ui.OnSingleRollRevealed(playerNumber, faceValue);
    }

    /// <summary>
    /// Clients call this to report their dice face to Master.
    /// </summary>
    public void ReportRollToMaster(int playerNumber, int faceValue)
    {
        if (!isOnline) return;
        photonView.RPC(nameof(RPC_ReportRoll), RpcTarget.MasterClient, playerNumber, faceValue);
    }

    [PunRPC]
    private void RPC_ReportRoll(int playerNumber, int faceValue, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (eliminated.Contains(playerNumber)) return;
        if (faceValue < 1 || faceValue > 6) return;

        turnRolls[playerNumber] = faceValue;

        // optional: reveal immediately
        photonView.RPC(nameof(RPC_RevealSingleRoll), RpcTarget.All, playerNumber, faceValue);
    }

    // ---------------------------
    // Offline Loop
    // ---------------------------

    private IEnumerator OfflineGameLoop()
    {
        if (players.Count < minPlayers)
        {
            Debug.LogWarning("[DiceGame][Offline] Not enough players.");
            yield break;
        }

        currentTurn = 0;
        eliminated.Clear();
        foreach (var k in totalPoints.Keys.ToList()) totalPoints[k] = 0;

        if (ui != null) ui.OnGameStart(maxTurns, rollWindowSeconds);

        while (currentTurn < maxTurns && AliveCount() > 1)
        {
            currentTurn++;
            currentBaba = UnityEngine.Random.Range(1, 7);

            double localDeadline = Time.timeAsDouble + rollWindowSeconds;
            if (ui != null) ui.OnTurnStart(currentTurn, currentBaba, localDeadline, false);

            // enable input
            foreach (var p in players.Values)
            {
                var ctrl = p.dicePlayer != null
                    ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                    : null;

                if (ctrl != null)
                {
                    ctrl.ResetForNewTurn();
                    ctrl.SetRollEnabled(!eliminated.Contains(p.myNumber));
                }
            }

            // wait until all rolled or timeout
            while (Time.timeAsDouble < localDeadline)
            {
                if (AllAliveRolledOffline()) break;
                yield return null;
            }

            // force auto roll
            foreach (var p in players.Values)
            {
                if (eliminated.Contains(p.myNumber)) continue;

                var ctrl = p.dicePlayer != null
                    ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                    : null;

                if (ctrl != null && !ctrl.HasRolledThisTurn) ctrl.AutoRoll();
            }

            // wait a bit for dice settle
            yield return new WaitForSeconds(2f);

            // collect results from controllers
            turnRolls.Clear();
            foreach (var p in players.Values)
            {
                if (eliminated.Contains(p.myNumber)) continue;

                var ctrl = p.dicePlayer != null
                    ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                    : null;

                if (ctrl != null)
                    turnRolls[p.myNumber] = ctrl.LastFaceValue;
                else
                    turnRolls[p.myNumber] = UnityEngine.Random.Range(1, 7);
            }

            // judge
            List<int> eliminatedThisTurn = new();
            foreach (var n in players.Keys)
            {
                if (eliminated.Contains(n)) continue;

                int face = turnRolls[n];
                if (face == currentBaba)
                {
                    eliminated.Add(n);
                    eliminatedThisTurn.Add(n);
                }
                else
                {
                    totalPoints[n] += face;
                }
            }

            if (ui != null)
                ui.OnTurnResult(currentTurn, currentBaba, new Dictionary<int, int>(turnRolls), eliminatedThisTurn.ToArray(), totalPoints, eliminated);

            // disable input
            foreach (var p in players.Values)
            {
                var ctrl = p.dicePlayer != null
                    ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                    : null;

                if (ctrl != null) ctrl.SetRollEnabled(false);
            }

            yield return new WaitForSeconds(2f);
        }

        if (ui != null) ui.OnGameEnd(new Dictionary<int, int>(totalPoints), eliminated.ToArray());
    }

    private bool AllAliveRolledOffline()
    {
        foreach (var p in players.Values)
        {
            if (eliminated.Contains(p.myNumber)) continue;

            var ctrl = p.dicePlayer != null
                ? p.dicePlayer.GetComponentInChildren<PlayerDiceController>(true)
                : null;

            if (ctrl == null || !ctrl.HasRolledThisTurn) return false;
        }
        return true;
    }
}

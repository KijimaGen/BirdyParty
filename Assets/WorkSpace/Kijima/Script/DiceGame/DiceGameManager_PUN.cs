using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BabaDiceGameManager_PUN : MonoBehaviourPun
{
    [System.Serializable]
    public class PlayerState
    {
        public int id;
        public int actorNumber; // Photon用
        public DiceActor dice;
        public bool alive = true;
        public int score = 0;
        public int lastRoll = 0;
    }

    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private DiceActor dicePrefab;
    [SerializeField] private BabaDiceUI ui;

    private List<PlayerState> players = new();

    private int babaNumber = 1;

    private bool IsOnline => PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
    private bool IsMaster => PhotonNetwork.IsMasterClient;

    // ==========================================
    // 初期化
    // ==========================================
    private void Start()
    {
        SetupPlayers();
        StartNewTurn();
    }

    private void SetupPlayers()
    {
        players.Clear();

        if (IsOnline)
        {
            var photonPlayers = PhotonNetwork.PlayerList;

            for (int i = 0; i < photonPlayers.Length; i++)
            {
                CreatePlayer(i, photonPlayers[i].ActorNumber);
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                CreatePlayer(i, -1);
            }
        }

        ui.Init(players.Select(p => p.id).ToArray());
    }

    private void CreatePlayer(int index, int actorNumber)
    {
        var dice = Instantiate(dicePrefab, spawnPoints[index].position, spawnPoints[index].rotation);
        dice.Setup(index, null);
        dice.OnRollFinalized += OnRollFinalized;

        players.Add(new PlayerState
        {
            id = index,
            actorNumber = actorNumber,
            dice = dice
        });
    }

    // ==========================================
    // 入力（ローカルは常に有効）
    // ==========================================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryRoll(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryRoll(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryRoll(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TryRoll(3);
    }

    private void TryRoll(int index)
    {
        if (index < 0 || index >= players.Count) return;

        var p = players[index];
        if (!p.alive) return;
        if (p.lastRoll > 0) return;

        p.dice.RollNow();
    }

    // ==========================================
    // ダイス確定（共通窓口）
    // ==========================================
    private void OnRollFinalized(DiceActor dice, int value)
    {
        var p = players.First(x => x.id == dice.PlayerId);

        if (IsOnline)
        {
            // オンラインならMasterに送る
            photonView.RPC(nameof(RPC_SubmitRoll),
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber,
                value);
        }
        else
        {
            // ローカルならそのまま処理
            ProcessRoll(p, value);
        }
    }

    // ==========================================
    // Masterが処理
    // ==========================================
    [PunRPC]
    private void RPC_SubmitRoll(int actorNumber, int value)
    {
        if (!IsMaster) return;

        var p = players.First(x => x.actorNumber == actorNumber);

        ProcessRoll(p, value);

        // 結果を全員へ同期
        photonView.RPC(nameof(RPC_SyncPlayer),
            RpcTarget.All,
            p.id,
            p.score,
            p.alive,
            value);
    }

    // ==========================================
    // 実際の処理（唯一のスコア処理場所）
    // ==========================================
    private void ProcessRoll(PlayerState p, int value)
    {
        p.lastRoll = value;

        if (value == babaNumber)
        {
            p.alive = false;
            ui.SetPlayerEliminated(p.id, true);
        }
        else
        {
            p.score += value;
        }

        ui.SetPlayerLastRoll(p.id, value);
        //ui.UpdateScores(players);
    }

    // ==========================================
    // 全員へ同期
    // ==========================================
    [PunRPC]
    private void RPC_SyncPlayer(int playerId, int score, bool alive, int value)
    {
        var p = players.First(x => x.id == playerId);

        p.score = score;
        p.alive = alive;
        p.lastRoll = value;

        ui.SetPlayerLastRoll(playerId, value);
        //ui.UpdateScores(players);
    }

    // ==========================================
    // ターン開始
    // ==========================================
    private void StartNewTurn()
    {
        babaNumber = Random.Range(1, 7);
        ui.SetBaba(babaNumber);

        foreach (var p in players)
            p.lastRoll = 0;
    }
}

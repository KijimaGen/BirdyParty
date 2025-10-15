/**
 * @file RaceManager_PUN.cs
 * @brief PUN2対応のレースゲーム管理クラス
 * @author Sum1r3 + GPT
 * @date 2025/10/10
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RaceManager_PUN : MonoBehaviourPunCallbacks {
    // --- シングルトン ---
    public static RaceManager_PUN instance;

    // --- 各種管理 ---
    private List<RacePlayer> racers = new List<RacePlayer>();
    private List<RacePlayer> ranking = new List<RacePlayer>();

    //準備完了かどうか
    //private bool isStandby = false;
    public bool isStart { get; private set; } = false;
    public bool isGoal { get; private set; } = false;

    //オンラインか否か
    public bool isOnline;

    // --- 各プレイヤーの開始位置 ---
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-65f, 1.2f, 1f),
        new Vector3(-65, 1.2f, -3.2f),
        new Vector3(-65, 1.2f, -7),
        new Vector3(-65, 1.2f, -11)
    };

    // --- ゴール後の表示位置 ---
    private readonly Vector3[] rankingPositions = new Vector3[]
    {
        new Vector3 (-3.6f, 6, -96f),
        new Vector3 (-1.6f, 5, -96f),
        new Vector3 (0.6f, 4, -96f),
        new Vector3 (2.6f, 3, -96f)
    };

    [SerializeField]
    private RacePlayer racersPlayer;

    private void Awake() {
        instance = this;
    }

    private void Update() {
        // 全員ゴール判定
        if (racers.Count == ranking.Count && isStart) {
            PlayerGoalPosSet();
            photonView.RPC(nameof(RPC_SetGoal), RpcTarget.AllBuffered); // ←こっちがオススメ！
        }
    }

    // ============================================
    // ✅ 初期化
    // ============================================
    public override void OnJoinedRoom() {
        Debug.Log($"Room joined! Player count: {PhotonNetwork.CurrentRoom.PlayerCount}");


    }

    // ============================================
    // ✅ 全員にカウントダウン合図を送る
    // ============================================
    public void TryStartCountDown() {
        if (PhotonNetwork.IsMasterClient) {
            if (isStart) return;

            Debug.Log("MasterClient: Sending StartCountDownRPC to all players...");
            photonView.RPC(nameof(StartCountDownRPC), RpcTarget.All);
        }
    }

    [PunRPC]
    private async void StartCountDownRPC() {
        if (isStart) return;
        Debug.Log("All Clients: StartCountDown開始！");
        await StartCountDown();
    }

    // ============================================
    // ✅ カウントダウン
    // ============================================
    private async UniTask StartCountDown() {
        // プレイヤーをスタート位置に置く
        PlayerStartPosSet();

        _ = AudioManager.instance.PlaySE(2);
        await UniTask.Delay(3000);

        // スタート！
        isStart = true;
        Debug.Log("GO!!!!");
    }

    // ============================================
    // ✅ プレイヤーの登録
    // ============================================
    public void AddRacers(RacePlayer player) {
        racers.Add(player);
    }

    /// <summary>
    /// 引数に来たオブジェクトが、何番目に来たのかを渡す
    /// </summary>
    /// <param Name="player"></param>
    /// <returns></returns>
    public int GetPlayerNumber(RacePlayer player) {
        return racers.IndexOf(player);
    }

    // ============================================
    // ✅ プレイヤーの初期位置・ゴール処理
    // ============================================
    public void PlayerStartPosSet() {
        for (int i = 0; i < racers.Count; i++) {
            if (racers[i] == null) continue;
            int num = racers[i].GetMyNumber();
            racers[i].SetPosition(spawnPositions[num]);
        }
    }

    /// <summary>
    /// 全員がゴールした後に、表彰台に並べる
    /// </summary>
    public void PlayerGoalPosSet() {
        for (int i = 0; i < racers.Count; i++) {
            if (racers[i] == null) continue;
            racers[i].SetPosition(rankingPositions[racers[i].myRank]);
        }
    }

    /// <summary>
    /// ランキングに加える
    /// </summary>
    /// <param Name="player"></param>
    public void AddRanking(RacePlayer player) {
        //一応ここでランキングが重複しないかチェック
        for(int i = 0,max = ranking.Count;i < max; i++) {
            if (ranking[i] == player)
                return;
        }

        ranking.Add(player);
    }

    /// <summary>
    /// 引数にきたオブジェクトがランキングの何番目にいるのか返す
    /// </summary>
    /// <param Name="player"></param>
    /// <returns></returns>
    public int GetRankingCount(RacePlayer player) {
        return ranking.IndexOf(player);
    }

    // 🔥 RPCで全員に同期する処理
    [PunRPC]
    private void RPC_SetGoal() {
        isGoal = true;
        Debug.Log("ゴールフラグが全員に伝わった！");
    }
}

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
using UnityEngine.SceneManagement;
using static GameConst;

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

    //終わった後に表示するランキングのリスト
    [SerializeField]
    private List<GameObject> rankingModelList = new List<GameObject>();
    //終わったときの演出を入れたかどうか
    private bool isFinished = false;
    //フィニッシュ
    [SerializeField]
    private GameObject finishCanvas;


    //自身のインスタンスを作成
    private void Awake() {
        instance = this;
        //ゴールを初期化
        isGoal = false;
        isFinished = false;
        //BGM再生
        AudioManager.instance.PlayBGM(1);
        //フェードイン
        _ = FadeManager.instance.FadeIn();
        //フィニッシュキャンバスを消す
        finishCanvas.SetActive(false);
    }


    //
    int lastCount = -1;
    double startTime;

    async void Update(){


        // 全員ゴール判定
        if (racers.Count == ranking.Count && isStart && !isGoal) {
            //終了演出出てないようだったら、出す
            if (!isFinished) {
                //二回も演出入れないように
                isFinished = true;
                //効果音の再生
                _ = AudioManager.instance.PlaySE(14);
                //フィニッシュキャンバスをつける
                finishCanvas.SetActive(true);
                //三秒待つ
                await UniTask.Delay(3000);
                //フィニッシュキャンバスを消す
                finishCanvas.SetActive(false);
                //次の処理
                Goal();
            }
        }

        //ゴールしてた時にポジションを常にゴール地点に設定
        if (isGoal) {
            //プレイヤーのランキングモデルを表示
            PlayerRankModelSetActive();
            //ゴール位置にプレイヤーを送る関数
            SetRankModelColor();
        }
        //オンラインの時のゲーム開始カウント
        if(isOnline) {
            if (!PhotonNetwork.CurrentRoom.CustomProperties
            .TryGetValue("StartTime", out object value))
                return;

            startTime = (double) value;

            double remaining = startTime - PhotonNetwork.Time;

            if (remaining <= 0) {
                if (!isStart) {
                    isStart = true;
                    Debug.Log("GO!!!!");
                }
                return;
            }

            int currentCount = Mathf.CeilToInt((float) remaining);

            if (currentCount != lastCount) {
                lastCount = currentCount;

                // 3,2,1 のとき鳴らす
                if (currentCount <= 3) {
                    _ = AudioManager.instance.PlaySE(1);
                }
            }
        }
    }

    // ============================================
    // ✅ 初期化
    // ============================================
    public override void OnJoinedRoom() {
        Debug.Log($"Room joined! Player count: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    // ============================================
    // ✅ 全員にカウントダウン合図を送る（オンライン/オフライン対応）
    // ============================================
    public void TryStartCountDown() {
        // オンライン時の処理
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (isStart) return;

            double startTime = PhotonNetwork.Time + 5.0; // 5秒後

            ExitGames.Client.Photon.Hashtable hash =
                new ExitGames.Client.Photon.Hashtable();

            hash["StartTime"] = startTime;

            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
        // オフライン時の処理
        else {
            if (isStart) return;
            Debug.Log("オフライン：カウントダウンを直接開始");
            StartCountDownRPC();
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

    /// <summary>
    /// げーむすたーと
    /// </summary>
    private void StartRace()
    {
        if (isStart) return;

        isStart = true;
        PlayerStartPosSet();
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
    /// <param myName="player"></param>
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
    /// <param myName="player"></param>
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
    /// <param myName="player"></param>
    /// <returns></returns>
    public int GetRankingCount(RacePlayer player) {
        return ranking.IndexOf(player);
    }

    // 🔥 RPCで全員に同期する処理
    [PunRPC]
    private async void RPC_SetGoal() {
        isGoal = true;
        Debug.Log("ゴールフラグが全員に伝わった！");
        await AfterGoal();
    }

    /// <summary>
    /// ゴールした後の処理
    /// </summary>
    /// <returns></returns>
    private async UniTask AfterGoal() {
        //五秒ほど待って
        await UniTask.Delay(5000);

        //フェードアウト
        await FadeManager.instance.FadeOut();

        if (GameManager.instance != null && GameManager.instance.isPartyMode && PartyModeManager.instance != null){
            // パーティ：次へ進めてルーレット（タイトル）へ戻す
            PartyModeManager.instance.OnMiniGameFinishedAndReturnToRoulette();
            return;
        }
        
        //画面遷移
        SceneManager.LoadScene(TITLE_SCENE_NAME);
    }

    /// <summary>
    /// レーサーの数だけモデルを表示する
    /// </summary>
    public void PlayerRankModelSetActive(){
        for(int i = 0,max = racers.Count;i < max; i++) {
            int rank = racers[i].myRank;
            if(rank < 0 || rank >= rankingModelList.Count) continue;
            //ランクに応じたモデルを表示
            rankingModelList[rank].SetActive(true);
        }
    }

    /// <summary>
    /// ランキングにあわせてモデルの色を変える
    /// </summary>
    public void SetRankModelColor(){
        for(int i = 0,max = ranking.Count;i < max; i++) {

            //色の取得
            Color rankColor = ranking[i].GetMyColror();
            foreach (Transform child in rankingModelList[i].transform){
                if (child.name == "LeftEye" || child.name == "RightEye") continue;
                if (child.name == "hat" || child.name == "Canvas") continue;
                if (child.name == "LeftReg" || child.name == "RightReg") continue;
                if (child.name == "UnderMouse" || child.name == "UpMouse") continue;
                if (child.name == "アーマチュア") continue;

                //色の適用
                child.GetComponent<Renderer>().material.color = rankColor;
            }

        }
    }

    // もしミニゲーム中にウィンドウを落としたらタイトルに戻るように
    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt(PartyModeManager.PREF_PARTY_RUNNING, 0);
        PlayerPrefs.SetInt(PartyModeManager.PREF_BACK_TO_PARTY, 0);
        PlayerPrefs.SetInt(PartyModeManager.PREF_PARTY_SHOW_RESULT, 0);
        PlayerPrefs.SetInt("ComeBackFromGame", 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 終了処理
    /// </summary>
    private void Goal() {
        //一応バグ制御でここで全員ゴールにしておく
        for (int i = 0; i < racers.Count; i++) {
            racers[i].Goal();
        }

        // オンライン時はRPCで同期、オフライン時は直接呼び出し
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom) {
            // オンライン：RPCで全プレイヤーに送信
            photonView.RPC(nameof(RPC_SetGoal), RpcTarget.AllBuffered);
            RPC_SetGoal();
        }
        else {
            // オフライン：直接メソッドを呼び出し
            RPC_SetGoal();
        }
    }
}

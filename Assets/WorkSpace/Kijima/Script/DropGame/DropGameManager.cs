/**
 * @file DropGameManager.cs
 * @brief PUN2対応のレースゲーム管理クラス
 * @author Sum1r3 + GPT
 * @date 2025/10/16
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using static GameConst;
using UnityEngine.UI;
using Unity.VisualScripting;
using System;
using System.Linq;

public class DropGameManager : MonoBehaviourPunCallbacks {
    // --- シングルトン ---
    public static DropGameManager instance;

    // --- 各種管理 ---
    private List<DropPlayer> droppers = new List<DropPlayer>();
    private List<DropPlayer> ranking = new List<DropPlayer>();

    //準備完了かどうか
    //private bool isStandby = false;
    public bool isStart { get; private set; } = false;
    public bool isEnd { get; private set; } = false;

    //オンラインか否か
    public bool isOnline;

    // --- 各プレイヤーの開始位置 ---(書き換え予定)
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-65f, 1.2f, 1f),
        new Vector3(-65, 1.2f, -3.2f),
        new Vector3(-65, 1.2f, -7),
        new Vector3(-65, 1.2f, -11)
    };

    // --- ゴール後の表示位置 ---(書き換え予定)
    private readonly Vector3[] rankingPositions = new Vector3[]
    {
        new Vector3 (-3.6f, 6, -96f),
        new Vector3 (-1.6f, 5, -96f),
        new Vector3 (0.6f, 4, -96f),
        new Vector3 (2.6f, 3, -96f)
    };

    
    // 落ちる先のパネルのリスト
    
    private List<DropPanel> dropPanelList = new List<DropPanel>();

    // 正解のパネルの種類
    private DropGamePanelVariation TrueAnswerPanel;

    //ドロップゲームのプレイヤーリスト
    private List<DropPlayer> dropPlayerList = new List<DropPlayer>();

    //登場予定の絵柄のリスト
    [SerializeField]
    private List<Sprite> VariationSpriteList = new List<Sprite>();
    //次に行く先を示す絵
    [SerializeField]
    private Image NextVariationImage;
    //パネルのプレファブ
    [SerializeField]
    private DropGameFrame PanelPrefab;
    private DropGameFrame PanelObject;

    [SerializeField]
    private List<Material> PanelMaterialList = new List<Material>();

    //パネルの最大数
    private const int PANEL_MAX = 4;

    private async void Awake() {
        instance = this;
        isStart = false;

        
        
        await UniTask.Delay(1000);
        

        //ゲームの開始を宣言する！
        //await StartCountDown();
    }

    private void Update() {
        // 全員ゴール判定
        //if (droppers.Count == ranking.Count && isStart) {
        //    PlayerGoalPosSet();
            
        //    // オンライン時はRPCで同期、オフライン時は直接呼び出し
        //    if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && GameManager.instance.IsOnline()) {
        //        // オンライン：RPCで全プレイヤーに送信
        //        photonView.RPC(nameof(RPC_SetGoal), RpcTarget.AllBuffered);
        //    } else {
        //        // オフライン：直接メソッドを呼び出し
        //        RPC_SetGoal();
        //    }
        //}
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
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom) {
            if (PhotonNetwork.IsMasterClient) {
                if (isStart) return;

                Debug.Log("オンライン：MasterClientがカウントダウンを開始");
                photonView.RPC(nameof(StartCountDownRPC), RpcTarget.All);
            }
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

        //何でか複数回生成されるバグが見受けられたので規制
        if(PanelObject == null) {
            //落ちる先のパネルたちの呼び出しついでに参照の取得
            PanelObject = Instantiate(PanelPrefab);
            //パネルの答えの抽選
            LotteryAnswerVariation();
        }

        
    }

    // ============================================
    // ✅ プレイヤーの登録
    // ============================================
    public void AddRacers(DropPlayer player) {
        droppers.Add(player);
    }

    /// <summary>
    /// 引数に来たオブジェクトが、何番目に来たのかを渡す
    /// </summary>
    /// <param myName="player"></param>
    /// <returns></returns>
    public int GetPlayerNumber(DropPlayer player) {
        return droppers.IndexOf(player);
    }

    // ============================================
    // ✅ プレイヤーの初期位置・ゴール処理
    // ============================================
    public void PlayerStartPosSet() {
        for (int i = 0; i < droppers.Count; i++) {
            if (droppers[i] == null) continue;
            int num = droppers[i].GetMyNumber();
            droppers[i].SetPosition(spawnPositions[num]);
        }
    }

    /// <summary>
    /// 全員がゴールした後に、表彰台に並べる
    /// </summary>
    public void PlayerGoalPosSet() {
        for (int i = 0; i < droppers.Count; i++) {
            if (droppers[i] == null) continue;
            droppers[i].SetPosition(rankingPositions[droppers[i].myRank]);
        }
    }

    /// <summary>
    /// ランキングに加える
    /// </summary>
    /// <param myName="player"></param>
    public void AddRanking(DropPlayer player) {
        //一応ここでランキングが重複しないかチェック
        for (int i = 0, max = ranking.Count; i < max; i++) {
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
    public int GetRankingCount(DropPlayer player) {
        return ranking.IndexOf(player);
    }

    // 🔥 RPCで全員に同期する処理
    [PunRPC]
    private void RPC_SetGoal() {
        isEnd = true;
        Debug.Log("ゴールフラグが全員に伝わった！");
    }

    /// <summary>
    /// マテリアルの名前を受け取って、それに合ったバリエーションを引き渡す
    /// </summary>
    /// <param name="materialName"></param>
    /// <returns></returns>
    public DropGamePanelVariation GetMyVariationFromMaterial(string materialName) {
        if (System.Enum.TryParse(materialName, out DropGamePanelVariation variation)) {
            return variation;
        }

        Debug.LogError($"無効なマテリアル名: {materialName}");
        return DropGamePanelVariation.None; // デフォルト値
    }

    /// <summary>
    /// バリエーションの名前を受け取って、それに合ったマテリアルを引き渡す
    /// </summary>
    /// <param name="materialName"></param>
    /// <returns></returns>
    public Material GetMyMaterialFromVariation(DropGamePanelVariation variation) {
        for(int i = 0,max = PanelMaterialList.Count; i < max; i++) {
            if (PanelMaterialList[i].name == variation.ToString())
            return PanelMaterialList[i];
        }

        Debug.LogError($"無効なバリエーション名: {variation}");
        return null; // デフォルト値
    }


    /// <summary>
    /// 答え合わせ(雑(だがこれでいい))
    /// </summary>
    /// <param name="AnswerPanel"></param>
    /// <returns></returns>
    public bool CheckingAnswers( DropGamePanelVariation AnswerPanel) {
        if(AnswerPanel == TrueAnswerPanel) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// プレイヤーエントリー
    /// </summary>
    /// <param name="player"></param>
    public void EntryDropPlayer(DropPlayer player) {
        dropPlayerList.Add(player);
    }

    /// <summary>
    /// バリエーションを受け取ってそれと同じ名前のスプライトを返す
    /// </summary>
    /// <param name="Variation"></param>
    /// <returns></returns>
    private Sprite GetSpriteFromVariation(DropGamePanelVariation Variation) {
        for(int i = 0,max = VariationSpriteList.Count; i < max; i++) {
            if (VariationSpriteList[i].name == Variation.ToString()) {
                return VariationSpriteList[i];
            }
        }

        return null;
    }

    /// <summary>
    /// パネルの再抽選を共有
    /// </summary>
    public void TryLotteryPanel() {
        // オンライン時の処理
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom) {
            if (PhotonNetwork.IsMasterClient) {
                Debug.Log("オンライン：MasterClientがパネル再抽選を開始");
                photonView.RPC(nameof(SetPanel), RpcTarget.All, MakePanelList().Select(v => (int) v).ToArray());
            }
        }
        // オフライン時の処理
        else {
            Debug.Log("オフライン：パネル再抽選を直接開始");
            SetPanel(MakePanelList().Select(v => (int) v).ToArray());
        }
    }


    /// <summary>
    /// 正解のバリエーションの抽選
    /// </summary>
    
    public void LotteryAnswerVariation() {

        //バリエーションリストをインターネッツで作ってもらう
        TryLotteryPanel();


        //パネルたちに自分のパネルバリエーション設定処理を呼んでもらってからそれを受け取る
        for (int i = 0, max = dropPanelList.Count; i < max; i++) {
            dropPanelList[i].SetMyVariation();
        }

        //答えのパネルを作ってもらう
        

        //画面上のパネルに答えを表示
        NextVariationImage.sprite = GetSpriteFromVariation(TrueAnswerPanel);

        //パネルの位置をリセット
        PanelObject.ResetPos();
    }

    /// <summary>
    /// インターネットに合わせた動作
    /// </summary>
    private void TrySetPanel(DropGamePanelVariation answerPanel) {
        // オンライン時の処理
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom) {
            if (PhotonNetwork.IsMasterClient) {
                //作ってあるパネルリストから答えを作成
                int randomVal = UnityEngine.Random.Range(0, dropPanelList.Count);
                TrueAnswerPanel = dropPanelList[randomVal].GetPanelVariation();

                Debug.Log("オンライン：MasterClientがパネル再抽選を開始");
                photonView.RPC(nameof(SetPanel), RpcTarget.All, MakePanelList().Select(v => (int) v).ToArray());
            }
        }
        // オフライン時の処理
        else {
            Debug.Log("オフライン：パネル再抽選を直接開始");
            SetPanel(MakePanelList().Select(v => (int) v).ToArray());
        }
    }

    /// <summary>
    /// パネルのリストに動的に追加
    /// </summary>
    /// <param name="panel"></param>
    public void AddPanelList(DropPanel panel) {
        dropPanelList.Add(panel);
    }

    /// <summary>
    /// 配列型を受け取ってパネルに反映
    /// </summary>
    /// <param name="PanelList"></param>
    [PunRPC]
    private void SetPanel(int[] variationInts) {
        // int配列 → enum配列 に戻す
        DropGamePanelVariation[] PanelList =
            variationInts.Select(i => (DropGamePanelVariation) i).ToArray();
        //先頭から四つ抽出してリストを作成
        for (int i = 0,max = PANEL_MAX; i < max;i++) {
            dropPanelList[i].SetMeshRenderer(GetMyMaterialFromVariation(PanelList[i]));
        }
    }

    /// <summary>
    /// ランダムにパネルリストを作成
    /// </summary>
    /// <returns></returns>
    private DropGamePanelVariation[] MakePanelList() {
        //enumを配列に変換
        DropGamePanelVariation[] RandomVariationList =
        Enum.GetValues(typeof(DropGamePanelVariation))
         .Cast<DropGamePanelVariation>()  // ←ここでCastする！
         .Where(s => s != DropGamePanelVariation.None) // ←Noneを除外
         .OrderBy(s => UnityEngine.Random.value)
         .ToArray();

        //シャッフル
        RandomVariationList = RandomVariationList.OrderBy(x => UnityEngine.Random.value).ToArray();
        return RandomVariationList;
    }

    public void SetAnswerPanel(int AnswerInt) {
        
    }
}

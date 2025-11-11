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
using System;
using System.Linq;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class DropGameManager : MonoBehaviourPunCallbacks {
    // --- シングルトン ---
    public static DropGameManager instance;

    // --- 各種管理 ---
    private List<DropPlayer> dropperList = new List<DropPlayer>();
    private List<DropPlayer> ranking = new List<DropPlayer>();

    //準備完了かどうか
    //private bool isStandby = false;
    public bool isStart { get; private set; } = false;
    public bool isEnd { get; private set; } = false;

    // --- 各プレイヤーの開始位置
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-1f, 100f, 1.8f),
        new Vector3(10, 100f, 1.8f),
        new Vector3(10, 100f, -12),
        new Vector3(-1f, 100f, -12)
    };

    // --- ゴール後の表示位置 ---(書き換え予定)
    private readonly Vector3[] rankingPositions = new Vector3[]
    {
        new Vector3 (-14f, 4.7f, 11f),
        new Vector3 (-12.7f, 4.05f,11f),
        new Vector3 (-11.35f, 3.4f, 11f),
        new Vector3 (-10.1f, 2.75f, 11f)
    };
    
    // 落ちる先のパネルのリスト
    private List<DropPanel> dropPanelList = new List<DropPanel>();

    // 正解のパネルの種類
    private DropGamePanelVariation TrueAnswerPanel;

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

    //パネルのマテリアル一覧
    [SerializeField]
    private List<Material> PanelMaterialList = new List<Material>();

    //パネルの最大数
    private const int PANEL_MAX = 4;

    //プレイヤーのポイントを示すUI
    [SerializeField]
    private List<GameObject> pointUIList = new List<GameObject>();

    //ゲームを行った回数
    private int gameCount = 0;

    //定数
    private const int POINT_TEXT_INDEX = 0;
    private const int NUMBER_TEXT_INDEX = 1;
    private const int GAME_END_COUNT = 8;   //ゲームが終わるカウント
    private const string KEY_NAME_POINT = "PlayerScore";

    private async void Awake() {
        instance = this;
        isStart = false;
        //カウントの初期化
        gameCount = 0;

        //ポイントの奴見れないようにするよ～
        for (int i = 0; i < pointUIList.Count; i++) {
            pointUIList[i].SetActive(false);
        }

        

        await UniTask.Delay(1000);
        
        

        //ゲームの開始を宣言する！
        //await StartCountDown();
    }

    private void Update() {

        //始まる前に行いたい処理
        if(!isStart) {
            //全員のポジションを設定
            for (int i = 0, max = dropperList.Count; i < max; i++) {
                if (dropperList[i] == null) continue;
                dropperList[i].SetPosition(spawnPositions[i]);
            }
        }

        // 全員ゴール判定
        //GAME_END_COUNT回やったらおしまい
        if (gameCount == GAME_END_COUNT) {
            //プレイヤーをポイント順でランキングに入れる
            MakeRanking();
            //プレイヤーをランキング順で並べる
            PlayerGoalPosSet();

            // オンライン時はRPCで同期、オフライン時は直接呼び出し
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && GameManager.instance.IsOnline()) {
                // オンライン：RPCで全プレイヤーに送信
                photonView.RPC(nameof(RPC_SetGoal), RpcTarget.AllBuffered);
            }
            else {
                // オフライン：直接メソッドを呼び出し
                RPC_SetGoal();
            }
        }
        //デバッグだよ
        if(Input.GetKeyDown(KeyCode.E)) {
            gameCount++;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public override void OnJoinedRoom() {
        Debug.Log($"Room joined! Player count: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    /// <summary>
    /// 全員にカウントダウン合図を送る（オンライン/オフライン対応）
    /// </summary>
    public void TryStartCountDown() {
        // オンライン時の処理
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom /*&& GameManager.instance.IsOnline()*/) {
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

    /// <summary>
    /// カウントダウンをRPCで始める
    /// </summary>
    [PunRPC]
    private async void StartCountDownRPC() {
        if (isStart) return;
        Debug.Log("All Clients: StartCountDown開始！");
        await StartCountDown();
    }

    /// <summary>
    /// カウントダウン
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// プレイヤーの登録
    /// </summary>
    /// <param name="player"></param>
    public void AddDropper(DropPlayer player) {
        dropperList.Add(player);
        pointUIList[player.GetMyNumber()].SetActive(true);
        //名前の反映
        int PlayerNumber = player.GetMyNumber();
        TextMeshProUGUI playerText = pointUIList[PlayerNumber].transform.GetChild(NUMBER_TEXT_INDEX).GetComponent<TextMeshProUGUI>();
        playerText.text = player.myName;


        //ポイントを反映してもらう
        SetPointUI(player);


    }

    /// <summary>
    /// 引数に来たオブジェクトが、何番目に来たのかを渡す
    /// </summary>
    /// <param myName="player"></param>
    /// <returns></returns>
    public int GetPlayerNumber(DropPlayer player) {
        return dropperList.IndexOf(player);
    }

    /// <summary>
    /// プレイヤーの初期位置・ゴール処理
    /// </summary>
    public void PlayerStartPosSet() {
        for (int i = 0; i < dropperList.Count; i++) {
            if (dropperList[i] == null) continue;
            int num = dropperList[i].GetMyNumber();
            dropperList[i].SetPosition(spawnPositions[num]);
        }
    }

    /// <summary>
    /// 全員がゴールした後に、表彰台に並べる
    /// </summary>
    public void PlayerGoalPosSet() {
        for (int i = 0; i < dropperList.Count; i++) {
            if (dropperList[i] == null) continue;
            dropperList[i].SetPosition(rankingPositions[dropperList[i].myRank]);
        }
    }

    /// <summary>
    /// ランキングを作る
    /// </summary>
    /// <param myName="player"></param>
    public void MakeRanking() {
        //ランキングにプレイヤーのリストを代入
        ranking = dropperList;
        //ランキングの中身をPoint順にソート
        ranking.Sort((p1,p2) => p1.myPoint.CompareTo(p2.myPoint));
        //ランキングをプレイヤーに渡す
        for(int i = 0,max = dropperList.Count; i < max; i++) {
            for(int j = 0; j < ranking.Count; j++) {
                //i番目とj番目が違ったら続行
                if (dropperList[i] != ranking[j]) continue;
                //プレイヤーにランキングを渡す
                dropperList[i].SetRank(GetRankingCount(dropperList[i]));
            }
        }

    }

    /// <summary>
    /// 引数にきたオブジェクトがランキングの何番目にいるのか返す
    /// </summary>
    /// <param myName="player"></param>
    /// <returns></returns>
    public int GetRankingCount(DropPlayer player) {
        return ranking.IndexOf(player);
    }

    /// <summary>
    /// RPCで全員に同期するゲームエンド
    /// </summary>
    [PunRPC]
    private void RPC_SetGoal() {
        isEnd = true;
        
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
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom /*&& GameManager.instance.IsOnline()*/) {
            if (PhotonNetwork.IsMasterClient) {
                //パネルリストをホストだけ作成
                var panelList = MakePanelList();

                photonView.RPC(nameof(SetPanel), RpcTarget.All, panelList.Select(v => (int) v).ToArray());
            }
        }
        // オフライン時の処理
        else {
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
        TrySetPanel();

        //パネルの位置をリセット
        PanelObject.ResetPos();
    }

    /// <summary>
    /// インターネットに合わせたパネルリストからの答えの作成
    /// </summary>
    private void TrySetPanel() {
        // オンライン時の処理
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && GameManager.instance.IsOnline()) {
            //部屋のホストだったら抽選を行う
            if (PhotonNetwork.IsMasterClient) {
                //先に作ってあるパネルリストから答えを作成
                int randomVal = UnityEngine.Random.Range(0, dropPanelList.Count);
                TrueAnswerPanel = dropPanelList[randomVal].GetPanelVariation();

                Debug.Log("オンライン：MasterClientがパネル再抽選を開始");
                photonView.RPC(nameof(SetAnswerPanel), RpcTarget.All,(int)TrueAnswerPanel);
            }
        }
        // オフライン時の処理
        else {
            //先に作ってあるパネルリストから答えを作成
            int randomVal = UnityEngine.Random.Range(0, dropPanelList.Count);
            TrueAnswerPanel = dropPanelList[randomVal].GetPanelVariation();
            Debug.Log("オフライン：パネル再抽選を直接開始");
            SetAnswerPanel((int) TrueAnswerPanel);
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
        for (int i = 0,max = dropPanelList.Count; i < max;i++) {
            
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
        //↑有能なAI君が書いてくれたよ！

        //シャッフル
        RandomVariationList = RandomVariationList.OrderBy(x => UnityEngine.Random.value).ToArray();
        return RandomVariationList;
    }


    /// <summary>
    /// 答えのパネルをセット
    /// </summary>
    /// <param name="answerInt"></param>
    [PunRPC]
    private void SetAnswerPanel(int answerInt) {
        //答えの設定
        TrueAnswerPanel = (DropGamePanelVariation) answerInt;
        //UIに表示
        NextVariationImage.sprite = GetSpriteFromVariation((DropGamePanelVariation) answerInt);
    }

    /// <summary>
    /// プレイヤーのポイントをUIに反映
    /// </summary>
    /// <param name="player"></param>
    public void SetPointUI(DropPlayer player) {
        //プレイヤーのポイントをUIのほうに反映
        if(!GameManager.instance.IsOnline())
            pointUIList[player.GetMyNumber()].transform.GetChild(POINT_TEXT_INDEX).GetComponent<TextMeshProUGUI>().text = player.GetPoint().ToString();
    }

    /// <summary>
    /// 何回したかを返す
    /// </summary>
    /// <returns></returns>
    public int GetGameCount() {
        return gameCount;
    }

    /// <summary>
    /// 何回やったかをセット
    /// </summary>
    /// <param name="count"></param>
    public void SetGameCount(int count) {
        gameCount = count;
    }

    /// <summary>
    /// プレイヤーのポイント数をセット(全体に共有するアイテムなのでよろしい)
    /// </summary>
    /// <param name="player"></param>
    public void SetPoint(DropPlayer player) {
        for(int i = 0; i < dropperList.Count; i++) {
            if (dropperList[i] != player) continue;
            
            //UIにもセットする
            SetPointUI(dropperList[i]);

        }
    }

    /// <summary>
    /// 各プレイヤーのカストムプロパティからスコアを再集計
    /// </summary>
    public void UpdateAllScore() {
        Debug.Log("[DropGameManager]スコア集計完了");

        //集計結果をルーム全体のCustomPropertiesに書き込む
        if (PhotonNetwork.IsMasterClient) {
            SyncScoreToRoom();
        }
    }

    /// <summary>
    /// ルーム全体のカスタムプロパティに反映
    /// </summary>
    private void SyncScoreToRoom() {
        Hashtable roomProps = new Hashtable();

       
        //カスタムプロパティに反映
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        Debug.Log("[DropGameManager] スコアをルームに同期レました");
    }

    /// <summary>
    /// 誰かが入室したときに呼ばれる
    /// </summary>
    /// <param name="newPlayer"></param>
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.Log($"[DropGameManager]{newPlayer.NickName}が入室しました");

        
        //MasterClientなら全スコアを再送
        if(PhotonNetwork.IsMasterClient) {
            SyncScoreToRoom();
        }
    }

    /// <summary>
    /// プレイヤーが退室したときに呼ばれる
    /// </summary>
    /// <param name="otherPlayer"></param>
    public override void OnPlayerLeftRoom(Player otherPlayer) {
        
        //マスタークライアントなら動機
        if (PhotonNetwork.IsMasterClient)
            SyncScoreToRoom();
    }

    /// <summary>
    /// DropPlayerのUIを更新 スコアが変更されたらUIに反映する
    /// </summary>
    void UpdateDropPlayerUI(int actorNumber, int score) {
        foreach (var dropper in dropperList) {
            if (dropper != null && dropper.GetComponent<PhotonView>().Owner.ActorNumber == actorNumber) {
                // DropPlayerのSetPointメソッドは使わずにUIだけ更新する
                // myPointはprivate setなので直接変更できないため、UIのみ更新
                SetPointUI(dropper);
                break;
            }
        }
    }

    
    /// <summary>
    /// ユーザー本人のドロッププレイヤーを渡す
    /// </summary>
    /// <returns></returns>
    public DropPlayer GetMyPlayer() {
        DropPlayer dropPlayer = null;
        if (dropperList.Count == 0)
            return null;
        for (int i = 0; i < dropperList.Count; i++) {
            if (dropperList[i].GetComponent<PhotonView>().IsMine)
                dropPlayer = dropperList[i];
        }

        return dropPlayer;
    }

}
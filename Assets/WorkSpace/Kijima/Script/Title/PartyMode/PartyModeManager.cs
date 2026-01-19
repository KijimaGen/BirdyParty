/**
 * @file PartyModeManager.cs
 * @brief パーティモードの管理者
 * @author Sum1r3
 * @date 2026/01/14
 */
using Cysharp.Threading.Tasks;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class PartyModeManager : SystemObject {
    //抽選されたシーン
    private List<string> ChoicedSceneList = new List<string>();
    //今何個目のゲームをやっているか
    public int NowGameIndex { get; private set; }
    //プレイヤーのランキングリスト(必要かどうかは不明)
    private List<PlayerInfomation> playerRankingList = new List<PlayerInfomation>();
    //一応こいつにも何回抽選を行うかを保存してもらう
    private int GameChoiceCount = 3; //<-一時的に三つにしておく

    //ゲームをランダム抽選してくれる奴
    [SerializeField]
    private SystemObject gamePickerPrefab;
    //自身のインスタンス
    public static PartyModeManager instance;
    //フォントんびゅーの参照
    PhotonView pv;

    private void Start() {
        _ = Initialize();
    }

    //初期化処理
    public override async UniTask Initialize() {
        //インスタンスを作成
        instance = this;
        //NowGameIndexの初期化
        NowGameIndex = 0;
        // ゲーム選択アイテム生成
        SystemObject createObject = Instantiate(gamePickerPrefab, transform);
        // 初期化
        await createObject.Initialize();
        //フォトンビューの参照の取得
        pv = GetComponent<PhotonView>();
        //自身を非破壊オブジェクトに
        DontDestroyOnLoad(gameObject);
        //UniTaskの使命
        await UniTask.CompletedTask;
    }
    
    /// <summary>
    /// ゲームリストの作成
    /// </summary>
    public void MakeGameList() {
        //何回抽選を行うかを設定
        PartyModeGamePicker.instance.SetRandomCount(GameChoiceCount);
        //選ばれたゲームのリストをもらう
        ChoicedSceneList = PartyModeGamePicker.instance.BuildGameIndexs();
        for(int i = 0,max = ChoicedSceneList.Count; i < max; i++) {
            Debug.Log("選ばれたシーン名" + ChoicedSceneList[i]);
        }

        //オンラインだったら
        if (!GameManager.instance.IsOnline()) return;
        //オンラインカツ自分がホストだったら
        if (!PhotonNetwork.IsMasterClient) return;
        //全体プレイヤーに抽選結果の上書きを要請
        pv.RPC(nameof(SetChoicedGameList), RpcTarget.All, ChoicedSceneList.ToArray());

    }

    /// <summary>
    /// 指定された引数でシーンのリストを上書き
    /// </summary>
    /// <param name="ChoicedGameList">
    /// 上書きするリスト
    /// </param>
    [PunRPC]
    public void SetChoicedGameList(string[] ChoicedGameList) {

        //一度リストを初期化
        ChoicedSceneList.Clear();

        //綺麗になったリストにシーンを増やしていく
        for (int i = 0,max = ChoicedGameList.Length;i < max;i++) {
            ChoicedSceneList.Add(ChoicedGameList[i]);
        }
    }

    /// <summary>
    /// 決められているゲームのリストを渡す
    /// </summary>
    public List<string> GetChoicedGameList() {
        return ChoicedSceneList;
    }

    /// <summary>
    /// 今何個目のゲームをやっているのか加算
    /// </summary>
    public void IncreaseNowGameIndex() {
        NowGameIndex++;
    }
}

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
    private int NowGameIndex;
    //プレイヤーのランキングリスト(必要かどうかは不明)

    //一応こいつにも何回抽選を行うかを保存してもらう
    private int GameChoiceCount = 3; //<-一時的に三つにしておく

    //ゲームをランダム抽選してくれる奴
    [SerializeField]
    private SystemObject gamePickerPrefab;
    //自身のインスタンス
    public static PartyModeManager instance;
    //初期化処理
    public override async UniTask Initialize() {
        //インスタンスを作成
        instance = this;

        // ゲーム選択アイテム生成
        SystemObject createObject = Instantiate(gamePickerPrefab, transform);
        // 初期化
        await createObject.Initialize();

        //デバッグでゲームリストを作成
        MakeGameList();

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
        photonView.RPC(nameof(SetChoicedGameList), RpcTarget.All);

    }

    /// <summary>
    /// 指定された引数でシーンのリストを上書き
    /// </summary>
    /// <param name="ChoicedGameList">
    /// 上書きするリスト
    /// </param>
    [PunRPC]
    public void SetChoicedGameList(List<string> ChoicedGameList) {
        for(int i = 0,max = ChoicedGameList.Count;i < max;i++) {
            ChoicedSceneList.Add(ChoicedGameList[i]);
        }
        Debug.Log("リストを上書きしました");
        for (int i = 0, max = ChoicedSceneList.Count; i < max; i++) {
            Debug.Log("選ばれたシーン名" + ChoicedSceneList[i]);
        }
    }

}

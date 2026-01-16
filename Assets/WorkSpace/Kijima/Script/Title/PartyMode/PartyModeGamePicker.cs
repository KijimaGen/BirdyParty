/**
 * @file PartyModeGamePicker.cs
 * @brief パーティモードのゲーム選択
 * @author Sum1r3
 * @date 2026/01/13
 */
using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyModeGamePicker : SystemObject{
    //ゲーム名のシーンの配列
    private string[] gameSceneNames = new string[] { "Race", "DropBird" , "DiceGame" };
    //どのゲームを選んだかの配列
    private List<int> gameIndexs;
    //何回抽選を行うか
    private int randomCount;
    //一応インスタンス
    public static PartyModeGamePicker instance;


    /// <summary>
    /// 最初に行う処理
    /// </summary>
    public override async UniTask Initialize() {
        //オンラインかつマスターじゃなかったら破壊
        if (GameManager.instance.IsOnline()) {
            //マスターだったらreturn
            if (PhotonNetwork.IsMasterClient) return;
            Destroy(gameObject);
        }

        //インスタンスの作成
        instance = this;
        //UniTaskの使命
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 数あるシーンの中から一つ選出
    /// </summary>
    public int PickUpIndex() {
        //番号を配列の長さの中から一つ選出
        int pickNumber;
        pickNumber = Random.Range(0, gameSceneNames.Length);

        return pickNumber;
    }

    /// <summary>
    /// 指定された回数分ゲームをランダムに選出する
    /// </summary>
    /// <param name="PickUpCount">
    /// 抽選を行う回数
    /// </param>
    public List<string> BuildGameIndexs() {
        //被り無しのリストを作成
        var cashIndexs = new HashSet<int>();

        //指定された数分埋まるまで抽選
        while(cashIndexs.Count < randomCount) {
            int randomIndex = PickUpIndex();
            cashIndexs.Add(randomIndex);
        }

        //値を変換してから返す
        List<int> returnIndexs = new List<int>(cashIndexs);
        //数値型から文字列型に変換してから渡す
        List<string> returnSceneNames = new List<string>(returnIndexs.Count);
        //順々に変換
        for(int i = 0,max  = returnIndexs.Count;i<max;i++) {
            returnSceneNames.Add(null);
            returnSceneNames[i] = gameSceneNames[returnIndexs[i]];
        }
        //変換し終わったものを引き渡す
        return returnSceneNames;
    }

    /// <summary>
    /// 文字列型を受け取ってシーン遷移
    /// </summary>
    /// <param name="sceneName">
    /// 移動先のシーン名
    /// </param>
    public void ChangeSceneWithName(string sceneName) {
        //オンラインかオフライン化で処理を変更
        if(GameManager.instance.IsOnline()) {
            PhotonNetwork.LoadLevel(sceneName);
        }else {
            SceneManager.LoadScene(sceneName);
        }

    }

    /// <summary>
    /// 何回行うかをセット
    /// </summary>
    /// <param name="setCount"></param>
    public void SetRandomCount(int setCount) {
        randomCount = setCount;
    }

    /// <summary>
    /// デバッグチェック
    /// </summary>
    public void CheckGameList() {
        for(int i = 0,max = gameIndexs.Count; i < max; i++) {
            Debug.Log("選ばれたゲームは : "+ gameSceneNames[gameIndexs[i]]);
        } 
    }

    
}

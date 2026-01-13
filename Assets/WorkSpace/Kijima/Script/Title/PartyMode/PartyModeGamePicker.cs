/**
 * @file PartyModeGamePicker.cs
 * @brief パーティモードのゲーム選択
 * @author Sum1r3
 * @date 2026/01/13
 */
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyModeGamePicker : MonoBehaviourPunCallbacks{
    //ゲーム名のシーンの配列
    private string[] gameSceneNames = new string[] { "Race", "DropBird" , "DiceGame" };
    //どのゲームを選んだかの配列
    private List<int> gameIndexs;
    //何回抽選を行うか
    private int randomCount;

    private void Start() {
        Initialize();
    }

    /// <summary>
    /// 最初に行う処理
    /// </summary>
    public void Initialize() {
        //オンラインかつマスターじゃなかったら破壊
        if (GameManager.instance.IsOnline()) {
            //マスターだったらreturn
            if (PhotonNetwork.IsMasterClient) return;
            Destroy(gameObject);
        }
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
    public void BuildGameIndexs(int PickUpCount) {
        for(int i = 0; i < PickUpCount; i++) {
            //抽選する値をキャッシュ
            int randomIndex = PickUpIndex();
            //ゲームの選出リストに追加
            gameIndexs.Add(randomIndex);
        }
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
}

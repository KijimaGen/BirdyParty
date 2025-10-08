/**
 * @file RacePlayer.cs
 * @brief レースゲームのマネージャー
 * @author Sum1r3
 * @date 2025/10/03
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RaceManager : SystemObject{
    //ゲーム開始時のカウントダウン
    private const float GameStartCount = 3.0f;
    //順位
    private List<GameObject> Ranking = new List<GameObject>();
    //スタートしたか
    public bool isStart;
    //自身のインスタンス
    public static RaceManager instance;
    void Start(){
        
    }

    void Update(){
        
    }

    /// <summary>
    /// ゲーム開始時のカウントダウンをここで行う
    /// </summary>
    private async UniTask StartCountDown() {
        //プレイヤーが揃うまで待つ
        //カメラゲット
        Camera camera = Camera.main;
        while (camera.gameObject.GetComponent<RaceCameraController>().GetRacer() < 2) {
            await UniTask.DelayFrame(100);
        }


        _ = AudioManager.instance.PlaySE(2);
        await UniTask.Delay(3000);
        //スタート
        isStart = true;
        await UniTask.CompletedTask;
    }

    public override async UniTask Initialize() {
        instance = this;

        await FadeManager.instance.FadeIn();

        isStart = false;
        await StartCountDown();
        
    }

    /// <summary>
    /// ランキングに追加
    /// </summary>
    /// <param name="player"></param>
    public void AddRanking(GameObject player) {
        Ranking.Add(player);
    }

    /// <summary>
    /// 特定のゲームオブジェクトがランキングのどこにいるかを返す
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public int GetRankingCount(GameObject player) {
        return Ranking.IndexOf(player);
    }
}

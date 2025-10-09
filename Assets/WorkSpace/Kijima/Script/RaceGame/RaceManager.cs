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

    //各プレイヤーを取っておく
    private List<RacePlayer> racers = new List<RacePlayer>();

    //各プレイヤーの開始位置
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-65f, 1.2f, 1f),
        new Vector3 (-65, 1.2f, -3.2f),
        new Vector3(-65, 1.2f, -7),
        new Vector3 (-65, 1.2f, -11)
    };



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
            //プレイヤーをスタートラインに置いておく
            PlayerStartPosSet();
            await UniTask.DelayFrame(100);
        }

        //プレイヤーをスタートラインに置いておく(OneMore)
        PlayerStartPosSet();

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

    /// <summary>
    /// レーサーのリストにプレイヤーを入れる
    /// </summary>
    /// <param name="player"></param>
    public void AddRacers(RacePlayer player) {
        racers.Add(player);
    }

    /// <summary>
    /// プレイヤーを最初の位置に着かせる
    /// </summary>
    public void PlayerStartPosSet() {
        for (int i = 0, max = racers.Count; i < max; i++) {
            if (racers[i] != null || racers != null)
                racers[i].SetPosition(spawnPositions[i]);
        }
    }

    /// <summary>
    /// 自分が何番目に入ってきたプレイヤーかを渡す
    /// </summary>
    /// <param name="racePlayer"></param>
    /// <returns></returns>
    public int GetPlayerNumber(RacePlayer racePlayer) {
        return racers.IndexOf(racePlayer);
    }

}

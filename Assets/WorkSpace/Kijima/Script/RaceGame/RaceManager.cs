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
    private List<GameObject> Ranking;
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
        float time = GameStartCount;
        _ = AudioManager.instance.PlaySE(2);
        while(time > 0) {
            time -= Time.deltaTime;
        }
        //スタート
        isStart = true;
        await UniTask.CompletedTask;
    }

    public override UniTask Initialize() {
        instance = this;


        isStart = false;
        _ = StartCountDown();
        return UniTask.CompletedTask;
    }
}

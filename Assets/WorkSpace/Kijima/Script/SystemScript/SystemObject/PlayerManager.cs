/**
 * @file PlayerManager.cs
 * @brief プレイヤーの情報を取っておきたい(唐突な願望)
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameConst;

public class PlayerManager : SystemObject {
    //自身のインスタンス
    public static PlayerManager instance;
    //プレイヤーたち
    [SerializeField]
    private List<PlayerInfomation> playerList = new List<PlayerInfomation>(PLAYER_MAX);

    public override async UniTask Initialize() {
        instance = this;
        //プレイヤーリストにPLAYER_MAX分nullを詰め込む
        playerList = Enumerable.Repeat<PlayerInfomation>(null, PLAYER_MAX).ToList();


        await UniTask.CompletedTask;
    }

    /// <summary>
    /// プレイヤーを追加
    /// </summary>
    /// <param Name="player"></param>
    public void AddPlayer(PlayerInfomation player) {
        //プレイヤーリストに追加
        for(int i = 0; i < playerList.Count; i++) {
            if (playerList[i] == null) {
                playerList[i] = player;
                return;
            }
        }
    }

    /// <summary>
    /// プレイヤーリストを引き渡す
    /// </summary>
    /// <returns></returns>
    public List<PlayerInfomation> GetPlayerList(){
        return playerList;
    }
}

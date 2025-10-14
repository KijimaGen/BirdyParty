/**
 * @file PlayerManager.cs
 * @brief プレイヤーの情報を取っておきたい(唐突な願望)
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameConst;

public class PlayerManager : SystemObject {
    //自身のインスタンス
    public static PlayerManager instance;
    //プレイヤーたち
    List<PlayerInfomation> playerList = new List<PlayerInfomation>(PLAYER_MAX);

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
        //一応最大数を超えそうだったら規制
        if (playerList.Count + 1 > PLAYER_MAX) return;
        //プレイヤーリストに追加
        playerList.Add(player);
    }

    public List<PlayerInfomation> GetPlayerList(){
        return playerList;

    }
}

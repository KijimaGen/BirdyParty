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
using static CommonModule;

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
    /// <param myName="player"></param>
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

    /// <summary>
    /// プレイヤーをもらって
    /// そのプレイヤーがリストの何番目なのかを渡す
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public int GetPlayerNumber(PlayerInfomation player) {
        return playerList.IndexOf(player);
    }

    /// <summary>
    /// プレイヤーに特定の処理を行わせる
    /// </summary>
    /// <param name="task"></param>
    public void ExeCuteAllPlayer(System.Func<PlayerInfomation, UniTask> task) {
        if (task == null || IsEmpty(playerList)) return;
        for (int i = 0, max = playerList.Count; i < max; i++) {
            if (playerList[i] == null) continue;
             task(playerList[i]);
        }
    }

    /// <summary>
    /// プレイヤーリストを一掃しながら破壊
    /// </summary>
    public void DestroyPlayerList() {
        // 後ろから削除する場合
        for (int i = playerList.Count - 1; i >= 0; i--) {
            if (playerList[i] == null) continue;

            PlayerInfomation player = playerList[i];
            playerList.RemoveAt(i);
            if (!GameManager.instance.IsOnline()) {
                // GameObjectを破壊
                Destroy(player.gameObject);
            }
            else {
                player.gameObject.SetActive(false);
            }
            
        }
    }
}

/**
 * @file PlayerManager.cs
 * @brief プレイヤーの情報を取っておきたい
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
    /// プレイヤーをリストから除外
    /// </summary>
    /// <param name="player"></param>
    public void RemovePlayer(PlayerInfomation player) {
        if(playerList.Contains(player)) {
            playerList.Remove(player);
        }

        //リストを再生性
        List<PlayerInfomation> newList = new List<PlayerInfomation>();
        //もともとあったリストから要素を入れてもらう
        for(int i = 0 , max = playerList.Count; i<max; i++) {
            if(playerList[i] != null)
                newList.Add(playerList[i]);
        }
        //要素数が4に届かなかったら足りない分追加
        if(newList.Count < 4) {
            //リストの不足分
            int ListMinus = PLAYER_MAX - newList.Count;

            //不足分nullを入れる
            for (int i = 0 ; i < ListMinus; i++) {
                newList.Add(null);
            }
        }
        //プレイヤーリストを新しいものと置き換えて完成
        playerList = newList;

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
    /// 全てのプレイヤーにエントリーさせる
    /// </summary>
    public void EntoriedAllPlayer() {
        for(int i = 0,max = playerList.Count ; i < max; i++) {
            if (playerList[i] != null)
            playerList[i].EntoriedPartyModeManager();
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

    /// <summary>
    /// playerリストのIndex番目に特定のポイントの加算処理を実行させる
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="point"></param>
    public void AddPointIndexPlayer(int playerIndex,int point) {
        int beforPoint = playerList[playerIndex].GetPoint();

        playerList[playerIndex].SetPoint(beforPoint+point);
    }

    public int PlayerCount => playerList == null ? 0 : playerList.Count;

    public int ActivePlayerCount => playerList == null ? 0 : playerList.Count(p => p != null);

    public List<int> GetActivePlayerIndices()
    {
        var list = new List<int>();
        if (playerList == null) return list;

        for (int i = 0; i < playerList.Count; i++)
            if (playerList[i] != null) list.Add(i);

        return list;
    }
}

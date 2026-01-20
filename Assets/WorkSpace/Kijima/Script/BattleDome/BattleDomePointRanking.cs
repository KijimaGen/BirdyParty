/**
 * @file BattleDomePointRanking.cs
 * @brief バトルドームの結果発表のランキング表示を行う
 * @author Eden_nu
 * @date 2026/1/20
 */
using System.Collections.Generic;
using UnityEngine;

public class BattleDomePointRanking : MonoBehaviour{
    
    //プレイヤーのランキングのリスト
    List<BattleDomePlayerScoreManager> playerRanking;

    /// <summary>
    /// ランキングを並び替える
    /// </summary>
    public void SortRank() {
        //先に元のリストに影響が出ないようにコピーを保存
        playerRanking = BattleDomePlayerManager.instance.GetPlayerScoreList();
        //コピーをmyPoint昇順で並び替え
        playerRanking.Sort((a,b) =>a.myPoint.CompareTo(b.myPoint));
    }

    /// <summary>
    /// 自身の持っているキャンバスに反映
    /// </summary>
    public void ShowRank() {

    }
}

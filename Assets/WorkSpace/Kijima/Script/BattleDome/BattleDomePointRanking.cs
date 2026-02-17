/**
 * @file BattleDomePointRanking.cs
 * @brief バトルドームの結果発表のランキング表示を行う
 * @author Eden_nu
 * @date 2026/1/20
 */
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleDomePointRanking : MonoBehaviour{
    
    //プレイヤーのランキングのリスト
    List<BattleDomePlayerScoreManager> playerRanking;
    //表示するランキングのテキスト
    [SerializeField]
    private TextMeshProUGUI _rankingText;
    //自身のインスタンス
    public static BattleDomePointRanking instance;

    private void Awake() {
        instance = this; 
    }

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
        if (playerRanking == null) return;

        //ランキングリストの素体を作成
        _rankingText.text = ("ランキング\n");

        for(int i = 0; i < playerRanking.Count; i++) {
            if (playerRanking[i] == null) return;

            //ランキングのテキストを作成
            _rankingText.text += ((i+1) +"位\n" + (playerRanking[i].myNumber + 1) + "P : " + playerRanking[i].myPoint+"\n");
        }
    }

    public List<BattleDomePlayerScoreManager> GetRankingList() {return playerRanking;}

    public int GetPlayerScoreRank(BattleDomePlayerScoreManager player) { return playerRanking.IndexOf(player); }
}

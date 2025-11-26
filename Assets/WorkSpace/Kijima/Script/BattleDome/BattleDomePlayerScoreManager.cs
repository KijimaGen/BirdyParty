/**
 * @file BattleDomePlayerScoreManager.cs
 * @brief プレイヤーの得点管理
 * @author Sum1r3
 * @date 2025/10/14
 */
using Photon.Pun;
using UnityEngine;
public class BattleDomePlayerScoreManager : MonoBehaviourPunCallbacks {
    //自身の持つ得点
    [SerializeField]
    private int myPoint = 0;

    /// <summary>
    /// ポイント加算
    /// </summary>
    /// <param name="point"></param>
    public void AddPoint(int point) {
        myPoint += point;
        //UIに変更の反映をする
    }
}

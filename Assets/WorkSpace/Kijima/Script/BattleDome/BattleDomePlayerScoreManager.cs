/**
 * @file BattleDomePlayerScoreManager.cs
 * @brief プレイヤーの得点管理
 * @author Sum1r3
 * @date 2025/10/14
 */
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using static CommonModule;
public class BattleDomePlayerScoreManager : MonoBehaviourPunCallbacks {
    //自身の持つ得点
    //[SerializeField]
    public int myPoint = 0;
    

    /// <summary>
    /// ポイント加算
    /// </summary>
    /// <param name="point"></param>
    public void AddPoint(int point) {
         // 自分以外のプレイヤーは操作禁止

        int newScore = myPoint + point;

        myPoint = newScore;

        SetScore(newScore);
    }

    /// <summary>
    /// ポイントを渡す
    /// </summary>
    /// <returns></returns>
    public int GetPoint() { return myPoint; }

    // 初期化
    private void Start() {
        // 自分のプレイヤーだけスコアを初期化
        if (photonView.IsMine) {
            SetScore(0);
        }

        BattleDomePlayerManager.instance.ScoreEnty(this);
    }

    // 内部でCustomPropertiesを更新（全員に共有される）
    private void SetScore(int newScore) {
        myPoint = newScore;

        // CustomPropertiesをセット
        Hashtable hash = new Hashtable();
        hash["point"] = myPoint;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        //オフラインだったら直にUI更新を呼び出す
        if (!GameManager.instance.IsOnline()) {
            ScoreboardUI.instance.RefreshUI();
        }

    }

    // PlayerのCustomPropertiesが変わった時に呼ばれるコールバック
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        // スコアが更新された時だけ処理
        if (changedProps.ContainsKey("point")) {
            // スコアボード更新
            ScoreboardUI.instance?.RefreshUI();
        }
    }
}

/**
 * @file DropGameScoreManager.cs
 * @brief ドロップゲームのプレイヤースコア管理(プレイヤー各々が持つ)
 * @author Sum1r3
 * @date 2025/11/10
 */
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class DropGameScoreManager : MonoBehaviourPunCallbacks {
    // 自分のスコアキャッシュ（CustomPropertiesにも保持される）
    private int myScore = 0;

    // 初期化
    private void Start() {
        // 自分のプレイヤーだけスコアを初期化
        if (photonView.IsMine) {
            SetScore(0);
        }
    }

    // 外部からスコア加算
    public void AddScore(int amount) {
        if (!photonView.IsMine) return; // 自分以外のプレイヤーは操作禁止


        int newScore = myScore + amount;
        SetScore(newScore);
    }

    // 内部でCustomPropertiesを更新（全員に共有される）
    private void SetScore(int newScore) {
        myScore = newScore;

        // CustomPropertiesをセット
        Hashtable hash = new Hashtable();
        hash["score"] = myScore;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

    }

    // PlayerのCustomPropertiesが変わった時に呼ばれるコールバック
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        // スコアが更新された時だけ処理
        if (changedProps.ContainsKey("score")) {
            int updatedScore = (int) changedProps["score"];
            //スコアボードにも更新してもらう
            DropGameManager.instance.UpdateDropPlayerUI(gameObject.GetComponent<PhotonView>().Owner.ActorNumber,updatedScore);
            // スコアボード更新
            DropgameScoreboardUI.Instance?.RefreshUI();
        }
    }

    // 自分のスコアを取得（UIなどで使う）
    public int GetMyScore() {
        return myScore;
    }

}

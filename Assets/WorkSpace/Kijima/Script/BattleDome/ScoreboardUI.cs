/**
 * @file ScoreboardUI.cs
 * @brief プレイヤーの得点表示
 * @author Sum1r3
 * @date 2025/10/27
 */
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class ScoreboardUI : MonoBehaviourPunCallbacks {
    [SerializeField] private List<TMP_Text> scoreTexts; // Inspectorでプレイヤー人数分登録
    public static ScoreboardUI instance;

    //すぐに呼ぶ処理
    private void Awake() {
        instance = this;
    }

    /// <summary>
    /// 部屋参加時に呼ばれる
    /// </summary>
    /// <param name="newPlayer"></param>
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        RefreshUI();
    }

    /// <summary>
    /// 部屋から抜けたタイミングで呼ばれる
    /// </summary>
    /// <param name="otherPlayer"></param>
    public override void OnPlayerLeftRoom(Player otherPlayer) {
        RefreshUI();
    }

    /// <summary>
    /// 各プレイヤーのカスタムプロパティに変更があったときに呼ばれる
    /// </summary>
    /// <param name="targetPlayer"></param>
    /// <param name="changedProps"></param>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) {
        if (changedProps.ContainsKey("score"))
            //UI更新
            RefreshUI();
    }

    /// <summary>
    /// UIの更新
    /// </summary>
    public void RefreshUI() {
        // すべてのテキストを一旦空にする（古いデータが残らないように）
        foreach (var text in scoreTexts) {
            if (text != null)
                text.text = "";
        }

        // 現在のプレイヤー一覧を取得
        if (GameManager.instance.IsOnline()) {
            var players = PhotonNetwork.PlayerList;

            // プレイヤー順にスコアをUIへ反映
            for (int i = 0; i < players.Length; i++) {
                if (i >= scoreTexts.Count)
                    break; // UI側が足りない場合は止める

                var player = players[i];
                int score = 0;

                if (player.CustomProperties.ContainsKey("point"))
                    score = (int) player.CustomProperties["point"];

                // 例: Player1: 100
                scoreTexts[i].text = $"{player.NickName}: {score}";
            }
        }
        else {
            var players = BattleDomePlayerManager.instance.GetPlayerScoreList();

            // プレイヤー順にスコアをUIへ反映
            for (int i = 0; i < players.Count; i++) {
                if (i >= scoreTexts.Count)
                    break; // UI側が足りない場合は止める

                var player = players[i];
                int score = 0;


                score = player.myPoint;

                // 例: Player1: 100
                scoreTexts[i].text = $"{i}P : {score}";
            }
        }
    }
}

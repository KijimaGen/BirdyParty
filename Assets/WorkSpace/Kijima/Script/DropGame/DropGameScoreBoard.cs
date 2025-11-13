using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class DropgameScoreboardUI : MonoBehaviourPunCallbacks {
    [SerializeField] private List<TMP_Text> scoreTexts; // Inspectorでプレイヤー人数分登録
    public static DropgameScoreboardUI Instance;

    private void Awake() {
        Instance = this;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        RefreshUI();
    }

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
        var players = PhotonNetwork.PlayerList;

        // プレイヤー順にスコアをUIへ反映
        for (int i = 0; i < players.Length; i++) {
            if (i >= scoreTexts.Count)
                break; // UI側が足りない場合は止める

            var player = players[i];
            int score = 0;

            if (player.CustomProperties.ContainsKey("score"))
                score = (int) player.CustomProperties["score"];

            // 例: Player1: 100
            scoreTexts[i].text = $" {score}";
        }
    }
}

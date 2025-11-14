using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class DiceScoreManager : MonoBehaviourPunCallbacks
{
    // 自分のスコアキャッシュ（CustomPropertiesにも保持される）
    private int diceScore = 0;

    // 初期化
    private void Start()
    {
        // 自分のプレイヤーだけスコアを初期化
        if (photonView.IsMine)
        {
            SetScore(0);
        }
    }

    // 外部からスコア加算
    public void AddScore(int amount)
    {
        Debug.Log("ダイスゲームでオンラインでのスコア加算処理が呼ばれたよ");
        GameObject rootObject = transform.parent.gameObject;
        Debug.Log($"親: {rootObject.name}");

        
        if (!rootObject.GetComponent<PhotonView>().IsMine) return; // 自分以外のプレイヤーは操作禁止

        Debug.Log("ダイスゲームでオンラインでのスコア加算処理が呼ばれたよ");
        int newScore = diceScore + amount;
        SetScore(newScore);
    }

    // 内部でCustomPropertiesを更新（全員に共有される）
    private void SetScore(int newScore)
    {
        diceScore = newScore;

        // CustomPropertiesをセット
        Hashtable hash = new Hashtable();
        hash["diceScore"] = diceScore;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

    }

    // PlayerのCustomPropertiesが変わった時に呼ばれるコールバック
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // スコアが更新された時だけ処理
        if (changedProps.ContainsKey("diceScore"))
        {
            int updatedScore = (int) changedProps["diceScore"];
            //スコアボードにも更新してもらう
            
            // スコアボード更新
            DropgameScoreboardUI.Instance?.RefreshUI();
            Debug.Log("ダイスゲームでのスコア共有処理が呼ばれました");
            DebugLogDice();
        }
    }

    private void DebugLogDice() {
        Debug.Log($"{GetComponent<PhotonView>().ViewID}の得点は{diceScore}");
    }

    // 自分のスコアを取得（UIなどで使う）
    public int GetMyScore()
    {
        return diceScore;
    }
}

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class DiceScoreManager : MonoBehaviourPunCallbacks
{
    private int diceScore = 0;

    private void Start()
    {
        // 自分のプレイヤーだけスコアを初期化
        if (photonView.IsMine)
        {
            SetScore(0);
        }
    }

    [PunRPC]
    public void ReceiveScoreAddition(int amount)
    {
        Debug.Log($"[DiceScoreManager.RPC] スコア加算リクエストを受信: +{amount}");
        AddScore(amount);
    }

    public void AddScore(int amount)
    {
        if (!photonView.IsMine) return;

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

            // 1. 親階層からPlayerInputHandlerを見つけ、そのPlayerInfoにアクセス
            PlayerInputHandler handler = GetComponentInParent<PlayerInputHandler>();
            if (handler != null && handler.PlayerData != null)
            {
                // PlayerInfo の TotalScore を更新（ローカルな集計用）
                handler.PlayerData.TotalScore = updatedScore;

                // DiceGameManagerのUIを更新（通常時のスコアボードとリザルトの両方に対応）
                BABADiceGameManager gameManager = FindObjectOfType<BABADiceGameManager>();
                if (gameManager != null)
                {
                    gameManager.UpdateScoreUIs();
                }
            }

            Debug.Log("ダイスゲームでのスコア共有処理が呼ばれました。PlayerInfo.TotalScoreを同期しました。");
            DebugLogDice();
        }
    }

    private void DebugLogDice()
    {
        if (GameManager.instance == null)
        {
            Debug.LogWarning("DebugLogDice: GameManager.instance が null です。");
            return;
        }

        Debug.Log($"{GetComponent<PhotonView>().ViewID}の得点は{diceScore}");
    }

    // 自分のスコアを取得（UIなどで使う）
    public int GetMyScore()
    {
        return diceScore;
    }
}
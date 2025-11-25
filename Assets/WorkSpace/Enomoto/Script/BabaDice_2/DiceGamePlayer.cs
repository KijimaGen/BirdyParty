using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

/**
 * @file DiceGamePlayer.cs
 * @brief PlayerInfomationの子オブジェクト(dicePlayer)にアタッチされるクラス
 * プレイヤーの入力（ボタン押下）を検知し、DiceGameManagerやDiceControllerに伝達する
 */
public class DiceGamePlayer : MonoBehaviour
{
    private PlayerInfomation playerInfo;

    void Start()
    {
        // 親オブジェクトからPlayerInfomationを取得
        playerInfo = GetComponentInParent<PlayerInfomation>();
        if (playerInfo == null)
        {
            Debug.LogError("DiceGamePlayer: PlayerInfomation parent not found!");
        }

        Debug.Log($"DiceGamePlayer for Player {playerInfo.GetMyNumber()} initialized.");
    }

    /// <summary>
    /// 【InputSystem連携】「RollDice」アクションが呼ばれたときに実行されるメソッド
    /// InputSystemのAction Map "DiceGame" に設定されている必要があります。
    /// </summary>
    /// <param name="context"></param>
    public void OnRollDice(InputAction.CallbackContext context)
    {
        // PressまたはButton Downの瞬間のみ実行
        if (!context.performed) return;

        if (playerInfo == null || playerInfo.GetComponent<PhotonView>() == null || !playerInfo.GetComponent<PhotonView>().IsMine)
        {
            // オンラインで自分の入力ではない、または情報が取得できていない場合は処理しない
            return;
        }

        // ログが出力されれば、入力自体は成功しています
        Debug.Log($"Input Received: Player {playerInfo.GetMyNumber()} attempts to roll dice.");

        // 1. 自分のダイスを取得 (DiceGameManagerが生成・管理しているはず)
        if (DiceGame_GameManager.instance == null)
        {
            Debug.LogError("DiceGameManager instance is null!");
            return;
        }

        DiceController myDice = DiceGame_GameManager.instance.GetDiceForPlayer(playerInfo.GetMyNumber());
            
        if (myDice != null)
        {
            // 2. ダイスが転がっているか確認
            if (!myDice.IsRolling)
            {
                // 3. 全クライアントでダイスを振るRPCを呼び出す
                myDice.photonView.RPC(nameof(DiceController.RollDice), RpcTarget.All);
                Debug.Log($"RPC called for Dice {playerInfo.GetMyNumber()}.");
            }
            else
            {
                // 既に結果が出ている、または転がっている場合
                Debug.Log("Dice is currently rolling. Cannot roll yet.");
            }
        }
        else
        {
            Debug.LogError($"DiceController for Player {playerInfo.GetMyNumber()} not found in DiceGameManager.");
        }
    }
}

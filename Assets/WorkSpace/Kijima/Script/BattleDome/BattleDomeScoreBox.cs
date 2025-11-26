/**
 * @file BattleDomePlayerScoreManager.cs
 * @brief プレイヤーの得点管理
 * @author Sum1r3
 * @date 2025/10/14
 */
using UnityEngine;

public class BattleDomeScoreBox : MonoBehaviour{
    [SerializeField]
    private int myNumber;

    private void OnTriggerEnter(Collider other) {
        //ボールに触れた時
        if(other.tag == "Ball") {
            //ゲームプレイヤーマネージャーに得点加算処理呼び出しを依頼
            BattleDomePlayerManager.instance.AddScoreReqest(1, myNumber);
            Destroy(other.gameObject);
        }
    }
}

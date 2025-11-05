/**
 * @file DropGameFrame.cs
 * @brief フレーム側を動かす処理
 * @author Sum1r3
 * @date 2025/10/20
 */

using UnityEngine;

public class DropGameFrame : MonoBehaviour{
    [SerializeField]
    private float moveSpeed = 3;
    //スタートポジション
    private Vector3 StartPos = new Vector3(0,-10,17.5f);
    //再抽選Y
    private const float reLotteryY = 65;
    void Start(){
        //再出現する位置を定義
        StartPos = transform.position;
    }

    void Update(){
        if (!DropGameManager.instance.isStart)
            return;
        Move();

        //位置再調整
        if(transform.position.y > reLotteryY) {
            DropGameManager.instance.SetGameCount(DropGameManager.instance.GetGameCount() + 1);

            DropGameManager.instance.LotteryAnswerVariation();
        }
    }

    /// <summary>
    /// 動き処理
    /// </summary>
    private void Move() {
        //移動量
        Vector3 moveVal = Vector3.zero;
        //移動量の設定
        
        moveVal.y = 1;

        //移動の反映
        transform.position += moveVal * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 初期位置に戻る
    /// </summary>
    [ContextMenu("ポジションをリセット")]
    public void ResetPos() {
        transform.position = StartPos;
    }
}

/**
* @file BattleDomePlayer.cs
* @brief バトルドームシーンのプレイヤー
* @author Sum1r3
* @date 2025/11/26
*/
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleDomePlayer : MonoBehaviourPunCallbacks {
    //Higneコンポーネントの参照
    [Header("Hingeのステータス")]
    [SerializeField]
    private float spring = 40000;
    [SerializeField]
    private float openAngle = 60;   //開く角度
    [SerializeField]
    private float closeAngle = 0;   //閉じる角度

    //HingeJoint
    private HingeJoint hjL; //AxisL
    private HingeJoint hjR; //AxisR

    //JointSpring
    private JointSpring jR; //AxisR
    private JointSpring jL; //AxisL

    //AxisLとAxisR
    [Header("参照を持ちたいオブジェクト")]
    [SerializeField]
    private GameObject _axisL;
    [SerializeField]
    private GameObject _axisR;

    //マイナンバー
    private int myNumber;

    private void Awake() {
        //ヒンジジョイントを受け取る
        hjL = _axisL.GetComponent<HingeJoint>();
        hjR = _axisR.GetComponent<HingeJoint>();

        //スプリングを受け取る
        jL = hjL.spring;
        jR = hjR.spring;

        //エントリー
        _=EntryToManager();
    }

    /// <summary>
    /// 左フリップの入力検知
    /// </summary>
    /// <param name="context"></param>
    public void FlipLeft(InputAction.CallbackContext context) {
        //ぬるちぇっく
        if (hjL == null || hjR == null) return;
        switch (context.phase) {
            //HingeJointを取ってSpringの値を弄り、動かす
            //押している間
            case InputActionPhase.Performed:
                jL.spring = spring;
                jL.targetPosition = openAngle;
                hjL.spring = jL;
                break;
            //離した瞬間
            case InputActionPhase.Canceled:
                jL.spring = spring;
                jL.targetPosition = closeAngle;
                hjL.spring = jL;
                break;
        }
    }

    /// <summary>
    /// 右フリップの入力検知
    /// </summary>
    /// <param name="context"></param>
    public void FlipRight(InputAction.CallbackContext context) {
        //ぬるちぇっく
        if (hjL == null || hjR == null) return;

        switch (context.phase) {
            //HingeJointを取ってSpringの値を弄り、動かす
            //押している間
            case InputActionPhase.Performed:
                jR.spring = spring;
                jR.targetPosition = openAngle;
                hjR.spring = jR;
                break;
            //離した瞬間
            case InputActionPhase.Canceled:
                jR.spring = spring;
                jR.targetPosition = closeAngle;
                hjR.spring = jR;


                break;
        }
    }

    public async UniTask EntryToManager() {
        //プレイヤーマネージャーの参照がなかったら出来るまで待つ
        while(BattleDomePlayerManager.instance == null) {
            await UniTask.Delay(1);
        }

        //参照をキャッシュ
        BattleDomePlayerManager PlayerManager = BattleDomePlayerManager.instance;
        //エントリーさせてもらう
        PlayerManager.Enty(this);
        //マイナンバー取得
        myNumber = PlayerManager.GetPlayerNumber(this);
        //位置調整
        transform.position = PlayerManager.GetPlayerPosition(myNumber);
        //角度調整
        transform.rotation = Quaternion.Euler(PlayerManager.GetPlayerRotation(myNumber));
    }
}

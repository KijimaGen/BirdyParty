using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Flipper : MonoBehaviour {
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

    private void Awake() {
        //ヒンジジョイントを受け取る
        hjL = _axisL.GetComponent<HingeJoint>();
        hjR = _axisR.GetComponent<HingeJoint>();

        //スプリングを受け取る
        jL = hjL.spring;
        jR = hjR.spring;
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
}

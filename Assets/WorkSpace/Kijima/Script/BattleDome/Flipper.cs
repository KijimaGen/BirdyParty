using System.Threading;
using UnityEngine;

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
    private JointSpring jL; //AxisL
    private JointSpring jR; //AxisR

    //AxisLとAxisR
    [Header("参照を持ちたいオブジェクト")]
    [SerializeField]
    private GameObject _axisL;
    [SerializeField]
    private GameObject _axisR;

    private void Start() {
        //ヒンジジョイントを受け取る
        hjL = _axisL.GetComponent<HingeJoint>();
        hjR = _axisR.GetComponent<HingeJoint>();

        //スプリングを受け取る
        jL = hjL.spring;
        jR = hjR.spring;
    }

    void Update() {
        
    }

    /// <summary>
    /// 左フリップ着火
    /// </summary>
    public void OnFlipLeft() {
        jL.spring = spring;
        jL.targetPosition = openAngle;
        hjL.spring = jL;
    }

    /// <summary>
    /// 左フリップ鎮火
    /// </summary>
    public void OffFlipLeft() {
        jL.spring = spring;
        jL.targetPosition = closeAngle;
        hjL.spring = jL;
    }

    /// <summary>
    /// 右フリップ着火
    /// </summary>
    public void OnFlipRight() {
        jR.spring = spring;
        jR.targetPosition = openAngle;
        hjR.spring = jR;
    }

    /// <summary>
    /// 右フリップ鎮火
    /// </summary>
    public void OffFlipRight() {
        jR.spring = spring;
        jR.targetPosition = closeAngle;
        hjR.spring = jR;
    }

    public void FlipLeft(ContextCallback context) {

    }
}

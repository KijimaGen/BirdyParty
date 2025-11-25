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
        //左クリック
        if(Input.GetMouseButtonDown(0)) {
            jL.spring = spring;
            jL.targetPosition = openAngle;
            hjL.spring = jL;
        }

        if(Input.GetMouseButtonUp(0)){
            jL.spring = spring;
            jL.targetPosition = closeAngle;
            hjL.spring = jL;
        }


        //右クリック
        if (Input.GetMouseButtonDown(1)) {
            jR.spring = spring;
            jR.targetPosition = openAngle;
            hjR.spring = jR;
        }

        if (Input.GetMouseButtonUp(1)) {
            jR.spring = spring;
            jR.targetPosition = closeAngle;
            hjR.spring = jR;
        }
    }
}

/**
 * @file Flipper.cs
 * @brief バトルドーム！のシューゥゥゥゥ！する奴
 * @author Sum1r3
 * @date 2025/11/20
 */
using UnityEngine;

public class Flipper : MonoBehaviour{
    //ヒンジ
    HingeJoint hinge;
    //モーター
    JointMotor motor;
    //分かってない

    //弾くスピード
    [SerializeField]
    private float motorSpeed = 800f;
    //元の位置に戻るスピード
    [SerializeField]
    private float releaseSpeed = -500f;

    
    void Start(){
        Initialize();
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Initialize() {
        //各コンポーネントの取得
        hinge = GetComponent<HingeJoint>();
        motor = hinge.motor;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            OnFlip();
        }

        if(Input.GetKeyUp(KeyCode.Space)) {
            OffFlip();
        }
    }

    public void OnFlip() {
        motor = hinge.motor;
        motor.targetVelocity = motorSpeed;
        hinge.motor = motor;
    }

    public void OffFlip() {
        motor = hinge.motor;
        motor.targetVelocity = releaseSpeed;
        hinge.motor = motor;
    }
}

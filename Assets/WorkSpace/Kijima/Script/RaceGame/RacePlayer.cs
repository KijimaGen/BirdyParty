/**
 * @file RacePlayer.cs
 * @brief レースゲームのプレイヤー
 * @author Sum1r3
 * @date 2025/10/03
 */
using UnityEngine;
using UnityEngine.InputSystem;

public class RacePlayer : MonoBehaviour{
    //スピード
    const float SPEED = 0.001f;
    //Z軸の移動スピード
    const float ZSPEED = 0.01f;
    //インプットアクション
    PlayerContoroller inputActions;
    //コントローラーのLスティックからもらえる値
    Vector2 controllerLStickValue;

    void Start(){
        inputActions = new PlayerContoroller();
        //インプットアクションからコントローラーの入力をもらえるようにする
        inputActions.RaceGame.UpDown.performed += controllerLStickMove;
        inputActions.RaceGame.UpDown.canceled += controllerLStickDontMove;
        //↑
        inputActions.RaceGame.Up.performed += KeybordUp;
        inputActions.RaceGame.Up.canceled += KeybordDontUp;
        //↓
        inputActions.RaceGame.Down.performed += KeybordDown;
        inputActions.RaceGame.Down.canceled += KeybordDontDown;

        inputActions.Enable();
    }

    
    void Update(){
        //ゲームが始まっているかどうかを確認
        if (!RaceManager.instance.isStart)
            return;
        
        //動く
        Move();
    }

    /// <summary>
    /// 動く
    /// </summary>
    private void Move() {
        //移動量を作る
        Vector3 moveValue = Vector3.zero;
        //X軸の移動量
        moveValue.x = SPEED;
        //y軸は固定
        moveValue.y = 0;
        moveValue.z = controllerLStickValue.y * ZSPEED;
        //座標に反映
        transform.position += moveValue;

    }

    /// <summary>
    /// コントローラーのLスティックの入力を受け取る
    /// </summary>
    /// <param name="context"></param>
    public void controllerLStickMove(InputAction.CallbackContext context) {
        controllerLStickValue = context.ReadValue<Vector2>();
    }
    public void controllerLStickDontMove(InputAction.CallbackContext context) {
        controllerLStickValue = Vector2.zero;
    }

    //上げボタン
    public void KeybordUp(InputAction.CallbackContext context) {
        controllerLStickValue.y = 1.0f;
    }

    public void KeybordDontUp(InputAction.CallbackContext context) {
        controllerLStickValue.y = 0;
    }

    //下げボタン
    public void KeybordDown(InputAction.CallbackContext context) {
        controllerLStickValue.y = -1.0f;
    }
    public void KeybordDontDown(InputAction.CallbackContext context) {
        controllerLStickValue.y = 0;
    }
}

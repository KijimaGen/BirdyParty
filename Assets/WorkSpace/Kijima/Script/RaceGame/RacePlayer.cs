/**
 * @file RacePlayer.cs
 * @brief ���[�X�Q�[���̃v���C���[
 * @author Sum1r3
 * @date 2025/10/03
 */
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class RacePlayer : MonoBehaviour{
    //�X�s�[�h
    const float SPEED = 0.001f;
    //Z���̈ړ��X�s�[�h
    const float ZSPEED = 0.01f;
    //�C���v�b�g�A�N�V����
    PlayerContoroller inputActions;
    //�R���g���[���[��L�X�e�B�b�N������炦��l
    Vector2 controllerLStickValue;

    void Start(){
        inputActions = new PlayerContoroller();
        //�C���v�b�g�A�N�V��������R���g���[���[�̓��͂����炦��悤�ɂ���
        inputActions.RaceGame.UpDown.performed += controllerLStickMove;
        inputActions.RaceGame.UpDown.canceled += controllerLStickDontMove;
        //��
        inputActions.RaceGame.Up.performed += KeybordUp;
        inputActions.RaceGame.Up.canceled += KeybordDontUp;
        //��
        inputActions.RaceGame.Down.performed += KeybordDown;
        inputActions.RaceGame.Down.canceled += KeybordDontDown;

        inputActions.Enable();
    }

    
    void Update(){
        //����
        Move();
    }

    /// <summary>
    /// ����
    /// </summary>
    private void Move() {
        //�ړ��ʂ����
        Vector3 moveValue = Vector3.zero;
        //X���̈ړ���
        moveValue.x = SPEED;
        //y���͌Œ�
        moveValue.y = 0;
        moveValue.z = controllerLStickValue.y * ZSPEED;
        //���W�ɔ��f
        transform.position += moveValue;

    }

    /// <summary>
    /// �R���g���[���[��L�X�e�B�b�N�̓��͂��󂯎��
    /// </summary>
    /// <param name="context"></param>
    public void controllerLStickMove(InputAction.CallbackContext context) {
        controllerLStickValue = context.ReadValue<Vector2>();
    }
    public void controllerLStickDontMove(InputAction.CallbackContext context) {
        controllerLStickValue = Vector2.zero;
    }

    //�グ�{�^��
    public void KeybordUp(InputAction.CallbackContext context) {
        controllerLStickValue.y = 1.0f;
    }

    public void KeybordDontUp(InputAction.CallbackContext context) {
        controllerLStickValue.y = 0;
    }

    //�����{�^��
    public void KeybordDown(InputAction.CallbackContext context) {
        controllerLStickValue.y = -1.0f;
    }
    public void KeybordDontDown(InputAction.CallbackContext context) {
        controllerLStickValue.y = 0;
    }
}

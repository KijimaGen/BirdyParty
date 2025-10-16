/**
 * @file DropPlayer.cs
 * @brief �h���b�v�Q�[���̃v���C���[
 * @author Sum1r3
 * @date 2025/10/16
 */
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameConst;

[RequireComponent(typeof(Rigidbody))]
public class DropPlayer : MonoBehaviour {
    //�ړ����x
    private float moveSpeed = 8f;

    //���͒l
    private Vector2 moveInput;
    //�肬���ǃ{�f�B�̓���
    private Rigidbody rb;

    //�I��������ǂ���
    private bool isEnd;

    //���g�̖��O
    private string playerName;
    //���g�̔ԍ�
    public int myNumber { get; private set; }
    //���g�̏���
    public int myRank { get; private set; }

    //���g�̃t�H�g���r���[
    PhotonView photonView;
    private PlayerInput myInput;
    //���g�̏Փ˂̋���
    [SerializeField]
    private float bounceForce;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //���g�̃t�H�g���r���[�擾
        photonView = GetComponent<PhotonView>();
        //�J�����̎Q�ƂɎ��g������
        Camera.main.gameObject.GetComponent<DropGameCameraContoller>().AddTarget(this.transform);
        
        
        //���g�̔ԍ����擾
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        //���g�ɂ��Ă���L�����o�X�̏������������Ăяo��
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();

        //���g�̃C���v�b�g�V�X�e�����擾���A�A�N�V�����}�b�v��؂�ւ���
        myInput = GetComponent<PlayerInput>();
        myInput.SwitchCurrentActionMap(DROPGAME_ACTION_NAME);

        myInput.SwitchCurrentActionMap(DROPGAME_ACTION_NAME);

        //�n�܂�
        isEnd = false;

        //�ʒu���͂邩�V���
        Vector3 startpos = transform.position;
        startpos.y = 100;
        transform.position = startpos;
        
    }

    //�A�b�v�f�[�g
    void FixedUpdate() {
       
        //�J�n����܂œ����Ă͂Ȃ�Ȃ�
        //if (!DropGameManager.instance.isStart) {
        //    rb.velocity = Vector3.zero;
        //    return;
        //}

        //�ړ�
        if (photonView.IsMine && !isEnd)
            Move();

        //�S�[�����Ă���̂ɓ����Ă͂Ȃ�Ȃ�
        if (isEnd) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }


    }

    //�C���v�b�g�V�X�e���̓��͒l�̎󂯎��
    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// �ړ�
    /// </summary>
    private void Move() {


        // ���͒l�̎󂯎��
        float x = moveInput.x;
        float z = moveInput.y;
        float y = -1;

        // ���K�����Ȃ��ł��̂܂ܓK�p
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed;
    }

    

    /// <summary>
    /// �S�[�����܂���
    /// </summary>
    public void End() {
        isEnd = true;
    }


    private void OnTriggerEnter(Collider other) {
        
    }

    private void OnCollisionEnter(Collision collision) {
        if (photonView == null) return;
        if (!photonView.IsMine) return; //���l�̃v���C���[�ł͏������Ȃ�
        if (!collision.gameObject.CompareTag("Player")) return; //�v���C���[�ȊO�ɂԂ����Ă��������Ȃ�

        Rigidbody rb = GetComponent<Rigidbody>();
        Rigidbody otherRb = collision.rigidbody;

        if (otherRb == null) return;

        //����Ƃ̕����x�N�g�����擾
        Vector3 dir = (transform.position - collision.transform.position).normalized;

        //���g�Ƒ���̗����ɒ��˕Ԃ��^����
        rb.AddForce(dir * bounceForce, ForceMode.Impulse);
        otherRb.AddForce(-dir * bounceForce, ForceMode.Impulse);
    }

    //�v���X�{�^�����������Ƃ��Ƀz�X�g��������Q�[���J�n(���̂����Ȃ����\��)
    public void Plus(InputAction.CallbackContext context) {
        RaceManager_PUN.instance.TryStartCountDown();
    }

    /// <summary>
    /// �}�C�i���o�[�������n��
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber() {
        return photonView.Owner.ActorNumber - 1;
    }

    /// <summary>
    /// �ʒu�ړ�
    /// </summary>
    public void SetPosition(Vector3 pos) {
        transform.position = pos;
    }

    public int GetRank() {
        return myRank;
    }
}

/**
 * @file DropPlayer.cs
 * @brief ドロップゲームのプレイヤー
 * @author Sum1r3
 * @date 2025/10/16
 */
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameConst;

[RequireComponent(typeof(Rigidbody))]
public class DropPlayer : MonoBehaviour {
    //移動速度
    private float moveSpeed = 8f;

    //入力値
    private Vector2 moveInput;
    //りぎっどボディの入手
    private Rigidbody rb;

    //終わったかどうか
    private bool isEnd;

    //自身の名前
    private string playerName;
    //自身の番号
    public int myNumber { get; private set; }
    //自身の順位
    public int myRank { get; private set; }

    //自身のフォトンビュー
    PhotonView photonView;
    private PlayerInput myInput;
    //自身の衝突の強さ
    [SerializeField]
    private float bounceForce;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //自身のフォトンビュー取得
        photonView = GetComponent<PhotonView>();
        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<DropGameCameraContoller>().AddTarget(this.transform);
        
        
        //自身の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        //自身についているキャンバスの初期化処理を呼び出す
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();

        //自身のインプットシステムを取得し、アクションマップを切り替える
        myInput = GetComponent<PlayerInput>();
        myInput.SwitchCurrentActionMap(DROPGAME_ACTION_NAME);

        myInput.SwitchCurrentActionMap(DROPGAME_ACTION_NAME);

        //始まり
        isEnd = false;

        //位置をはるか天空へ
        Vector3 startpos = transform.position;
        startpos.y = 100;
        transform.position = startpos;
        
    }

    //アップデート
    void FixedUpdate() {
       
        //開始するまで動いてはならない
        //if (!DropGameManager.instance.isStart) {
        //    rb.velocity = Vector3.zero;
        //    return;
        //}

        //移動
        if (photonView.IsMine && !isEnd)
            Move();

        //ゴールしているのに動いてはならない
        if (isEnd) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }


    }

    //インプットシステムの入力値の受け取り
    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {


        // 入力値の受け取り
        float x = moveInput.x;
        float z = moveInput.y;
        float y = -1;

        // 正規化しないでそのまま適用
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed;
    }

    

    /// <summary>
    /// ゴールしました
    /// </summary>
    public void End() {
        isEnd = true;
    }


    private void OnTriggerEnter(Collider other) {
        
    }

    private void OnCollisionEnter(Collision collision) {
        if (photonView == null) return;
        if (!photonView.IsMine) return; //他人のプレイヤーでは処理しない
        if (!collision.gameObject.CompareTag("Player")) return; //プレイヤー以外にぶつかっても処理しない

        Rigidbody rb = GetComponent<Rigidbody>();
        Rigidbody otherRb = collision.rigidbody;

        if (otherRb == null) return;

        //相手との方向ベクトルを取得
        Vector3 dir = (transform.position - collision.transform.position).normalized;

        //自身と相手の両方に跳ね返りを与える
        rb.AddForce(dir * bounceForce, ForceMode.Impulse);
        otherRb.AddForce(-dir * bounceForce, ForceMode.Impulse);
    }

    //プラスボタンを押したときにホストだったらゲーム開始(そのうちなくす予定)
    public void Plus(InputAction.CallbackContext context) {
        RaceManager_PUN.instance.TryStartCountDown();
    }

    /// <summary>
    /// マイナンバーを引き渡す
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber() {
        return photonView.Owner.ActorNumber - 1;
    }

    /// <summary>
    /// 位置移動
    /// </summary>
    public void SetPosition(Vector3 pos) {
        transform.position = pos;
    }

    public int GetRank() {
        return myRank;
    }
}

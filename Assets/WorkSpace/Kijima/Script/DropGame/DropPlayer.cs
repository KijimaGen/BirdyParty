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
    [SerializeField]
    private float moveSpeed = 8f;

    //入力値
    private Vector2 moveInput;
    //りぎっどボディの入手
    private Rigidbody rb;

    //終わったかどうか
    private bool isEnd;

    
    //自身の番号
    public int myNumber { get; private set; }
    //自身の順位
    public int myRank { get; private set; }

    //自身のフォトンビュー
    PhotonView photonView;
    
    //自身の衝突の強さ
    [SerializeField]
    private float bounceForce;

    //マイポイント
    private int myPoint = 0;

    //固定ハイ
    private const float SET_Y = 100;

    void Start() {
        //途中参加は認めない
        if (DropGameManager.instance.isStart)
            this.gameObject.SetActive(false);


        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //自身のフォトンビュー取得
        photonView = GetComponent<PhotonView>();
        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<DropGameCameraContoller>().AddTarget(this.transform);
        
        
        //自身の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        //自身の持っているポイントを0にする
        myPoint = 0;
        
        //自身についているキャンバスの初期化処理を呼び出す
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();
        //エントリー
        DropGameManager.instance.AddDropper(this);
        

        //始まり
        isEnd = false;

        //位置をはるか天空へ
        Vector3 startpos = transform.position;
        startpos.y = SET_Y;
        transform.position = startpos;
        
    }

    //アップデート
    void FixedUpdate() {
       
        //開始するまで動いてはならない
        if (!DropGameManager.instance.isStart) {
            rb.velocity = Vector3.zero;
            return;
        }

        //移動
        if (photonView.IsMine && !isEnd)
            Move();

        //ゴールしているのに動いてはならない
        if (isEnd) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }

        //位置を天空に固定
        Vector3 startpos = transform.position;
        startpos.y = SET_Y;
        transform.position = startpos;

    }

   
    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {

        moveInput = GetComponentInParent<PlayerInfomation>().GetLeftStickValue();
        // 入力値の受け取り
        float x = moveInput.x;
        float z = moveInput.y;
        

        // 正規化しないでそのまま適用
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed * Time.deltaTime;
    }

    

    /// <summary>
    /// ゴールしました
    /// </summary>
    public void End() {
        isEnd = true;
    }


    private void OnTriggerEnter(Collider other) {
        
    }

    /// <summary>
    /// 他のプレイヤーを押し返す
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionEnter(Collision col) {
        //自分の物かPhotonViewはあるか確認
        if(photonView == null) return;
        if (!photonView.IsMine) return;

        if (col.gameObject.CompareTag("Player")) {
            Vector3 pushDir = (col.transform.position - transform.position).normalized;
            col.gameObject.GetComponent<PhotonView>()
                .RPC("ApplyPushBack", RpcTarget.All, pushDir, bounceForce); 
        }
    }

    [PunRPC]
    void ApplyPushBack(Vector3 dir, float power) {
        rb.AddForce(dir * power, ForceMode.Impulse);
    }

    //プラスボタンを押したときにホストだったらゲーム開始(そのうちなくす予定)
    public void Plus(InputAction.CallbackContext context) {
        DropGameManager.instance.TryStartCountDown();
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

    /// <summary>
    /// ポイント加算
    /// </summary>
    /// <param name="point"></param>
    public void AddPoint(int point) {
        myPoint += point;
        DropGameManager.instance.SetPointUI(this);
    }

    /// <summary>
    /// ポイントを上げる
    /// </summary>
    /// <returns></returns>
    public int GetPoint() {
        return myPoint;
    }

}

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
    // 移動速度
    [SerializeField]
    private float moveSpeed = 8f;

    // 入力値
    private Vector2 moveInput;
    // 剛体リジッドボディの参照
    private Rigidbody rb;

    // 終了したかどうか
    private bool isEnd;

    // 自分の番号
    public int myPhotonNumber { get; private set; }
    // 自分の順位
    public int myRank { get; private set; }

    // 自分のフォトンビュー
    PhotonView PV;
    
    // 自分の衝突の強さ
    [SerializeField]
    private float bounceForce;

    // マイポイント
    public int myPoint { get; private set; } = 0;

    // 固定ハイ
    private const float SET_Y = 100;

    // プレイヤーのニックネーム
    public string myName;

    //移動量
    Vector3 moveDir;

    void OnEnable() {
        // 途中参加は認めない
        //if (DropGameManager.instance.isStart)
        //    gameObject.SetActive(false);

        // rbの取得と設定
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //rb.isKinematic = true;
        // 自分のフォトンビュー取得
        PV = GetComponent<PhotonView>();
        // カメラの参照に自分を追加
        Camera.main.gameObject.GetComponent<DropGameCameraContoller>().AddTarget(this.transform);
        // エントリー
        DropGameManager.instance.AddDropper(this);

        // 自分の番号を取得
        myPhotonNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        // 自分の持っているポイントを0にする
        myPoint = 0;
        
        // 自分についているキャンバスの初期化処理を呼び出す
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();

        //マネージャーにポイントを反映してもらう
        DropGameManager.instance.SetPoint(this);

        // 始まり
        isEnd = false;

        // 位置を張るかシラ
        Vector3 startpos = transform.position;
        startpos.y = SET_Y;
        transform.position = startpos;

        //色の設定
        SetMyColor();

        //移動量初期化
        moveDir = Vector3.zero;
    }

    // アップデート
    void FixedUpdate() {
        // 開始するまで動いてはならない
        if (!DropGameManager.instance.isStart) {
            rb.velocity = Vector3.zero;
            return;
        }

        // 移動
        // オンラインで、自分のキャラのみ動かす
        if (PV.IsMine && !isEnd)
            Move();
        // 移動(オフライン時も動かす)
        if (!GameManager.instance.IsOnline() && !isEnd)
            Move();

        // ゴールしているのに動いてはならない
        if (DropGameManager.instance.isEnd) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }
        // 終わってたらキネマティックを切る也
        if (DropGameManager.instance.isEnd)
            rb.isKinematic = false;

    }

   
    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {
        //インプットの受け取り
        moveInput = GetComponentInParent<PlayerInfomation>().GetLeftStickValue();
        // 入力値の加算
        moveDir.x += moveInput.x /10;
        moveDir.z += moveInput.y /10;

        //上限の設定
        if (moveDir.x > 1f) moveDir.x = 1f;
        if (moveDir.z > 1f) moveDir.z = 1f;

        //移動量の反映
        rb.velocity = moveDir * moveSpeed * Time.deltaTime;

        if(moveDir.x > 0f)
            moveDir.x -= 0.01f;
        if(moveDir.x < 0f)
            moveDir.x += 0.01f;

        if (moveDir.z > 0f)
            moveDir.z -= 0.01f;
        if (moveDir.z < 0f)
            moveDir.z += 0.01f;
    }

    

    /// <summary>
    /// ゴールしました
    /// </summary>
    public void End() {
        isEnd = true;
        //パーティモードだったときに
        if (GameManager.instance.isPartyMode) {
            //プレイヤーインフォメーションに自身の順位に合わせた得点を加算してもらう
            //PlayerInfomation側の自身のポイント
            int myBeforePoint = GetComponentInParent<PlayerInfomation>().GetPoint();
            //自身の順位に合わせたポイント
            int myScorePoint = GameConst.PLAYER_SCORE_LIST[myRank];

            //二つを合わせたものを適用
            GetComponentInParent<PlayerInfomation>().SetPoint(myBeforePoint + myScorePoint);
            //デバッグ
            Debug.Log(GetComponentInParent<PlayerInfomation>().myNumber + "が" + GetComponentInParent<PlayerInfomation>().GetPoint() + "点になりました");
        }
    }

    /// <summary>
    /// 他のプレイヤーと当たり返す
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionEnter(Collision col) {
        //プレイヤーでなかったら、終わってたら処理しない
        if (col.gameObject.tag == PLAYER_TAG || isEnd)
            return;
       
        //オフラインだったらこっち
        if (!GameManager.instance.IsOnline()) {
            Vector3 pushDir = (col.transform.position - transform.position).normalized;
            //カス
            if (pushDir.x < 0f) moveDir.x = -1;
            if (pushDir.x > 0f) moveDir.x =  1;
            if (pushDir.z < 0f) moveDir.z = -1;
            if (pushDir.z > 0f) moveDir.z =  1;


            //ApplyPushBack(pushDir, bounceForce);
            return;
        }

        // 念のためPhotonViewはあるか確認
        if (PV == null) return;
        if (!PV.IsMine) return;

        // プレイヤーに当たったら跳ね返す
        if (col.gameObject.CompareTag(PLAYER_TAG)) {
            Vector3 pushDir = (col.transform.position - transform.position).normalized;
            if (pushDir.x < 0f) moveDir.x = -1;
            if (pushDir.x > 0f) moveDir.x = 1;
            if (pushDir.z < 0f) moveDir.z = -1;
            if (pushDir.z > 0f) moveDir.z = 1;
        }
    }
    /// <summary>
    /// オンラインで跳ね返す
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="power"></param>
    [PunRPC]
    void ApplyPushBack(Vector3 dir, float power) {
        if (rb == null) 
            rb = GetComponent<Rigidbody>();

        //力を加えるよん
        rb.AddForce(dir * power, ForceMode.Impulse);
    }

    

    #region 各ゲッターセッター
    /// <summary>
    /// マイナンバーを引き渡す
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber()
    {
        //親の番号渡したほうが確実
        return GetComponentInParent<PlayerInfomation>().GetMyNumber();
    }


    /// <summary>
    /// 位置移動
    /// </summary>
    public void SetPosition(Vector3 pos) {
        transform.position = pos;
    }

    /// <summary>
    /// ランキングをセット
    /// </summary>
    /// <returns></returns>
    public int GetRank() {
        return myRank;
    }
    /// <summary>
    /// 順位をセット
    /// </summary>
    /// <param name="rank"></param>
    public void SetRank(int rank) {
        myRank = rank;
    }

    

    /// <summary>
    /// ポイントセット(Addと別にしてよかったかもしれない)
    /// </summary>
    /// <param name="point"></param>
    public void SetPoint(int point) {
        myPoint = point;
        DropGameManager.instance.SetPoint(this);
    }

    /// <summary>
    /// ポイントを渡す
    /// </summary>
    /// <returns></returns>
    public int GetPoint() {
        return myPoint;
    }

    /// <summary>
    /// 名前の設定
    /// </summary>
    public void SetName(string newName) {
        // 名前を適用(一時的にカス)
        myName = newName;
    }

    /// <summary>
    /// 自身の色を渡す
    /// </summary>
    public Color GetMyColror(){
        return GetComponentInParent<PlayerInfomation>().GetMyColor();
    }


    #endregion

    /// <summary>
    /// 自身のいろをかえる
    /// </summary>
    public void SetMyColor() {
        Color myColor = GetComponentInParent<PlayerInfomation>().GetMyColor();
        foreach (Transform child in transform) {
            if (child.name == "LeftEye" || child.name == "RightEye") continue;
            if (child.name == "hat" || child.name == "Canvas") continue;
            if (child.name == "LeftReg" || child.name == "RightReg") continue;
            if (child.name == "UnderMouse" || child.name == "UpMouse") continue;

            child.GetComponent<Renderer>().material.color = myColor;
        }
    }

}

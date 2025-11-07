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
using ExitGames.Client.Photon;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
public class DropPlayer : MonoBehaviourPunCallbacks {
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
    public int myNumber { get; private set; }
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

    // プレイヤーのスコア(所有権用)

    // プレイヤーのニックネーム
    public string myName;

    void Start() {
        // 途中参加は認めない
        if (DropGameManager.instance.isStart)
            this.gameObject.SetActive(false);

        // rbの取得と設定
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        // 自分のフォトンビュー取得
        PV = GetComponent<PhotonView>();
        // カメラの参照に自分を追加
        Camera.main.gameObject.GetComponent<DropGameCameraContoller>().AddTarget(this.transform);
        
        
        // 自分の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        // 自分の持っているポイントを0にする
        myPoint = 0;
        
        // 自分についているキャンバスの初期化処理を呼び出す
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();
        // 名前を適用(一時的にカス)
        PlayerInfomation myInfo = GetComponentInParent<PlayerInfomation>();
        myName = 'P' + myInfo.myNumber.ToString();
        // エントリー
        DropGameManager.instance.AddDropper(this);
        

        // 始まり
        isEnd = false;

        // 位置を張るかシラ
        Vector3 startpos = transform.position;
        startpos.y = SET_Y;
        transform.position = startpos;
        


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
        if (isEnd) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }

        

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


    /// <summary>
    /// トリガーに入った時の処理 正解パネルに飛び込んだらスコア追加
    /// </summary>
    private void OnTriggerEnter(Collider other) {
        // 自分のプレイヤーのみ処理
        if (PV != null && !PV.IsMine) return;
        
        // 正解パネルに飛び込んだ場合
        if (other.CompareTag("CorrectPanel")) {
            // オンラインの場合はDropGameManagerのスコア管理システムを使用
            if (GameManager.instance.IsOnline()) {
                Player myPlayer = PV.Owner;
                DropGameManager.instance.AddPlayerScore(myPlayer, 100);
                Debug.Log("正解パネルに飛び込みました スコア100点追加");
            } 
            // オフラインの場合は直接myPointを更新
            else {
                myPoint += 100;
                DropGameManager.instance.SetPointUI(this);
                Debug.Log("オフライン 正解パネルに飛び込みました スコア100点追加");
            }
        }
    }

    /// <summary>
    /// 他のプレイヤーと当たり返す
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionEnter(Collision col) {
        if (GameManager.instance.IsOnline()) {
            
        }

        // 念のためPhotonViewはあるか確認
        if(PV == null) return;
        if (!PV.IsMine) return;

        // プレイヤーに当たったら跳ね返す
        if (col.gameObject.CompareTag(PLAYER_TAG)) {
            Vector3 pushDir = (col.transform.position - transform.position).normalized;
            col.gameObject.GetComponent<PhotonView>()
                .RPC(nameof(ApplyPushBack), RpcTarget.All, pushDir, bounceForce); 
        }
    }
    /// <summary>
    /// オンラインで跳ね返す
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="power"></param>
    [PunRPC]
    void ApplyPushBack(Vector3 dir, float power) {
        if (rb == null) {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null)
        rb.AddForce(dir * power, ForceMode.Impulse);
    }

    // プラスボタンが押されたときにホストがゲーム開始(今のところない可能性)
    public void Plus(InputAction.CallbackContext context) {
        DropGameManager.instance.TryStartCountDown();
    }


    #region 各ゲッターセッター
    /// <summary>
    /// マイナンバーを渡す
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber() {
        // オフライン対応
        if (!GameManager.instance.IsOnline()) { return 0; }
        return PV.Owner.ActorNumber - 1;
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
        // インターネットで加算
        AddScoreToOnline();
        DropGameManager.instance.SetPointUI(this);
    }

    /// <summary>
    /// ポイントを渡す
    /// </summary>
    /// <returns></returns>
    public int GetPoint() {
        return myPoint;
    }

    #endregion

    

    /// <summary>
    /// 自分のスコアをPhotonのCustomProoertiesに反映する
    /// </summary>
    private void AddScoreToOnline() {
        // 一時的なHashtableを作成
        // このhashtableはPhoton用で、System.Collections.Hashtableとは別物
        Hashtable props = new Hashtable();

        // キー名を"Point" + NickNameにして値を格納
        props[KEY_NAME_POINT + PhotonNetwork.LocalPlayer.NickName] = myPoint;

        // Player(自分)のCustomPropertiesを更新(これで全員に同期される)
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// 誰かがスコア更新を行ったときに呼ばれる処理
    /// </summary>
    /// <param name="targetPlayer"></param>
    /// <param name="changedProps"></param>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        // "Point"が含まれていたら実行
        if (changedProps.ContainsKey(KEY_NAME_POINT + targetPlayer.NickName)) {
            int updateScore = (int) changedProps[KEY_NAME_POINT + targetPlayer.NickName];
            Debug.Log($"[PlayerScore] {targetPlayer.NickName} のスコアが {updateScore} に更新されました");

            if (PhotonNetwork.IsMasterClient) {
                DropGameManager.instance.UpdateAllScore();
            }
        }
    }

    /// <summary>
    /// このオブジェクト用のユニークキーを生成する
    /// </summary>
    /// <returns>生成したキー</returns>
    public string GetUniqueKey() {
        // PhotonViewの所有者を取得
        Player owner = photonView.Owner;

        if (owner == null) {
            // 所有者がまだいない場合は警告を出す
            Debug.LogWarning("PhotonViewに所有者がまだ割り当てられていません");
            return null;
        }

        // NickName と UserId を組み合わせてキーを生成
        // こうすることで、同じ名前のプレイヤーがいても衝突しない
        string uniqueKey = $"score_{owner.NickName}_{owner.UserId}";

        //  デバッグ表示
        Debug.Log($"ユニークキー生成: {uniqueKey}");

        return uniqueKey;
    }
}

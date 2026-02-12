/**
 * @file RacePlayer.cs
 * @brief レースゲームのプレイヤー
 * @author Sum1r3
 * @date 2025/9/6
 */
using Photon.Pun;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RacePlayer : MonoBehaviour {
    //移動速度
    [SerializeField]
    private float moveSpeed = 300f;
    //オリジナルのスピード
    private float originSpeed;

    //りぎっどボディの入手
    private Rigidbody rb;

    //ゴールしたかどうか
    [SerializeField]
    private bool isGoal;

    //各マックスタイム
    private const float MAX_TIME = 3f;
    //ブースト中
    private bool isBoost;
    //ブースト時間
    private float boostTime;
    //スロウ中
    private bool isSlow;
    //スロウ時間
    private float slowTime;
    //減速、加速の割合
    private const float SPEED_CHANGE_RATE = 1.5f;

    //自身の番号
    public int myNumber { get; private set; }
    //自身の順位
    public int myRank ;//{ get; private set; }

    //ブーストエフェクト
    [SerializeField]
    private ParticleSystem boostEffect;
    //スロウエフェクト
    [SerializeField]
    private ParticleSystem slowEffect;

    //つけるオーラの名前
    private const string SLOW_AURA_NAME = "SlowAura(Clone)";
    private const string BOOST_AURA_NAME = "BoostAura(Clone)";
    //親のフォトンビュ-
    [SerializeField]
    PhotonView photonView;
    [SerializeField]
    bool PVIsMine;

    //これがプレイヤーかどうかを示す
    [SerializeField]
    private GameObject PlayerIsMine;

    //デフォルトのY座標
    private const float DefaultYPos = 1.2f;

    //自身のアニメーター
    private Animator animator;
    //各種再生速度
    private const float _SLOW_ANIMATION_SPEED = 0.5f;
    private const float _BOOST_ANIMATION_SPEED = 1.0f;
    private const float _NORMAL_ANIMATION_SPEED = 0.75f;

    //キャンバスの参照
    [SerializeField]
    private GameObject canvasObject;

    /// <summary>
    /// 参照取得系
    /// </summary>
    private void Awake() {

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        //親のフォトンビュー取得
        photonView = transform.parent.GetComponent<PhotonView>();

        //スピードのオリジナルを取得
        originSpeed = moveSpeed;

        
    }


    void OnEnable() {
        
        

        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<RaceCameraController>().AddRacer(this.transform);
        //レースマネージャーにも入れる
        RaceManager_PUN.instance.AddRacers(this);
        
        //自身の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if(myNumber == -1) {
            myNumber = RaceManager_PUN.instance.GetPlayerNumber(this);
        }

        //変数初期化
        isGoal = false;


        //自身についているキャンバスの初期化処理を呼び出す
        if(canvasObject != null)
            canvasObject.GetComponent<PlayerIndexCanvas>().InitializeCanvas();

        //自分のフォトンビューを取得して、それが自分の物だったらYouの表示をつける
        if(photonView.IsMine)
            PlayerIsMine.SetActive(true); 
        else 
            PlayerIsMine.SetActive(false);

        //ゲーム開始
        RaceManager_PUN.instance.TryStartCountDown();

        //色の設定
        SetMyColor();

        //自身のポジションを設定
        RaceManager_PUN.instance.PlayerStartPosSet();

        //自身のアニメーターを取得
        animator = GetComponent<Animator>();
        //アニメーションを再生
        ChangeAnimationSpeed(_NORMAL_ANIMATION_SPEED);
        //角度の初期化
        transform.localRotation = Quaternion.Euler(0, -90, 0);
    }

    //アップデート
    void FixedUpdate() {
        if(transform.position.y - DefaultYPos > 0.1f && !RaceManager_PUN.instance.isGoal) {
            //Y軸ポジション固定
            Vector3 setpos = new Vector3(transform.position.x, DefaultYPos, transform.position.z);
            transform.position = setpos;
        }

        //スタート前に位置を固定
        if (!RaceManager_PUN.instance.isStart) {
            //自身のポジションを設定
            RaceManager_PUN.instance.PlayerStartPosSet();
        }
        
        //ここでブースト時間の確認＆switchの切り替え
        if (isBoost) {
            boostTime -= Time.deltaTime;
            moveSpeed = originSpeed * SPEED_CHANGE_RATE;
        }
        //ブーストしてかつ時間切れだったら
        if(boostTime <= 0 && isBoost) {
            isBoost = false;
            moveSpeed = originSpeed;
            //自身の子オブジェクトの中の特定のオブジェクトを探して破壊する
            Transform child = transform.Find(BOOST_AURA_NAME);
            Destroy(child.gameObject);
            //ゴールしてなかったら
            if (!isGoal) {
                //アニメーションの再生速度を普通の物に
                ChangeAnimationSpeed(_NORMAL_ANIMATION_SPEED);
            }
                
        }

        //ここでスロウ時間の確認＆switchの切り替え
        if (isSlow) {
            slowTime -= Time.deltaTime;
            moveSpeed = originSpeed / SPEED_CHANGE_RATE;
        }
        //スロウしてかつ時間切れだったら
        if (slowTime <= 0 && isSlow) {
            isSlow = false;
            moveSpeed = originSpeed;
            //自身の子オブジェクトの中の特定のオブジェクトを探して破壊する
            Transform child = transform.Find(SLOW_AURA_NAME);
            Destroy(child.gameObject);
            if (!isGoal) {
                //アニメーションの再生速度を普通の物に
                ChangeAnimationSpeed(_NORMAL_ANIMATION_SPEED);
            }
        }

        //移動
        if (!isGoal　&& RaceManager_PUN.instance.isStart) {
            Move();
        }
        
        //ゴールしているのに動いてはならない
        if(isGoal) {
            rb.velocity = Vector3.zero;
            transform.eulerAngles = Vector3.zero;
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {
        // X方向は固定（常に前進）
        float x = 1f; // ← 進行方向固定したいならこれでOK
        float z = GetComponentInParent<PlayerInfomation>().GetLeftStickValue().y;

        // 正規化しないでそのまま適用
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 減速
    /// </summary>
    /// <returns></returns>
    public void Slow() {
        if (!photonView.IsMine && GameManager.instance.IsOnline()) return;

        _ = AudioManager.instance.PlaySE(1);
        //アニメーションをゆっくりに
        ChangeAnimationSpeed(_SLOW_ANIMATION_SPEED);
        slowTime = MAX_TIME;
        if(!isSlow) {
            isSlow = true;
            //自身の直下にブーストオーラを生成
            transform.SpawnChildLocal(slowEffect.gameObject, Vector3.zero, new Vector3(90, 0, 0));
        }
    }

    /// <summary>
    /// 加速
    /// </summary>
    /// <returns></returns>
    public void Boost() {
        if (!photonView.IsMine && GameManager.instance.IsOnline()) return;

        _ = AudioManager.instance.PlaySE(0);
        //アニメーションを速く
        ChangeAnimationSpeed(_BOOST_ANIMATION_SPEED);
        boostTime = MAX_TIME;
        if (!isBoost) {
            isBoost = true;
            //自身の直下にブーストオーラを生成
            transform.SpawnChildLocal(boostEffect.gameObject, Vector3.zero, new Vector3(-90, 0, 0));
        }
    }

    /// <summary>
    /// ゴールしました
    /// </summary>
    public void Goal() {
        isGoal = true;
        //アニメーションを止める
        ChangeAnimationSpeed(0);

        //自身の順位を入れてもらい、値をもらう
        RaceManager_PUN.instance.AddRanking(this);
        myRank = RaceManager_PUN.instance.GetRankingCount(this);

        //自身の順位をプレイヤー情報管理クラスに引き渡す
        GetComponentInParent<PlayerInfomation>().SetRank(myRank);
    }

    
    private void OnTriggerEnter(Collider other) {
        //ゴールしたときにレースマネージャーのランキングに入れる
        if(other.gameObject.tag == "Finish") {
            //ゴールした時の処理を呼ぶ
            Goal();

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
    }

    /// <summary>
    /// マイナンバーを引き渡す
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber() {
        if(photonView.Owner == null) {
            return myNumber;
        }
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
    /// 自身のいろをかえる
    /// </summary>
    public void SetMyColor() {
        Color myColor = GetComponentInParent<PlayerInfomation>().GetMyColor();
        foreach(Transform child in transform) {
            if (child.name == "LeftEye" || child.name == "RightEye") continue;
            if (child.name == "hat" || child.name == "Canvas") continue;
            if (child.name == "LeftReg" || child.name == "RightReg") continue;
            if (child.name == "UnderMouse" || child.name == "UpMouse") continue;
            if (child.name == "アーマチュア" || child.name == "RacePlayerCanvas") continue;
            if (child.name == "RacePlayerCanvas" || child.name == "RacePlayerCanvas") continue;

            child.GetComponent<Renderer>().material.color = myColor;
        }
    }

    /// <summary>
    /// アニメーションの再生速度の変更
    /// </summary>
    /// <param name="changeTime"></param>
    private void ChangeAnimationSpeed(float changeTime) {
        animator.speed = changeTime;
    }
}

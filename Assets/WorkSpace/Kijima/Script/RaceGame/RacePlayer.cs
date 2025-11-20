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
    public int myRank { get; private set; }

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

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //親のフォトンビュー取得
        photonView = transform.parent.GetComponent<PhotonView>();

        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<RaceCameraController>().AddRacer(this.transform);
        //レースマネージャーにも入れる
        RaceManager_PUN.instance.AddRacers(this);
        
        //自身の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if(myNumber == -1) {
            myNumber = RaceManager_PUN.instance.GetPlayerNumber(this);
        }

        isGoal = false;
        //スピードのオリジナルを取得
        originSpeed = moveSpeed;

        //自身についているキャンバスの初期化処理を呼び出す
        GetComponentInChildren<PlayerIndexCanvas>().InitializeCanvas();

        //自分のフォトンビューを取得して、それが自分の物だったらYouの表示をつける
        if(photonView.IsMine)
            PlayerIsMine.SetActive(true); 
        else 
            PlayerIsMine.SetActive(false);

        //ゲーム開始
        RaceManager_PUN.instance.TryStartCountDown();

        //向きの固定
        transform.Rotate(0, -90, 0);
        //色の設定
        SetMyColor();

        //自身のポジションを設定
        RaceManager_PUN.instance.PlayerStartPosSet();
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
        if(boostTime <= 0 && isBoost) {
            isBoost = false;
            moveSpeed = originSpeed;
            //自身の子オブジェクトの中の特定のオブジェクトを探して破壊する
            Transform child = transform.Find(BOOST_AURA_NAME);
            Destroy(child.gameObject);
        }

        //ここでスロウ時間の確認＆switchの切り替え
        if (isSlow) {
            slowTime -= Time.deltaTime;
            moveSpeed = originSpeed / SPEED_CHANGE_RATE;
        }
        if (slowTime <= 0 && isSlow) {
            isSlow = false;
            moveSpeed = originSpeed;
            //自身の子オブジェクトの中の特定のオブジェクトを探して破壊する
            Transform child = transform.Find(SLOW_AURA_NAME);
            Destroy(child.gameObject);
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
    }

    
    private void OnTriggerEnter(Collider other) {
        //ゴールしたときにレースマネージャーのランキングに入れる
        if(other.gameObject.tag == "Finish") {
            //ゴールした時の処理を呼ぶ
            Goal();
            //自身の順位を入れてもらい、値をもらう
            RaceManager_PUN.instance.AddRanking(this);
            myRank = RaceManager_PUN.instance.GetRankingCount(this);

            //自身の順位をプレイヤー情報管理クラスに引き渡す
            GetComponentInParent<PlayerInfomation>().SetRank(myRank);

            //デバッグでランキング表示
            Debug.Log(this.gameObject.name + "は" + RaceManager_PUN.instance.GetRankingCount(this));
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

            child.GetComponent<Renderer>().material.color = myColor;
        }
    }
}

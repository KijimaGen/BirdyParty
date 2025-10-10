﻿/**
 * @file RacePlayer.cs
 * @brief レースゲームのプレイヤー
 * @author Sum1r3
 * @date 2025/9/6
 */
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RacePlayer : MonoBehaviour {
    //移動速度
    private float moveSpeed = 8f;
    //オリジナルのスピード
    private float originSpeed;

    //入力値
    private Vector2 moveInput;
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

    //自身の名前
    private string playerName;
    //自身の番号
    private int myNumber;

    //ブーストエフェクト
    [SerializeField]
    private ParticleSystem boostEffect;
    //スロウエフェクト
    [SerializeField]
    private ParticleSystem slowEffect;

    //つけるオーラの名前
    private const string SLOW_AURA_NAME = "SlowAura(Clone)";
    private const string BOOST_AURA_NAME = "BoostAura(Clone)";


    

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<RaceCameraController>().AddRacer(this.transform);
        //レースマネージャーにも入れる
        RaceManager.instance.AddRacers(this);
        //自身のポジションを設定
        RaceManager.instance.PlayerStartPosSet();
        //自身の番号を取得
        myNumber = RaceManager.instance.GetPlayerNumber(this);
        isGoal = false;
        //スピードのオリジナルを取得
        originSpeed = moveSpeed;
    }

    //アップデート
    void FixedUpdate() {
        //ポジション固定
        Vector3 setpos = new Vector3(transform.position.x,1.2f,transform.position.z);
        transform.position = setpos;

        //デバッグ用
        if (Input.GetMouseButtonDown(0)) {
            moveSpeed *= 10;
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


        //開始するまで動いてはならない
        if (!RaceManager.instance.isStart) {
            rb.velocity = Vector3.zero;
            return;
        }

        //移動
        Move();

        //ゴールしているのに動いてはならない
        if(isGoal) {
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
        // X方向は固定（常に前進）
        float x = 1f; // ← 進行方向固定したいならこれでOK
        float z = moveInput.y;

        // 正規化しないでそのまま適用
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed;
    }

    /// <summary>
    /// 減速
    /// </summary>
    /// <returns></returns>
    public void Slow() {
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

    /// <summary>
    /// ゴール
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Finish") {
            Goal();
            RaceManager.instance.AddRanking(this.gameObject);
            Debug.Log(this.gameObject.name + "は" + RaceManager.instance.GetRankingCount(this.gameObject));

        }
    }

    /// <summary>
    /// 位置移動
    /// </summary>
    public void SetPosition(Vector3 setPos) {
        transform.position = setPos;
    }

    /// <summary>
    /// マイナンバーを引き渡す
    /// </summary>
    /// <returns></returns>
    public int GetMyNumber() {
        return myNumber;
    }

    public void Plus(InputAction.CallbackContext context) {
        RaceManager.instance.StandbyOK();
    }
}

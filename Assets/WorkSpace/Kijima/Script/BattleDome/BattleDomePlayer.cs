/**
* @file BattleDomePlayer.cs
* @brief バトルドームシーンのプレイヤー
* @author Sum1r3
* @date 2025/11/26
*/
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleDomePlayer : MonoBehaviourPunCallbacks {
    //Higneコンポーネントの参照
    [Header("Hingeのステータス")]
    [SerializeField]
    private float spring = 40000;
    [SerializeField]
    private float openAngle = 60;   //開く角度
    [SerializeField]
    private float closeAngle = 0;   //閉じる角度

    //HingeJoint
    private HingeJoint hjL; //AxisL
    private HingeJoint hjR; //AxisR

    //JointSpring
    private JointSpring jR; //AxisR
    private JointSpring jL; //AxisL

    //AxisLとAxisR
    [Header("参照を持ちたいオブジェクト")]
    [SerializeField]
    private GameObject _axisL;
    [SerializeField]
    private GameObject _axisR;

    //マイナンバー
    private int _myNumber;

    //マイフォトンビュー
    PhotonView _photonview;

    private void Start() {
        //ヒンジジョイントを受け取る
        hjL = _axisL.GetComponent<HingeJoint>();
        hjR = _axisR.GetComponent<HingeJoint>();

        //スプリングを受け取る
        jL = hjL.spring;
        jR = hjR.spring;

        //自身のフォトンビューの取得
        _photonview = GetComponent<PhotonView>();

        //エントリー
        _=EntryToManager();

        //色設定
        SetMyColor();
    }

    public void TryFlipLeft(InputAction.CallbackContext context) {
        //ぬるちぇっく
        if (hjL == null || hjR == null) return;
        //オンラインカツ、自分の物かチェック
        if (GameManager.instance.IsOnline() && !_photonview.IsMine) return;

        //contextからboolに変換(オンラインでcontext送れないらしい)
        bool isFlip = context.performed;

        //オンラインかオフラインかで処理を変える
        if(GameManager.instance.IsOnline()) {
            photonView.RPC(nameof(FlipLeft),RpcTarget.All, isFlip); return;
        }
        else {
            FlipLeft(isFlip);
        }
    }


    /// <summary>
    /// 左フリップの入力検知
    /// </summary>
    /// <param name="context"></param>
    [PunRPC]
    public void FlipLeft(bool context) {
        //HingeJointを取ってSpringの値を弄り、動かす
        if (context) {
            jL.spring = spring;
            jL.targetPosition = openAngle;
            hjL.spring = jL;
        }
        else {
            jL.spring = spring;
            jL.targetPosition = closeAngle;
            hjL.spring = jL;
        }
    }

    public void TryFlipRight(InputAction.CallbackContext context) {
        //ぬるちぇっく
        if (hjL == null || hjR == null) return;
        //オンラインカツ、自分の物かチェック
        if (GameManager.instance.IsOnline() && !_photonview.IsMine) return;

        //contextからboolに変換(オンラインでcontext送れないらしい)
        bool isFlip = context.performed;

        //オンラインかオフラインかで処理を変える
        if (GameManager.instance.IsOnline()) {
            photonView.RPC(nameof(FlipRight), RpcTarget.All, isFlip); return;
        }
        else {
            FlipRight(isFlip);
        }
    }

    /// <summary>
    /// 右フリップの入力検知
    /// </summary>
    /// <param name="context"></param>
    [PunRPC]
    public void FlipRight(bool context) {
        //HingeJointを取ってSpringの値を弄り、動かす
        if (context) {
            jR.spring = spring;
            jR.targetPosition = openAngle;
            hjR.spring = jR;
        }
        else {
            jR.spring = spring;
            jR.targetPosition = closeAngle;
            hjR.spring = jR;
        }
    }


    /// <summary>
    /// プレイヤーマネージャーにエントリーさせてもらう
    /// </summary>
    /// <returns></returns>
    public async UniTask EntryToManager() {
        //プレイヤーマネージャーの参照がなかったら出来るまで待つ
        while(BattleDomePlayerManager.instance == null) {
            await UniTask.Delay(1);
        }

        //参照をキャッシュ
        BattleDomePlayerManager PlayerManager = BattleDomePlayerManager.instance;
        //エントリーさせてもらう
        PlayerManager.Enty(this);
        //マイナンバー取得
        if (GameManager.instance.IsOnline()) {
            _myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            //アクターナンバー直入れだとズレるのでマイナスする
            _myNumber--;
        }
        else {
            _myNumber = PlayerManager.GetPlayerNumber(this);
        }


        //位置調整
        transform.position = PlayerManager.GetPlayerPosition(_myNumber);
        //角度調整
        transform.rotation = Quaternion.Euler(PlayerManager.GetPlayerRotation(_myNumber));

    }

    /// <summary>
    /// 自身のいろをかえる
    /// </summary>
    public void SetMyColor() {
        Color myColor = GetComponentInParent<PlayerInfomation>().GetMyColor();
        
        _axisL.GetComponent<Renderer>().material.color = myColor;
        _axisL.transform.GetChild(0).GetComponent<Renderer>().material.color = myColor;
        _axisR.GetComponent<Renderer>().material.color = myColor;
        _axisR.transform.GetChild(0).GetComponent<Renderer>().material.color = myColor;

    }

}

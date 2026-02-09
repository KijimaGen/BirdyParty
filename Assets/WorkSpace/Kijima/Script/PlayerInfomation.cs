/**
 * @file PlayerInfomation.cs
 * @brief プレイヤーの各々の情報
 * @author Sum1r3
 * @date 2025/10/14
 */
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static GameConst;

public class PlayerInfomation:MonoBehaviour{
    //今持っているポイント
    public int myPoint;
    //現在の順位
    public int rank;
    //自分の名前
    public string myName;
    //自分のskin
    public SkinVariation mySkin;
    //自分の番号
    public int myNumber;
    
    //自分の色
    private Color myColor;

    //自分のマテリアル
    private int materialIndex = 0;

    //自身のフォトンビュー
    PhotonView photonView;
   
    //レースゲームのプレイヤー
    [SerializeField]
    private GameObject racePlayer;
    //ドロップゲームのプレイヤー
    [SerializeField]
    private GameObject dropPlayer;
    // ダイスのプレイヤー
    [SerializeField]
    public GameObject dicePlayer;
    // ダイスのプレイヤー
    [SerializeField]
    public GameObject battleDomePlayer;

    //エントリ～したかどうか
    [SerializeField]
    private bool isEntry = false;

    //自分のコントローラーの入力値
    Vector2 myInputLeftStickValue = Vector2.zero;

    

    /// <summary>
    /// スタート
    /// </summary>
    void Start() {
        //ポイントを初期化
        myPoint = 0;
        //自身のフォトンビュー取得
        photonView = GetComponent<PhotonView>();
        //プレイヤー管理クラスに登録
        PlayerManager.instance.AddPlayer(this);

        //自身の番号をもらってくる
        SetMyNumber();

        //オンラインだったらエントリーを行う
        if (GameDataManager.instance != null
            && GameManager.instance.IsOnline()
            && GameDataManager.instance.GetToriFromNumber(myNumber) != null) {
            Entry();
        }

        // シーン読み込み時のコールバック登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        //自身の実稼働オブジェクトを取得し、そいつを引き渡してカーソルをもらう
        PlayerInput playerInput = transform.GetChild(0).GetComponent<PlayerInput>();
        
        //タイトルに基本呼ぶのでバーチャゥマウスを作る
        if(VirtualMouseManager.instance != null)
            VirtualMouseManager.instance.OnPlayerJoined(playerInput);

        //オンラインだったら
        if (GameManager.instance.IsOnline()) {
            //名前の取得
            var player = PhotonNetwork.PlayerList[myNumber];
            myName = player.NickName;
            //タイトルマネージャーのヌルチェック
            if (TitleManager.instance != null) {
                //タイトルマネージャーに名前の表示を依頼
                TitleManager.instance.SetPlayerNameList(myNumber, myName);
            }
        }

        //エントリーしましたテキストを作る
        if(GameManager.instance.IsOnline() && GameDataManager.instance != null)
            GameDataManager.instance.GetComponent<PhotonView>().RPC(nameof(GameDataManager.instance.InstantiateNameBox), RpcTarget.All, myName);

        //自身の色を決める
        myColor = PLAYER_COLOR[myNumber];

        //一回実働オブジェクトを非アクティブ
        DestroySelectedChildren();

        //自身が消えないようにする
        DontDestroyOnLoad(gameObject);
    }

    
    void Update() {
        //それ以外の処理はこの上に書く
        // 簡単な切断検知
        if (!GameManager.instance.IsOnline())
            return;

        //接続できているか確認
        if (!PhotonNetwork.IsConnected) {
            Debug.Log("接続が切れています");
            // 切断時の処理
            Destroy(gameObject); return;
        }

        
    }

    //自身が消えるときにコールバックを止める
    private void OnDisable() {
        // 忘れずに解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 自身が破壊されるときに呼ばれる関数
    /// </summary>
    private void OnDestroy() {
        //プレイヤーマネージャーのリストから除外
        PlayerManager.instance.RemovePlayer(this);
    }

    /// <summary>
    /// シーン遷移関数
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //エントリーしてないならいなくなる
        if(!isEntry) Destroy(gameObject);

        //一回実働オブジェクトを破壊
        DestroySelectedChildren();
        //それぞれのシーンの名前に合わせたロードシーン関数を呼ぶ
        if (scene.name == RACEGAME_SCENE_NAME) {
            LoadRaceScene();
        }

        if (scene.name == DROPGAME_SCENE_NAME) {
            LoadDropGameScene();
        }

        if (scene.name == DICEGAME_SCENE_NAME){
            LoadDiceGameScene();
        }

        if (scene.name == BATTLEDOME_SCENE_NAME){
            LoadBattleDomeScene();
        }

        if (scene.name == TITLE_SCENE_NAME) {
            LoadTitleScene();
        }
    }

    /// <summary>
    /// 自身の実働オブジェクトの中身を非アクティブ
    /// </summary>
    public void DestroySelectedChildren() {
        if (transform == null) return;

        // 子オブジェクトの孫を順番に非アクティブ
        foreach (Transform grandChild in transform) {
            if (grandChild != null)
                grandChild.gameObject.SetActive(false);
        }
    }

    //レースゲームのシーンが読み込まれたときに呼ぶ
    public void LoadRaceScene() {
        racePlayer.SetActive(true);
    }

    //ドロップゲームのシーンが読み込まれたときに呼ぶ
    public void LoadDropGameScene() {
        dropPlayer.SetActive(true);
    }

    //バトルドームのシーンが読み込まれたときに呼ぶ
    public void LoadBattleDomeScene() {
        PlayerInput playerInput = gameObject.GetComponent<PlayerInput>();
        playerInput.SwitchCurrentActionMap("BattleDome");
        battleDomePlayer.SetActive(true);
    }

    //ダイスゲームのシーンが読み込まれたときに呼ぶ
    private void LoadDiceGameScene(){
        dicePlayer.SetActive(true);

        var pi = GetComponent<PlayerInput>();
        if (pi != null)
        {
            pi.SwitchCurrentActionMap("DiceGame");
            Debug.Log($"[Input] Switched to DiceGame : P{myNumber}");
        }
    }

    //タイトルシーンが読み込まれたときに呼ぶ(今は破壊)
    private void LoadTitleScene() {
        //Destroy(gameObject);
    }

    //タイトル画面でエントリーしたい
    public void Plus() {
        if (GameDataManager.instance == null || GameManager.instance.IsOnline())
            return;

        if(GameDataManager.instance.GetToriFromNumber(myNumber) != null) {
            Entry();
        }
    }

    /// <summary>
    /// 自分のコントローラーの入力値を受け取る
    /// </summary>
    /// <param name="context"></param>
    public void SetLeftStickValue(InputAction.CallbackContext context) {
        //オンラインだったら自分のだけ。オフラインだったら気にせず取る
        if(GetComponent<PhotonView>().IsMine || !GameManager.instance.IsOnline())
            myInputLeftStickValue = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 自分のコントローラーの入力値を渡す
    /// </summary>
    /// <returns></returns>
    public Vector2 GetLeftStickValue() {
        return myInputLeftStickValue;
    }

    /// <summary>
    /// 自身をエントリーから外す
    /// </summary>
    public void WithdrawEntry() {
        //自身のモデルをfalseにしてもらう
        GameDataManager.instance.GetToriFromNumber(myNumber).SetActive(false);
    }

    public void Entry() {
        //ゲームデータマネージャー側でエントリーしてもらう
        //自身のモデルをtrueにしてもらう
        GameDataManager.instance.GetToriFromNumber(myNumber).SetActive(true);
        //エントリー変数true
        isEntry = true;

        //名前を登録してもらう
        GameDataManager.instance.EntryPlayer(this);
    }

    /// <summary>
    /// プレイヤーナンバーを他のところからもらってくる
    /// </summary>
    private void SetMyNumber() {
        //自身の番号を取得
        if (GameManager.instance.IsOnline()) {
            //オンラインの場合、Photonに番号の管理を任せる
            var pv = GetComponent<PhotonView>();
            myNumber = pv.ControllerActorNr-1; //<-Photonは１から番号がスタートするので-1
        }
        else {
            //オフラインだったらローカルのマネージャーに
            myNumber = PlayerManager.instance.GetPlayerNumber(this);
        }
    }

    /// <summary>
    /// 自身をパーティモードマネージャーにエントリーさせる
    /// </summary>
    public void EntoriedPartyModeManager() {
        PartyModeManager.instance.AddPlayerRanking(this);
    }

    #region 各種ゲッターとセッター
    // Point
    public int GetPoint() { return myPoint; }
    public void SetPoint(int value) { 
        myPoint = value;
        //パーティモードマネージャーに得点の変化を共有
        PartyModeManager.instance?.SortPlayerRanking();
    }

    // Rank
    public int GetRank() { return rank; }
    public void SetRank(int value) { rank = value; }

    // myName
    public string GetName() { return myName; }
    public void SetName(string value) { myName = value; }

    // Skin
    public SkinVariation GetMySkin() { return mySkin; }
    public void SetMySkin(SkinVariation value) { mySkin = value; }

    // Number
    public int GetMyNumber() { return myNumber; }
    public void SetMyNumber(int value) { myNumber = value; }
    //Color

    public Color GetMyColor() { return myColor; }

    public int GetMaterialIndex() { return this.materialIndex; }
    public void SetMaterialIndex(int index) { this.materialIndex = index; }

    #endregion


}


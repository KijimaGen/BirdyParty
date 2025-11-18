/**
 * @file PlayerInfomation.cs
 * @brief プレイヤーの各々の情報
 * @author Sum1r3
 * @date 2025/10/14
 */
using Photon.Pun;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameConst;

public class PlayerInfomation:MonoBehaviour{
    //今持っているポイント
    public int point;
    //現在の順位
    public int rank;
    //自分の名前
    public string myName;
    //自分のskin
    public SkinVariation mySkin;
    //自分の番号
    
    public int myNumber;

    //自身のフォトンビュー
    PhotonView photonView;
   
    //レースゲームのプレイヤー
    [SerializeField]
    private GameObject racePlayerPrefab;

    // ダイスのプレイヤー
    [SerializeField]
    private GameObject dicePlayerPrefab;

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
        point = 0;
        //自身のフォトンビュー取得
        photonView = GetComponent<PhotonView>();
        //プレイヤー管理クラスに登録
        PlayerManager.instance.AddPlayer(this);
        //自身の番号を取得
        myNumber = PlayerManager.instance.GetPlayerNumber(this);
        // シーン読み込み時のコールバック登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        //自身の実稼働オブジェクトを取得し、そいつを引き渡してカーソルをもらう
        PlayerInput playerInput = transform.GetChild(0).GetComponent<PlayerInput>();
        if(VirtualMouseManager.instance != null)
            VirtualMouseManager.instance.OnPlayerJoined(playerInput);
        if (GameManager.instance.IsOnline()) {
            //名前の取得
            myName = NetworkManager.instance.GetName();
            
        }

        //エントリーしましたテキストを作る
        if(GameManager.instance.IsOnline() && GameDataManager.instance != null)
            GameDataManager.instance.GetComponent<PhotonView>().RPC(nameof(GameDataManager.instance.InstantiateNameBox), RpcTarget.All, myName);

        //自身が消えないようにする
        DontDestroyOnLoad(gameObject);
    }

    // 簡単な切断検知
    void Update() {
        if (!GameManager.instance.IsOnline())
            return;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //エントリーしてないならいなくなる
        if(!isEntry) Destroy(gameObject);

        //一回実働オブジェクトを破壊
        DestroySelectedChildren();
        if (scene.name == RACEGAME_SCENE_NAME) {
            LoadRaceScene();
        }

        if (scene.name == DROPGAME_SCENE_NAME) {
            LoadDropGameScene();
        }

        if (scene.name == DICE_SCENE_NAME)
        {
            LoadDiceGameScene();
        }
    }
    
    /// <summary>
    /// 自身の実働オブジェクトの中身を破壊
    /// </summary>
    public void DestroySelectedChildren() {
        if (transform == null) return;

        // 子オブジェクトの孫を順番に破壊
        foreach (Transform grandChild in transform) {
            if (grandChild != null)
                grandChild.gameObject.SetActive(false);
        }
    }

    //レースゲームのシーンが読み込まれたときに呼ぶ
    public void LoadRaceScene() {
        
        racePlayerPrefab.SetActive(true);
    }

    //ドロップゲームのシーンが読み込まれたときに呼ぶ
    public void LoadDropGameScene() {

    }

    private void LoadDiceGameScene()
    {
        if (photonView.IsMine)
        {
            // プレイヤーの ActorNumber (1から始まる) を取得
            int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;

            // DiceGameManager からスポーンポイントを取得 (FindObjectOfTypeを使用)
            DiceGameManager manager = FindObjectOfType<DiceGameManager>();
            Vector3 spawnPos = Vector3.zero;
            if (manager != null && playerIndex > 0 && playerIndex <= manager.playerSpawnPoints.Length)
            {
                // 0始まりの配列に合わせる
                spawnPos = manager.playerSpawnPoints[playerIndex - 1].position;
            }

            Debug.Log($"[Spawn Debug] P{playerIndex} のスポーン座標: {spawnPos}");

            // ★★★ 追加ログ ★★★
            Debug.Log($"[Instantiate Check] プレイヤー: {PhotonNetwork.LocalPlayer.NickName} が自身のダイスオブジェクト生成を試みます。");

            GameObject spawnedDicePlayer = PhotonNetwork.Instantiate(
                dicePlayerPrefab.name, // ここで指定しているPrefab名を確認
                spawnPos,
                Quaternion.identity
            );

            // ★★★ 追加ログ ★★★
            if (spawnedDicePlayer != null)
            {
                Debug.Log($"[Instantiate Check] ネットワークオブジェクトの生成に成功しました。名前: {spawnedDicePlayer.name}");
            }
            else
            {
                Debug.LogError($"[Instantiate Check] ネットワークオブジェクトの生成に失敗しました。Prefab名: {dicePlayerPrefab.name}");
            }
        }
        else
        {
            Debug.Log($"[Instantiate Check] リモートプレイヤーのため生成をスキップ。");
        }
    }

    //タイトル画面でエントリーしたい
    public void Plus(InputAction.CallbackContext context) {
        if (GameDataManager.instance == null)
            return;

        if(GameDataManager.instance.GetToriFromNumber(myNumber) != null) {
            GameDataManager.instance.GetToriFromNumber(myNumber).SetActive(true);
            isEntry = true ;

            //名前を登録してもらう
            GameDataManager.instance.EntryPlayer(this);
        }
    }

    /// <summary>
    /// 自分のコントローラーの入力値を受け取る
    /// </summary>
    /// <param name="context"></param>
    public void SetLeftStickValue(InputAction.CallbackContext context) {
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


    #region 各種ゲッターとセッター
    // Point
    public int GetPoint() { return point; }
    public void SetPoint(int value) { point = value; }

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

    #endregion
}


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
    public string Name;
    //自分のskin
    public SkinVariation mySkin;
    //自分の番号
    public int myNumber;

    //自身のフォトンビュー
    PhotonView photonView;
   
    //レースゲームのプレイヤー
    [SerializeField]
    private GameObject racePlayer;
    //エントリ～したかどうか
    [SerializeField]
    private bool isEntry = false;

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
        VirtualMouseManager.instance.OnPlayerJoined(playerInput);
        //名前の取得
        Name = NetworkManager.instance.GetName();
        //デバッグ名前表示
        Debug.Log("Nameは" + Name);


        //自身が消えないようにする
        DontDestroyOnLoad(gameObject);
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
    }
    
    /// <summary>
    /// 自身の実働オブジェクトの中身を破壊
    /// </summary>
    public void DestroySelectedChildren() {
        if (transform == null) return;

        // 子オブジェクトの孫を順番に破壊
        foreach (Transform grandChild in transform) {
            if (grandChild != null)
                Destroy(grandChild.gameObject);
        }
    }

    //レースゲームのシーンが読み込まれたときに呼ぶ
    public void LoadRaceScene() {
        Instantiate(racePlayer, transform);
    }

    //ドロップゲームのシーンが読み込まれたときに呼ぶ
    public void LoadDropGameScene() {

    }

    //タイトル画面でエントリーしたい
    public void Plus(InputAction.CallbackContext context) {
        if(GameDataManager.Instance.GetToriFromNumber(myNumber) != null) {
            GameDataManager.Instance.GetToriFromNumber(myNumber).SetActive(true);
            isEntry = true ;
        }
    }


    #region 各種ゲッターとセッター
    // Point
    public int GetPoint() { return point; }
    public void SetPoint(int value) { point = value; }

    // Rank
    public int GetRank() { return rank; }
    public void SetRank(int value) { rank = value; }

    // Name
    public string GetName() { return Name; }
    public void SetName(string value) { Name = value; }

    // Skin
    public SkinVariation GetMySkin() { return mySkin; }
    public void SetMySkin(SkinVariation value) { mySkin = value; }

    // Number
    public int GetMyNumber() { return myNumber; }
    public void SetMyNumber(int value) { myNumber = value; }

    #endregion
}


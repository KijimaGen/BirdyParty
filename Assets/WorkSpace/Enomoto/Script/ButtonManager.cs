using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using static GameConst;



public class ButtonManager : MonoBehaviour
{
    [Header("UI定義")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject modeUI;
    [SerializeField] private GameObject minigameSelectUI;
    [SerializeField] private GameObject partyModeUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject netWorkUI;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject playerSelectUI;
    [SerializeField] private GameObject gameReadyUI;

    [Header("エラーログ")]
    [SerializeField] private GameObject errorMinigameSelect;

    [Header("ネットワーク関連")]
    [SerializeField] TMP_InputField roomCodeInputField; // ルームコード入力欄
    [SerializeField] private TextMeshProUGUI connectionStatusText; // 接続状態表示
    [SerializeField] private UnityEngine.UI.Button createRoomButton; // ルーム作成ボタン
    [SerializeField] private UnityEngine.UI.Button joinRoomButton; // ルーム参加ボタン


    [Header("パーティモードの時に消すボタン")]
    [SerializeField] private GameObject backButton;

    [Header("ルームコードUI（プレイヤーエントリー）")]
    [SerializeField] private GameObject roomCodeUI;

    [Header("オンライン時にクライアントに見せるUI")]
    [SerializeField] private GameObject clientLog;
    [SerializeField] private GameObject clientUI;

    [Header("ミニゲーム・パーティモードアイコン")]
    [SerializeField] private GameObject minigameIcon;
    [SerializeField] private GameObject partyIcon;

    // 開いたUIを保存して戻れるように
    private Stack<GameObject> uiHistory = new Stack<GameObject>();
    //ホストかどうか
    private bool isHost;
    // オンラインモードか？
    private bool isOnlineMode = false;
    //次に行くシーンの名前
    private string NextSceneText;
    [Header("説明テキスト")]
    //説明テキスト
    [SerializeField]
    private TextMeshProUGUI ExplanationText;


    //レースゲームの説明テキスト
    private const string RaceGameExplanationText = "レーンを走って1位を競います。\r\nダッシュ板に乗ると加速、\r\nハードルに当たると減速です。" +
        "\r\n--------------------------------------\r\n操作説明\r\n左スティック：移動";
    //ドロップゲームの説明テキスト
    private const string DropGameExplanationText = "４つのパネルのうち１つのパネルは\r\n通ることが出来ます。\r\n正解のパネルに飛び込んで\r\n得点を手に入れましょう。" +
        "\r\n--------------------------------------\r\n操作説明\r\n左スティック：移動";
    //ダイスゲームの説明テキスト
    private const string DiceGameExplanationText = "５ターン制のダイスゲームです。\r\n一斉にダイスを転がし、\r\n指定されるBABAを当てないようにしましょう。" +
        "\r\n--------------------------------------\r\n操作説明\r\nA：ダイスを振る";

    [Header("名前入力欄")]
    [SerializeField]
    private TMP_InputField _nameInputField;

    //自身のインスタンス
    public static ButtonManager instance;

    [Header("ミニゲームの説明オブジェクト一覧")]
    [SerializeField]
    private List<GameObject> MiniGameImage;

    [Header("ルームナンバーを表示する看板")]
    [SerializeField]
    private GameObject RoomNumberPlate;


    private void Start()
    {

        bool backToPartyRoulette = PlayerPrefs.GetInt(PartyModeManager.PREF_BACK_TO_PARTY, 0) == 1;

        bool comeBack = PlayerPrefs.GetInt("ComeBackFromGame", 0) == 1;

        // オンライン中かつホストの場合はローカル同様各ゲームによって戻ってきた際にUIを変更
        if (GameManager.instance.IsOnline() && PhotonNetwork.IsMasterClient)
        {
            // ゲームから戻ってきたかどうかでUIを切り替え
            if (backToPartyRoulette)
            {
                Debug.Log("パーティモード：ゲーム終了→ルーレット");

                titleUI.SetActive(false);
                modeUI.SetActive(false);
                minigameSelectUI.SetActive(false);

                partyModeUI.SetActive(true);

                uiHistory.Clear();
                uiHistory.Push(titleUI);
                uiHistory.Push(modeUI);
                uiHistory.Push(partyModeUI);

                ChangePartyMode();

                PlayerPrefs.SetInt(PartyModeManager.PREF_BACK_TO_PARTY, 0);
                PlayerPrefs.Save();

                PlayStyle();

                instance = this;
                return;
            }

            if (comeBack)
            {
                Debug.Log("ミニゲームモード：ゲーム終了→選択画面");

                titleUI.SetActive(false);
                modeUI.SetActive(false);
                minigameSelectUI.SetActive(true);

                uiHistory.Clear();
                uiHistory.Push(titleUI);
                uiHistory.Push(modeUI);
                uiHistory.Push(minigameSelectUI);

                ChangeMinigameMode();

                PlayerPrefs.SetInt("ComeBackFromGame", 0);
                PlayerPrefs.Save();

                PlayStyle();

                instance = this;
                return;
            }
        }
        // オンライン中かつクライアントの場合は待機画面を出す
        else if (GameManager.instance.IsOnline() && !PhotonNetwork.IsMasterClient)
        {
            titleUI.SetActive(false);
            modeUI.SetActive(false);
            clientUI.SetActive(true);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
            uiHistory.Push(modeUI);
            uiHistory.Push(clientUI);

            instance = this;
            return;
        }
        // ローカルの場合は関係なく戻ってきた際のUIを出す
        else
        {
            // ゲームから戻ってきたかどうかでUIを切り替え
            if (backToPartyRoulette)
            {
                Debug.Log("パーティモード：ゲーム終了→ルーレット");

                titleUI.SetActive(false);
                modeUI.SetActive(false);
                minigameSelectUI.SetActive(false);

                partyModeUI.SetActive(true);

                uiHistory.Clear();
                uiHistory.Push(titleUI);
                uiHistory.Push(modeUI);
                uiHistory.Push(partyModeUI);

                ChangePartyMode();

                PlayerPrefs.SetInt(PartyModeManager.PREF_BACK_TO_PARTY, 0);
                PlayerPrefs.Save();

                PlayStyle();

                instance = this;
                return;
            }

            if (comeBack)
            {
                Debug.Log("ミニゲームモード：ゲーム終了→選択画面");

                titleUI.SetActive(false);
                modeUI.SetActive(false);
                minigameSelectUI.SetActive(true);

                uiHistory.Clear();
                uiHistory.Push(titleUI);
                uiHistory.Push(modeUI);
                uiHistory.Push(minigameSelectUI);

                ChangeMinigameMode();

                PlayerPrefs.SetInt("ComeBackFromGame", 0);
                PlayerPrefs.Save();

                PlayStyle();

                instance = this;
                return;
            }

            Debug.Log("タイトルに移動");

            titleUI.SetActive(true);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(false);

            uiHistory.Clear();
            uiHistory.Push(titleUI);

            //参照の取得
            instance = this;
        }
    }

    public void Open(GameObject openUI)
    {
        if (uiHistory.Count > 0)
        {
            GameObject current = uiHistory.Peek();
            current.SetActive(false);
        }

        openUI.SetActive(true);
        uiHistory.Push(openUI);

        if (openUI == modeUI){
            GameDataManager.instance.playOnline = false;
        }else{
            GameDataManager.instance.playOnline = true;
        }

        if (openUI == partyModeUI)
        {
            ChangePartyMode();
        }
        else
        {
            ChangeMinigameMode();
        }
    }

    public void Back(){
        if (GameManager.instance == null) {
            Debug.LogWarning("GameManager が存在しません");
            return;
        }
        if (uiHistory.Count > 1)
        {
            GameObject closing = uiHistory.Pop();
            closing.SetActive(false);

            GameObject previous = uiHistory.Peek();
            previous.SetActive(true);
        }
    }

    //プレイスタイル変更関数
    public void PlayStyle(){
        if (GameManager.instance == null) {
            Debug.LogWarning("GameManager が存在しません");
            return;
        }

        if (GameDataManager.instance.playOnline){
            //ゲームマネージャーの内部的なfalseとtrueを変える
            GameManager.instance.SetIsOnline(true);
        }else{
            //ゲームマネージャーの内部的なfalseとtrueを変える
            GameManager.instance.SetIsOnline(false);
        }
    }

    // ログオープン
    public void OpenObject(GameObject openObj){
        openObj.SetActive(true);
    }

    // ログクローズ
    public void CloseObject(GameObject closeObj)
    {
        closeObj.SetActive(false);
    }

    public void OnClickStartGame() {
        //自分がマスタークライアントだったら全員に送る
        PhotonView photonView = gameObject.GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("StartGame", RpcTarget.All, NextSceneText);
            Debug.Log(NextSceneText);
        }

        if (!GameManager.instance.IsOnline())
            StartGame(NextSceneText);
    }

    [PunRPC]
    // ミニゲーム開始
    public void StartGame(string sceneName) {
        // プレイヤーがいないのにゲームは始められませんでしょう
        //if (GameDataManager.instance.GetEntriedPlayerCount() == 0) return;


        if (GameDataManager.instance != null) {
            GameDataManager.instance.selectedMiniGame = sceneName;
            GameDataManager.instance.comeBackFromGame = true;
        }

        PhotonNetwork.LoadLevel(sceneName);
    }

    public void OnExit() {
#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

                Application.Quit();

#endif
    }


    // ネットワーク関連のメソッド
    /// <summary>
    /// ルーム作成ボタンが押された時の処理
    /// </summary>
    public void CreateRoom() {
        if (NetworkManager.instance != null) {
            UpdateNetworkStatus("ランダムな名前でルームを作成中...");
            NetworkManager.instance.CreateRandomRoom();
        }
        else {
            UpdateNetworkStatus("NetworkManagerが見つかりません");
        }
    }


    /// <summary>
    /// ルーム参加ボタンが押された時の処理（入力欄から取得）
    /// </summary>
    public void JoinRoomFromInput() {
        // InputFieldの設定チェック
        if (roomCodeInputField == null) {
            UpdateNetworkStatus("⚠️ InputFieldが設定されていません！");
            Debug.LogError("roomCodeInputField が null です。Inspectorで設定してください。");
            return;
        }

        // 入力内容をデバッグ表示
        string rawInput = roomCodeInputField.text;
        Debug.Log(roomCodeInputField.gameObject.name);
        Debug.Log(roomCodeInputField.text);
        string roomCode = rawInput.Trim().ToUpper();

        Debug.Log($"🔍 デバッグ情報:");
        Debug.Log($"  - 生の入力: '{rawInput}' (長さ: {rawInput.Length})");
        Debug.Log($"  - 処理後: '{roomCode}' (長さ: {roomCode.Length})");
        Debug.Log($"  - InputField名: {roomCodeInputField.name}");

        JoinRoom(roomCode);
    }

    /// <summary>
    /// 指定されたルームコードでルームに参加
    /// </summary>
    /// <param name="roomCode">参加するルームコード</param>
    public void JoinRoom(string roomCode) {
        if (NetworkManager.instance == null) {
            UpdateNetworkStatus("NetworkManagerが見つかりません");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady) {
            UpdateNetworkStatus("Photonに接続中です...");
            return;
        }

        if (string.IsNullOrEmpty(roomCode)) {
            UpdateNetworkStatus("❌ ルームコードを入力してください");
            Debug.LogWarning($"ルームコードが空です: '{roomCode}' (null: {roomCode == null}, empty: {roomCode == ""})");
            return;
        }

        if (roomCode.Length != 5) {
            UpdateNetworkStatus("ルームコードは5文字で入力してください");
            return;
        }

        UpdateNetworkStatus($"ルーム {roomCode} に参加中...");
        NetworkManager.instance.JoinRoomWithCode(roomCode);
    }

    /// <summary>
    /// ネットワーク状態表示を更新
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    private void UpdateNetworkStatus(string message) {
        if (connectionStatusText != null) {
            connectionStatusText.text = message;
        }
        Debug.Log($"Network: {message}");
    }

    /// <summary>
    /// 手動でルームコードを設定してテスト
    /// </summary>
    /// <param name="code">設定するルームコード</param>
    public void SetRoomCodeAndJoin(string code) {
        if (roomCodeInputField != null) {
            roomCodeInputField.text = code;
            Debug.Log($"ルームコードを '{code}' に設定しました");
        }
        JoinRoom(code);
    }

    /// <summary>
    /// 現在の状況を詳しく表示（超デバッグ用）
    /// </summary>
    [ContextMenu("🔍 現在の状況を詳しく確認")]
    public void ShowDetailedStatus() {
        Debug.Log("=== 🔍 詳細状況確認 ===");

        // 実行環境
        string environment = Application.isEditor ? "Unity エディター" : "ビルド版";
        Debug.Log($"📱 実行環境: {environment}");

        // SimpleNetworkManager確認
        if (NetworkManager.instance == null) {
            Debug.LogError("❌ NetworkManager.instance が null です！");
            UpdateNetworkStatus("NetworkManager");
            return;
        }

        // Photon接続状態（安全チェック付き）
        try {
            Debug.Log($"🌐 Photon接続状態: {PhotonNetwork.NetworkClientState}");
            Debug.Log($"🔗 接続準備完了: {PhotonNetwork.IsConnectedAndReady}");
            Debug.Log($"🏠 ルーム内: {PhotonNetwork.InRoom}");

            // 現在のルーム情報
            if (PhotonNetwork.CurrentRoom != null) {
                Debug.Log($"🎮 現在のルーム: {PhotonNetwork.CurrentRoom.Name}");
                Debug.Log($"👥 プレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
                UpdateNetworkStatus($"ルーム '{PhotonNetwork.CurrentRoom.Name}' に参加中 ({PhotonNetwork.CurrentRoom.PlayerCount}人)");
            }
            else {
                Debug.Log("🚫 現在ルームに参加していません");
                UpdateNetworkStatus("ルームに参加していません");
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"❌ Photon参照エラー: {e.Message}");
            UpdateNetworkStatus("Photonの設定に問題があります");
        }

        // SimpleNetworkManagerの状態
        if (NetworkManager.instance != null) {
            Debug.Log($"📝 現在のルームコード: '{NetworkManager.instance.GetCurrentRoomCode()}'");
        }
        else {
            Debug.LogWarning("SimpleNetworkManager.instance が見つかりません");
        }
    }

    /// <summary>
    /// セットイズホスト
    /// </summary>
    /// <param name="ishost"></param>
    public void SetIsHost(bool ishost) {
        isHost = ishost;
    }

    /// <summary>
    /// オンラインにセット
    /// </summary>
    public void SetOnline() {
        if (GameManager.instance != null && GameManager.instance.gameObject != null) {
            GameManager.instance.SetIsOnline(true);
            isOnlineMode = true;
        }
        else {
            Debug.LogWarning("GameManager が破壊されています");
        }
    }

    /// <summary>
    /// オフラインにセット
    /// </summary>
    public void SetOffline() {
        if (GameManager.instance != null && GameManager.instance.gameObject != null) {
            GameManager.instance.SetIsOnline(false);
            isOnlineMode = false;
        }
        else {
            Debug.LogWarning("GameManager が破壊されています");
        }
    }


    /// <summary>
    /// 部屋を抜ける処理を呼び出すよん
    /// </summary>
    public void LeaveRoom() {
        if(NetworkManager.instance != null)
            NetworkManager.instance.LeaveRoom();
    }

    /// <summary>
    /// サーバーから切&断
    /// </summary>
    public void DisconnectingServer() {
        NetworkManager.instance.DisconnectingServer();
    }

    /// <summary>
    /// サーバーに再接続
    /// </summary>
    public void ReconnectServer() {
        if (!PhotonNetwork.IsConnected) {
            NetworkManager.instance.ConnectingServer();
        }
    }

    /// <summary>
    /// プレイヤーセレクトUIに行くときの処理
    /// </summary>
    public void GoPlayerSelectUI() {
        //次へボタンを見せなくする
        TitleManager.instance.SetActiveNextButton(false);
    }

    /// <summary>
    /// オンラインモードの時プレイヤーエントリー画面でクライアントに見せるUI
    /// </summary>
    public void OpenLogClientOnline()
    {
        if (GameManager.instance.IsOnline() && !PhotonNetwork.IsMasterClient)
        {
            
        }
    }

    /// <summary>
    /// ミニゲームを選択した場合はミニゲーム選択画面に戻ってくるように
    /// </summary>
    public void MiniGameSelect()
    {
        PlayerPrefs.SetInt("ComeBackFromGame", 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ミニゲーム選択画面から離れた時は戻ってこなくてよし
    /// </summary>
    public void MiniGameUnSelect()
    {
        PlayerPrefs.SetInt("ComeBackFromGame", 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ルームコードUIをオンラインモードの時は表示、オフラインは非表示に
    /// </summary>
    public void DisplayRoomCode()
    {
        if (isOnlineMode)
        {
            roomCodeUI.SetActive(true);
        }
        else
        {
            roomCodeUI.SetActive(false);
        }
    }

    /// <summary>
    /// パーティモードの使用に変更
    /// </summary>
    public void ChangePartyMode()
    {
        // パーティモード時はReadyUIから戻るボタン削除
        if (GameManager.instance.isPartyMode) 
            backButton.SetActive(false);

        // ミニゲームアイコンを非表示
        minigameIcon.SetActive(false);

        // アイコンをパーティモードに
        partyIcon.SetActive(true);
    }

    public void ChangeMinigameMode()
    {
        // パーティモード時はReadyUIから戻るボタン削除
        if (!GameManager.instance.isPartyMode)
            backButton.SetActive(true);

        // ミニゲームアイコンを非表示
        minigameIcon.SetActive(true);

        // アイコンをパーティモードに
        partyIcon.SetActive(false);
    }

    /// <summary>
    /// セレクトから戻るボタン押したときの処理
    /// </summary>
    public void BackSelectUIButton() {
        if(PlayerManager.instance != null) {
            PlayerManager.instance.DestroyPlayerList();
            GameDataManager.instance.AllToriListEliminate();
        }
    }

    /// <summary>
    /// 次のシーンの名前を設定する
    /// </summary>
    /// <param name="nextName"></param>
    public void SetNextSceneName(string nextName) {
        NextSceneText = nextName;
        SetExplanationText();
        SetIsActiveMinigameImage();
    }

    /// <summary>
    /// 次のシーンに合わせて説明テキストを変える
    /// </summary>
    public void SetExplanationText() {
        switch(NextSceneText) {
            //レース
            case "Race":
                ExplanationText.text = RaceGameExplanationText;
                break;
                //ドロップ
            case "DropBird":
                ExplanationText.text = DropGameExplanationText;
                break;
            case "DiceGame":
                ExplanationText.text = DiceGameExplanationText;
                break;
        }
    }
    /// <summary>
    /// 次のシーンに合わせて説明画像を変える
    /// </summary>
    public void SetIsActiveMinigameImage() {
        //一度全部アクティブを切る
        for(int i = 0;i<MiniGameImage.Count;i++) {
            MiniGameImage[i].SetActive(false);
        }

        //シーンの名前に合わせたイメージを点灯
        switch(NextSceneText) {
            //レース
            case "Race":
                MiniGameImage[0].SetActive(true);
                break;
                //ドロップ
            case "DropBird":
                MiniGameImage[1].SetActive(true);
                break;
                //ダイス
            case "DiceGame":
                MiniGameImage[2].SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 名前を取る
    /// </summary>
    /// <returns></returns>
    public string GetNameInput() {
        return _nameInputField.text;
    }

    /// <summary>
    /// パーティモードのボタンが押されたときの処理
    /// </summary>
    public void PressPartyModeButton() {

        if (GameManager.instance != null)
        {
            GameManager.instance.SetPartyMode(true);
        }

        // ついでに PlayerPrefs のパーティ中フラグも立てておく（保険）
        //PlayerPrefs.SetInt(PartyModeManager.PREF_PARTY_RUNNING, 1);
        //PlayerPrefs.Save();

        if (PartyModeManager.instance != null) {
            //パーティモードマネージャーがゲームリストを作成
            PartyModeManager.instance.MakeGameList();
        } 
    }

    /// <summary>
    /// ローカルモードの時にルームコードの表示をなくす
    /// </summary>
    public void HideRoomCodePlate() {
        RoomNumberPlate.SetActive(false);
    }

    /// <summary>
    /// オンラインモードの時にルームコードの表示を出す
    /// </summary>
    public void ShowRoomCodePlate() {
        RoomNumberPlate.SetActive(true);
    }
}
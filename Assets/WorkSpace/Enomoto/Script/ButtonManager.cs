using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;



public class ButtonManager : MonoBehaviour
{
    [Header("UI定義")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject modeUI;
    [SerializeField] private GameObject minigameSelectUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject netWorkUI;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject playerSelectUI;
    [SerializeField] private GameObject gameReadyUI;

    [Header("オンライン定義")]
    [SerializeField] private GameObject online;
    [SerializeField] private GameObject offline;

    [Header("エラーログ")]
    [SerializeField] private GameObject errorMinigameSelect;

    [Header("ネットワーク関連")]
    [SerializeField] TMP_InputField roomCodeInputField; // ルームコード入力欄
    [SerializeField] private TextMeshProUGUI connectionStatusText; // 接続状態表示
    [SerializeField] private UnityEngine.UI.Button createRoomButton; // ルーム作成ボタン
    [SerializeField] private UnityEngine.UI.Button joinRoomButton; // ルーム参加ボタン


    // 開いたUIを保存して戻れるように
    private Stack<GameObject> uiHistory = new Stack<GameObject>();
    //ホストかどうか
    private bool isHost;

    private void Start()
    {
        // ゲームから戻ってきたかどうかでUIを切り替え
        if (GameDataManager.instance != null && GameDataManager.instance.comeBackFromGame)
        {
            titleUI.SetActive(false);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(true);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
            uiHistory.Push(modeUI);
            uiHistory.Push(minigameSelectUI);

            GameDataManager.instance.comeBackFromGame = false;

            PlayStyle();
        }
        else
        {
            titleUI.SetActive(true);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(false);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
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

        if (openUI == modeUI)
        {
            GameDataManager.instance.playOnline = false;
        }
        else
        {
            GameDataManager.instance.playOnline = true;
        }
    }

    public void Back()
    {
        if (uiHistory.Count > 1)
        {
            GameObject closing = uiHistory.Pop();
            closing.SetActive(false);

            GameObject previous = uiHistory.Peek();
            previous.SetActive(true);
        }
    }

    public void PlayStyle()
    {
        if (GameDataManager.instance.playOnline)
        {
            online.SetActive(true);
            offline.SetActive(false);
            //ゲームマネージャーの内部的なfalseとtrueを変える
            GameManager.instance.SetIsOnline(true);
        }
        else
        {
            online.SetActive(false);
            offline.SetActive(true);
            //ゲームマネージャーの内部的なfalseとtrueを変える
            GameManager.instance.SetIsOnline(false);
        }
    }

    // ログオープン
    public void OpenObject(GameObject openLog)
    {
        openLog.SetActive(true);
    }

    // ログクローズ
    public void CloseObject(GameObject closeLog)
    {
        closeLog.SetActive(false);
    }

    public void OnClickStartGame(string sceneName) {

        //自分がマスタークライアントだったら全員に送る
        PhotonView photonView = gameObject.GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("StartGame", RpcTarget.All, sceneName);
            Debug.Log("マスタークライアントなので全員におくりますた");
        }

        if (!GameManager.instance.IsOnline())
            StartGame(sceneName);
    }

    [PunRPC]
    // ミニゲーム開始
    public void StartGame(string sceneName) {
        // プレイヤーがいないのにゲームは始められませんでしょう
        if (GameDataManager.instance.GetEntriedPlayerCount() == 0) return;


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
        GameManager.instance.SetIsOnline(true);
    }

    /// <summary>
    /// オフラインにセット
    /// </summary>
    public void SetOffline() {
        GameManager.instance.SetIsOnline(false);
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
        if (GameManager.instance.IsOnline()&& !isHost) {
            TitleManager.instance.SetActiveNextButton(false);
        }
    }

    /// <summary>
    /// セレクトから戻るボタン押したときの処理
    /// </summary>
    public void BackSelectUIButton() {
        if(PlayerManager.instance != null) {
            PlayerManager.instance.DestroyPlayerList();
        }
    }
}

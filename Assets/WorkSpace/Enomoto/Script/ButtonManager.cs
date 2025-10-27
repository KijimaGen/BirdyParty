/**
 * @file ButtonManager.cs
 * @brief UI操作とネットワーク機能を管理するクラス
 * タイトル画面のボタン操作とオンラインマルチプレイ機能を提供
 * @author Enomoto + Kiro (ネットワーク機能追加)
 * @date 2025/10/20
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// UI操作とネットワーク機能を管理するクラス
/// タイトル画面のボタン制御とオンラインマルチプレイ機能を提供
/// </summary>
[PunRPC]
public class ButtonManager : MonoBehaviour
{
    [Header("UIボタン")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject modeUI;
    [SerializeField] private GameObject minigameSelectUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject netWorkUI;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject playerSelectUI;
    [SerializeField] private GameObject gameReadyUI;

    [Header("オンラインかオフラインかのボタン")]
    [SerializeField] private GameObject online;
    [SerializeField] private GameObject offline;

    [Header("ミニゲームがない時の物")]
    [SerializeField] private GameObject errorMinigameSelect;

    [Header("ネットワーク関連")]
    [SerializeField] TMP_InputField roomCodeInputField; // ルームコード入力欄
    [SerializeField] private TextMeshProUGUI connectionStatusText; // 接続状態表示
    [SerializeField] private UnityEngine.UI.Button createRoomButton; // ルーム作成ボタン
    [SerializeField] private UnityEngine.UI.Button joinRoomButton; // ルーム参加ボタン


    
    private Stack<GameObject> uiHistory = new Stack<GameObject>();

    
    
    private void Start()
    {
        // ゲームから戻ってきた場合はどのUIに切り替える
        if (GameDataManager.Instance != null && GameDataManager.Instance.comeBackFromGame)
        {
            titleUI.SetActive(false);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(true);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
            uiHistory.Push(modeUI);
            uiHistory.Push(minigameSelectUI);

            GameDataManager.Instance.comeBackFromGame = false;

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
            GameDataManager.Instance.playOnline = false;
        }
        else
        {
            GameDataManager.Instance.playOnline = true;
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
        if (GameDataManager.Instance.playOnline)
        {
            online.SetActive(true);
            offline.SetActive(false);
            //ゲームマネージャーのオンライン設定をオンにする
            GameManager.instance.SetIsOnline(true);
        }
        else
        {
            online.SetActive(false);
            offline.SetActive(true);
            //ゲームマネージャーのオンライン設定をオフにする
            GameManager.instance.SetIsOnline(false);
        }
    }

    // ログオープン
    public void OpenLog(GameObject openLog)
    {
        openLog.SetActive(true);
    }

    // ログクローズ
    public void CloseLog(GameObject closeLog)
    {
        closeLog.SetActive(false);
    }

    public void OnClickStartGame(string sceneName) {
        _ = AudioManager.instance.PlaySE(0);
        //自分がマスタークライアントだったら全員に送る
        PhotonView photonView = gameObject.GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("StartGame", RpcTarget.All, sceneName);
            Debug.Log("マスタークライアントなので全員におくりますた");
        }

        //ローカルでまわしててもローカルで始める
        if (!GameManager.instance.IsOnline()) {
            StartGame(sceneName);
        }

    }

    [PunRPC]
    // ミニゲーム開始
    public void StartGame(string sceneName){
        // プレイヤーがいないのにゲームは始められませんでしょう
        if (GameDataManager.Instance.GetEntriedPlayerCount() == 0 ) return;
        

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.selectedMiniGame = sceneName;
            GameDataManager.Instance.comeBackFromGame = true;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void OnExit()
    {
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
            UpdateNetworkStatus("ルームを作成中...");
            NetworkManager.instance.CreateRandomRoom();
        } else {
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
            UpdateNetworkStatus("SimpleNetworkManagerが見つかりません");
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
    /// デバッグ用：TEST2ルームに参加
    /// </summary>
    [ContextMenu("TEST2ルームに参加")]
    public void JoinTestRoom() {
        JoinRoom("TEST2");
    }

    /// <summary>
    /// TEST2ルームを作成または参加（自動判定）
    /// </summary>
    [ContextMenu("🎯 TEST2作成/参加")]
    public void CreateOrJoinTestRoom() {
        if (NetworkManager.instance != null) {
            UpdateNetworkStatus("TEST2ルームを作成/参加中...");
            NetworkManager.instance.CreateRandomRoom();
        } else {
            UpdateNetworkStatus("SimpleNetworkManagerが見つかりません");
        }
    }

    /// <summary>
    /// 安全版：Photonを使わないテスト
    /// </summary>
    [ContextMenu("🛡 安全版テスト")]
    public void SafeTest() {
        Debug.Log("=== 🛡 安全版テスト ===");
        
        // SimpleNetworkManagerの存在確認
        if (NetworkManager.instance == null) {
            Debug.LogError("❌ NetworkManager.instance が見つかりません");
            UpdateNetworkStatus("NetworkManager");
            return;
        }
        
        Debug.Log("✅ NetworkManager.instance 存在確認OK");
        
        // InputFieldの確認
        if (roomCodeInputField == null) {
            Debug.LogWarning("⚠️ roomCodeInputField が設定されていません");
            UpdateNetworkStatus("InputFieldを設定してください");
        } else {
            Debug.Log($"✅ InputField OK: '{roomCodeInputField.text}'");
            UpdateNetworkStatus($"InputField設定済み: '{roomCodeInputField.text}'");
        }
        
        Debug.Log("🎯 基本設定は正常です");
    }

    /// <summary>
    /// デバッグ用：InputField設定チェック
    /// </summary>
    [ContextMenu("InputField設定チェック")]
    public void CheckInputFieldSettings() {
        Debug.Log("=== InputField設定チェック ===");
        
        if (roomCodeInputField == null) {
            Debug.LogError("❌ roomCodeInputField が設定されていません！");
            UpdateNetworkStatus("InputFieldを設定してください");
        } else {
            Debug.Log($"✅ InputField設定OK: {roomCodeInputField.name}");
            Debug.Log($"   現在の値: '{roomCodeInputField.text}'");
            Debug.Log($"   文字制限: {roomCodeInputField.characterLimit}");
            Debug.Log($"   アクティブ: {roomCodeInputField.gameObject.activeInHierarchy}");
            Debug.Log($"   インタラクト可能: {roomCodeInputField.interactable}");
            UpdateNetworkStatus($"InputField OK: '{roomCodeInputField.text}'");
        }
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
            } else {
                Debug.Log("🚫 現在ルームに参加していません");
                UpdateNetworkStatus("ルームに参加していません");
            }
        } catch (System.Exception e) {
            Debug.LogError($"❌ Photon参照エラー: {e.Message}");
            UpdateNetworkStatus("Photonの設定に問題があります");
        }
        
        // SimpleNetworkManagerの状態
        if (NetworkManager.instance != null) {
            Debug.Log($"📝 現在のルームコード: '{NetworkManager.instance.GetCurrentRoomCode()}'");
        } else {
            Debug.LogWarning("SimpleNetworkManager.instance が見つかりません");
        }
    }
}

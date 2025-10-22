/**
 * @file NetworkUIController.cs
 * @brief ネットワーク関連のUI制御クラス
 * 接続状態に応じてボタンの有効/無効を切り替える
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ネットワーク関連のUI制御を行うクラス
/// Photonの接続状態に応じてボタンやUIの状態を管理する
/// </summary>
public class NetworkUIController : MonoBehaviour
{
    [Header("ネットワークボタン")]
    [SerializeField] private Button createRoomButton;      // ルーム作成ボタン
    [SerializeField] private Button joinRoomButton;        // ルーム参加ボタン
    [SerializeField] private Button debugJoinButton;       // デバッグ用参加ボタン
    
    [Header("入力フィールド")]
    [SerializeField] private TMP_InputField roomCodeInput; // ルームコード入力フィールド
    
    [Header("状態表示")]
    [SerializeField] private TextMeshProUGUI statusText;   // 接続状態表示テキスト
    [SerializeField] private GameObject loadingPanel;      // ローディング表示パネル
    
    [Header("デバッグ設定")]
    [SerializeField] private string debugRoomCode = "TEST2";    // デバッグ用ルームコード（有効文字のみ使用）
    [SerializeField] private Button createTestRoomButton;       // テスト用部屋作成ボタン
    
    private NetworkManager networkManager;
    private bool isUILocked = false; // UI操作をロックするフラグ

    void Start()
    {
        // NetworkManagerを取得
        networkManager = NetworkManager.instance;
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }

        // ボタンイベントを設定
        SetupButtonEvents();
        
        // ネットワークイベントを購読
        SubscribeToNetworkEvents();
        
        // 初期UI状態を設定
        UpdateUIState();
    }

    void OnDestroy()
    {
        // イベント購読を解除
        UnsubscribeFromNetworkEvents();
    }

    /// <summary>
    /// ボタンイベントを設定
    /// </summary>
    private void SetupButtonEvents()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            
        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            
        if (debugJoinButton != null)
            debugJoinButton.onClick.AddListener(OnDebugJoinClicked);
            
        if (createTestRoomButton != null)
            createTestRoomButton.onClick.AddListener(OnCreateTestRoomClicked);
    }

    /// <summary>
    /// ネットワークイベントを購読
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        NetworkManager.OnConnectedToServer += OnConnectedToServer;
        NetworkManager.OnConnectionStatusChanged += UpdateUIState;
        NetworkManager.OnRoomCodeGenerated += OnRoomCreated;
        NetworkManager.OnRoomJoined += OnRoomJoined;
        NetworkManager.OnRoomJoinFailed += OnRoomJoinFailed;
    }

    /// <summary>
    /// ネットワークイベント購読を解除
    /// </summary>
    private void UnsubscribeFromNetworkEvents()
    {
        NetworkManager.OnConnectedToServer -= OnConnectedToServer;
        NetworkManager.OnConnectionStatusChanged -= UpdateUIState;
        NetworkManager.OnRoomCodeGenerated -= OnRoomCreated;
        NetworkManager.OnRoomJoined -= OnRoomJoined;
        NetworkManager.OnRoomJoinFailed -= OnRoomJoinFailed;
    }

    /// <summary>
    /// ルーム作成ボタンがクリックされた時の処理
    /// </summary>
    private void OnCreateRoomClicked()
    {
        if (networkManager != null && networkManager.CanUseNetworkUI())
        {
            UpdateStatus("ルームを作成中...");
            LockUI(true);
            networkManager.CreateRandomRoom();
        }
        else
        {
            UpdateStatus("Photonに接続中です。しばらくお待ちください");
        }
    }

    /// <summary>
    /// ルーム参加ボタンがクリックされた時の処理
    /// </summary>
    private void OnJoinRoomClicked()
    {
        if (networkManager != null && networkManager.CanUseNetworkUI())
        {
            string roomCode = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : "";
            
            if (string.IsNullOrEmpty(roomCode))
            {
                UpdateStatus("ルームコードを入力してください");
                return;
            }
            
            if (roomCode.Length != 5)
            {
                UpdateStatus("ルームコードは5文字で入力してください");
                return;
            }
            
            UpdateStatus($"ルーム {roomCode} に参加中...");
            LockUI(true);
            networkManager.JoinRoomWithCode(roomCode);
        }
        else
        {
            UpdateStatus("Photonに接続中です。しばらくお待ちください");
        }
    }

    /// <summary>
    /// デバッグ用参加ボタンがクリックされた時の処理
    /// </summary>
    private void OnDebugJoinClicked()
    {
        if (!string.IsNullOrEmpty(debugRoomCode))
        {
            if (roomCodeInput != null)
            {
                roomCodeInput.text = debugRoomCode;
            }
            OnJoinRoomClicked();
        }
        else
        {
            UpdateStatus("デバッグ用ルームコードが設定されていません");
        }
    }

    /// <summary>
    /// テスト用部屋作成ボタンがクリックされた時の処理
    /// </summary>
    private void OnCreateTestRoomClicked()
    {
        if (networkManager != null && networkManager.CanUseNetworkUI())
        {
            UpdateStatus("テスト用部屋を作成中...");
            LockUI(true);
            networkManager.CreateTestRoom();
        }
        else
        {
            UpdateStatus("Photonに接続中です。しばらくお待ちください");
        }
    }

    /// <summary>
    /// サーバー接続完了時の処理
    /// </summary>
    private void OnConnectedToServer()
    {
        UpdateStatus("サーバーに接続完了！ルーム操作が可能です");
        LockUI(false);
    }

    /// <summary>
    /// ルーム作成完了時の処理
    /// </summary>
    /// <param name="roomCode">作成されたルームコード</param>
    private void OnRoomCreated(string roomCode)
    {
        UpdateStatus($"ルーム作成完了！コード: {roomCode}");
        LockUI(false);
    }

    /// <summary>
    /// ルーム参加成功時の処理
    /// </summary>
    /// <param name="roomCode">参加したルームコード</param>
    private void OnRoomJoined(string roomCode)
    {
        UpdateStatus($"ルーム {roomCode} に参加しました！");
        LockUI(false);
        
        // ここで次のシーンに移動したり、ゲーム開始処理を行う
        // 例: SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// ルーム参加失敗時の処理
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void OnRoomJoinFailed(string errorMessage)
    {
        UpdateStatus($"参加失敗: {errorMessage}");
        LockUI(false);
    }

    /// <summary>
    /// UI状態を更新（ボタンの有効/無効切り替え）
    /// </summary>
    private void UpdateUIState()
    {
        bool canUseNetwork = networkManager != null && networkManager.CanUseNetworkUI() && !isUILocked;
        
        // ボタンの有効/無効を設定
        if (createRoomButton != null)
            createRoomButton.interactable = canUseNetwork;
            
        if (joinRoomButton != null)
            joinRoomButton.interactable = canUseNetwork;
            
        if (debugJoinButton != null)
            debugJoinButton.interactable = canUseNetwork;
            
        if (createTestRoomButton != null)
            createTestRoomButton.interactable = canUseNetwork;
            
        // 入力フィールドの有効/無効を設定
        if (roomCodeInput != null)
            roomCodeInput.interactable = canUseNetwork;
    }

    /// <summary>
    /// UIをロック/アンロックする
    /// </summary>
    /// <param name="lockUI">ロックする場合true</param>
    private void LockUI(bool lockUI)
    {
        isUILocked = lockUI;
        
        // ローディング表示の切り替え
        if (loadingPanel != null)
            loadingPanel.SetActive(lockUI);
            
        UpdateUIState();
    }

    /// <summary>
    /// ステータステキストを更新
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"NetworkUI: {message}");
    }

    /// <summary>
    /// 現在の接続状態を取得（デバッグ用）
    /// </summary>
    [ContextMenu("接続状態を確認")]
    public void CheckConnectionStatus()
    {
        if (networkManager != null)
        {
            string status = NetworkHelper.GetConnectionStatus();
            UpdateStatus(status);
        }
        else
        {
            UpdateStatus("NetworkManagerが見つかりません");
        }
    }
}
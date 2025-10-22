/**
 * @file SimpleNetworkManager.cs
 * @brief シンプルで確実に動作するネットワーク管理クラス
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// シンプルで確実に動作するPUN2ネットワーク管理クラス
/// エラーを最小限に抑えた安全な実装
/// </summary>
public class SimpleNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("基本設定")]
    [SerializeField] private GameObject playerPrefab;
    
    [Header("デバッグ設定")]
    [SerializeField] private string testRoomCode = "TEST2";
    [SerializeField] private TextMeshProUGUI statusText;
    
    // シングルトン
    public static SimpleNetworkManager instance;
    
    // 現在の状態
    private string currentRoomCode = "";
    private bool isProcessing = false;

    void Awake()
    {
        // シングルトン設定
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateStatus("Photonに接続中...");
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// マスターサーバー接続完了
    /// </summary>
    public override void OnConnectedToMaster()
    {
        UpdateStatus("サーバー接続完了！");
        Debug.Log("✅ Photonマスターサーバーに接続完了");
    }

    /// <summary>
    /// ルーム作成（ランダムコード）
    /// </summary>
    [ContextMenu("🎲 ランダムルーム作成")]
    public void CreateRandomRoom()
    {
        if (!CanPerformAction()) return;
        
        string roomCode = GenerateSimpleRoomCode();
        CreateRoomWithCode(roomCode);
    }

    /// <summary>
    /// テスト用ルーム作成
    /// </summary>
    [ContextMenu("🧪 TEST2ルーム作成")]
    public void CreateTestRoom()
    {
        if (!CanPerformAction()) return;
        
        CreateRoomWithCode(testRoomCode);
    }

    /// <summary>
    /// TEST2ルームを作成または参加（自動判定）
    /// </summary>
    [ContextMenu("🎯 TEST2ルーム作成/参加")]
    public void CreateOrJoinTestRoom()
    {
        if (!CanPerformAction()) return;
        
        CreateOrJoinRoom(testRoomCode);
    }

    /// <summary>
    /// 指定されたルームを作成または参加（自動判定）
    /// </summary>
    /// <param name="roomCode">ルームコード</param>
    public void CreateOrJoinRoom(string roomCode)
    {
        if (!CanPerformAction()) return;
        
        if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 5)
        {
            UpdateStatus("❌ 無効なルームコードです");
            return;
        }
        
        UpdateStatus($"ルーム {roomCode} を作成/参加中...");
        currentRoomCode = roomCode.ToUpper();
        isProcessing = true;
        
        // まず作成を試行（失敗したら自動で参加）
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };
        
        Debug.Log($"🎯 ルーム '{currentRoomCode}' の作成を試行中（既存の場合は自動参加）");
        PhotonNetwork.CreateRoom(currentRoomCode, options);
    }

    /// <summary>
    /// ルーム参加
    /// </summary>
    /// <param name="roomCode">参加するルームコード</param>
    public void JoinRoom(string roomCode)
    {
        if (!CanPerformAction()) return;
        
        if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 5)
        {
            UpdateStatus("❌ 無効なルームコードです");
            return;
        }
        
        UpdateStatus($"ルーム {roomCode} に参加中...");
        currentRoomCode = roomCode.ToUpper();
        isProcessing = true;
        
        PhotonNetwork.JoinRoom(currentRoomCode);
    }

    /// <summary>
    /// 指定されたコードでルーム作成
    /// </summary>
    private void CreateRoomWithCode(string roomCode)
    {
        UpdateStatus($"ルーム {roomCode} を作成中...");
        currentRoomCode = roomCode.ToUpper();
        isProcessing = true;
        
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };
        
        PhotonNetwork.CreateRoom(currentRoomCode, options);
    }

    /// <summary>
    /// ルーム作成成功
    /// </summary>
    public override void OnCreatedRoom()
    {
        UpdateStatus($"✅ ルーム作成成功: {currentRoomCode}");
        Debug.Log($"✅ ルーム作成成功: {currentRoomCode}");
        isProcessing = false;
    }

    /// <summary>
    /// ルーム作成失敗
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"ルーム作成失敗: {message} (コード: {returnCode})");
        
        if (returnCode == ErrorCode.GameIdAlreadyExists)
        {
            Debug.Log($"✅ 部屋 '{currentRoomCode}' が既に存在します。自動で参加します！");
            UpdateStatus($"部屋 '{currentRoomCode}' が既に存在 → 自動参加中...");
            PhotonNetwork.JoinRoom(currentRoomCode);
        }
        else
        {
            Debug.LogError($"❌ ルーム作成エラー: {message}");
            UpdateStatus($"❌ ルーム作成失敗: {message}");
            isProcessing = false;
        }
    }

    /// <summary>
    /// ルーム参加成功
    /// </summary>
    public override void OnJoinedRoom()
    {
        UpdateStatus($"✅ ルーム参加成功: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"✅ ルーム参加成功: {PhotonNetwork.CurrentRoom.Name}");
        
        // プレイヤー生成
        if (playerPrefab != null)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        }
        
        isProcessing = false;
    }

    /// <summary>
    /// ルーム参加失敗
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ ルーム参加失敗: {message}");
        
        string errorMsg = returnCode switch
        {
            ErrorCode.GameDoesNotExist => "ルームが見つかりません",
            ErrorCode.GameFull => "ルームが満員です",
            _ => $"参加エラー: {message}"
        };
        
        UpdateStatus($"❌ {errorMsg}");
        isProcessing = false;
    }

    /// <summary>
    /// ルーム退出完了
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("ルーム退出完了 - ロビーに参加中...");
        UpdateStatus("ルーム退出完了 - ロビーに参加中...");
        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// ロビー参加完了
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("✅ ロビー参加完了 - マッチメイキング可能");
        UpdateStatus("✅ ロビー参加完了");
        isProcessing = false;
    }

    /// <summary>
    /// 切断処理
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"切断されました: {cause}");
        Debug.Log($"Photonから切断: {cause}");
        isProcessing = false;
        
        // 自動再接続
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            Debug.Log("自動再接続中...");
            UpdateStatus("自動再接続中...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    /// <summary>
    /// アクション実行可能かチェック
    /// </summary>
    private bool CanPerformAction()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("⚠️ Photonに接続中です...");
            return false;
        }
        
        if (isProcessing)
        {
            UpdateStatus("⚠️ 処理中です...");
            return false;
        }
        
        // GameServerにいる場合はリセットが必要
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined)
        {
            Debug.Log("🔄 GameServerにいるため、マスターサーバーに戻ります");
            UpdateStatus("GameServerにいるため、マスターサーバーに戻り中...");
            ResetToMasterServer();
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// シンプルなルームコード生成
    /// </summary>
    private string GenerateSimpleRoomCode()
    {
        string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string result = "";
        
        for (int i = 0; i < 5; i++)
        {
            result += chars[Random.Range(0, chars.Length)];
        }
        
        return result;
    }

    /// <summary>
    /// ステータス更新
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"SimpleNetworkManager: {message}");
    }

    /// <summary>
    /// 現在のルームコード取得
    /// </summary>
    public string GetCurrentRoomCode()
    {
        return currentRoomCode;
    }

    /// <summary>
    /// 現在の状況確認
    /// </summary>
    [ContextMenu("🔍 状況確認")]
    public void CheckStatus()
    {
        Debug.Log("=== SimpleNetworkManager 状況確認 ===");
        Debug.Log($"接続状態: {PhotonNetwork.NetworkClientState}");
        Debug.Log($"準備完了: {PhotonNetwork.IsConnectedAndReady}");
        Debug.Log($"ルーム内: {PhotonNetwork.InRoom}");
        Debug.Log($"現在のルーム: {(PhotonNetwork.CurrentRoom?.Name ?? "なし")}");
        Debug.Log($"処理中: {isProcessing}");
        
        // 状態に応じたアドバイス
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined)
        {
            Debug.LogWarning("⚠️ GameServerにいます。マスターサーバーに戻る必要があります");
            UpdateStatus("⚠️ GameServerにいます");
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("✅ マッチメイキング可能な状態です");
            UpdateStatus("✅ マッチメイキング可能");
        }
    }

    /// <summary>
    /// マスターサーバーに戻る
    /// </summary>
    [ContextMenu("🔄 マスターサーバーに戻る")]
    public void ResetToMasterServer()
    {
        Debug.Log("🔄 マスターサーバーに戻ります");
        UpdateStatus("マスターサーバーに戻り中...");
        
        isProcessing = true;
        currentRoomCode = "";
        
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("ルームから退出中...");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("既にルーム外です。ロビーに参加中...");
            PhotonNetwork.JoinLobby();
        }
    }

    /// <summary>
    /// 完全リセット（最終手段）
    /// </summary>
    [ContextMenu("💥 完全リセット")]
    public void CompleteReset()
    {
        Debug.Log("💥 Photon接続を完全リセットします");
        UpdateStatus("完全リセット中...");
        
        isProcessing = false;
        currentRoomCode = "";
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            // 既に切断されている場合は直接再接続
            UpdateStatus("再接続中...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }
}
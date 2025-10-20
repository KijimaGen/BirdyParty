using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

/// <summary>
/// PUN2を使用したネットワーク管理クラス
/// ランダムな5文字のルームコードでルーム作成・参加を行う
/// </summary>
public class NetworkManager : MonoBehaviourPunCallbacks {
    [Header("プレイヤー設定")]
    [SerializeField] private GameObject playerPrefab; // ルーム参加時に生成するプレイヤーのプレハブ

    // ルーム管理用の変数
    [SerializeField]
    private string currentRoomCode = ""; // 現在のルームコード（5文字）
    private bool isCreatingRoom = false; // ルーム作成中かどうかのフラグ
    private bool isJoiningRoom = false;  // ルーム参加中かどうかのフラグ
    
    // UI更新用のイベント（他のスクリプトから購読可能）
    public static event Action<string> OnRoomCodeGenerated; // ルームコードが生成された時
    public static event Action<string> OnRoomJoined;        // ルーム参加成功時
    public static event Action<string> OnRoomJoinFailed;    // ルーム参加失敗時
    public static event Action OnConnectedToServer;         // サーバー接続完了時

    //自身のインスタンス
    public static NetworkManager instance;

    /// <summary>
    /// ゲーム開始時の初期化処理
    /// Photonサーバーへの接続を開始する
    /// </summary>
    void Start() {
        Debug.Log("Photonに接続中...");
        PhotonNetwork.ConnectUsingSettings(); // Photon設定を使用して接続開始
        instance = this;
    }

    /// <summary>
    /// Photonのマスターサーバーに接続完了時に呼ばれる
    /// この時点でルーム作成・参加が可能になる
    /// </summary>
    public override void OnConnectedToMaster() {
        Debug.Log("サーバーに接続完了！");
        OnConnectedToServer?.Invoke(); // UI更新用イベントを発火
    }

    /// <summary>
    /// ランダムな5文字のコードで新しいルームを作成する
    /// ホストプレイヤーが使用する機能
    /// </summary>
    public void CreateRandomRoom() {
        // Photonに接続済みかチェック
        if (!PhotonNetwork.IsConnectedAndReady) {
            Debug.LogWarning("Photonに接続されていません");
            return;
        }

        // ランダムな5文字のルームコードを生成
        currentRoomCode = RoomCodeGenerator.GenerateRoomCode();
        isCreatingRoom = true; // ルーム作成中フラグをON
        
        // ルーム設定を作成
        RoomOptions roomOptions = new RoomOptions {
            MaxPlayers = GameConst.PLAYER_MAX, // 最大プレイヤー数
            IsVisible = true,  // ルーム一覧に表示する
            IsOpen = true      // 参加可能にする
        };
        
        Debug.Log($"ルームを作成中: {currentRoomCode}");
        PhotonNetwork.CreateRoom(currentRoomCode, roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// 指定されたルームコードでルームに参加する
    /// ゲストプレイヤーが使用する機能
    /// </summary>
    /// <param name="roomCode">参加したいルームの5文字コード</param>
    public void JoinRoomWithCode(string roomCode) {
        // Photonに接続済みかチェック
        if (!PhotonNetwork.IsConnectedAndReady) {
            Debug.LogWarning("Photonに接続されていません");
            return;
        }

        // ルームコードの基本的な形式チェック
        if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 5) {
            Debug.LogWarning("無効なルームコードです");
            OnRoomJoinFailed?.Invoke("無効なルームコード");
            return;
        }

        // ルームコードに使用可能な文字かチェック
        if (!RoomCodeGenerator.IsValidRoomCode(roomCode)) {
            Debug.LogWarning("ルームコードに無効な文字が含まれています");
            OnRoomJoinFailed?.Invoke("無効な文字が含まれています");
            return;
        }

        // ルームコードを大文字に統一して保存
        currentRoomCode = roomCode.ToUpper();
        isJoiningRoom = true; // ルーム参加中フラグをON
        
        Debug.Log($"ルームに参加中: {currentRoomCode}");
        PhotonNetwork.JoinRoom(currentRoomCode); // Photonのルーム参加処理を実行
    }

    /// <summary>
    /// ルーム作成が成功した時に呼ばれる
    /// UIにルームコードを表示するためのイベントを発火
    /// </summary>
    public override void OnCreatedRoom() {
        Debug.Log($"ルーム作成成功: {currentRoomCode}");
        OnRoomCodeGenerated?.Invoke(currentRoomCode); // UIにルームコードを通知
    }

    /// <summary>
    /// ルーム作成が失敗した時に呼ばれる
    /// 重複エラーの場合は自動的に新しいコードで再試行
    /// </summary>
    /// <param name="returnCode">エラーコード</param>
    /// <param name="message">エラーメッセージ</param>
    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.LogError($"ルーム作成失敗: {message}");
        isCreatingRoom = false; // ルーム作成中フラグをOFF
        
        // ルームコードが重複していた場合は新しいコードで再試行
        if (returnCode == ErrorCode.GameIdAlreadyExists) {
            Debug.Log("ルームコードが重複しています。新しいコードを生成中...");
            CreateRandomRoom(); // 自動的に新しいコードで再試行
        }
    }

    /// <summary>
    /// ルーム参加が成功した時に呼ばれる
    /// プレイヤーオブジェクトを生成してゲーム開始準備を行う
    /// </summary>
    public override void OnJoinedRoom() {
        Debug.Log($"ルームに参加しました: {PhotonNetwork.CurrentRoom.Name}");
        OnRoomJoined?.Invoke(PhotonNetwork.CurrentRoom.Name); // UIに参加成功を通知
        
        // プレイヤーオブジェクトを生成（ネットワーク同期）
        if (playerPrefab != null) {
            // ランダムな位置にプレイヤーを配置
            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-2, 2), 0, UnityEngine.Random.Range(-2, 2));
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.Euler(0, -90, 0));
        }
        
        // フラグをリセット
        isCreatingRoom = false;
        isJoiningRoom = false;
    }

    /// <summary>
    /// ルーム参加が失敗した時に呼ばれる
    /// エラーコードに応じてわかりやすいメッセージを表示
    /// </summary>
    /// <param name="returnCode">エラーコード</param>
    /// <param name="message">エラーメッセージ</param>
    public override void OnJoinRoomFailed(short returnCode, string message) {
        Debug.LogError($"ルーム参加失敗: {message}");
        isJoiningRoom = false; // ルーム参加中フラグをOFF
        
        // エラーコードに応じてユーザーフレンドリーなメッセージを作成
        string errorMessage = returnCode switch {
            ErrorCode.GameDoesNotExist => "ルームが見つかりません",
            ErrorCode.GameFull => "ルームが満員です",
            ErrorCode.GameClosed => "ルームが閉じられています",
            _ => $"参加エラー: {message}"
        };
        
        OnRoomJoinFailed?.Invoke(errorMessage); // UIにエラーメッセージを通知
    }

    /// <summary>
    /// 現在のルームコードを取得する
    /// UIでルームコードを表示する際に使用
    /// </summary>
    /// <returns>現在のルームコード（5文字）</returns>
    public string GetCurrentRoomCode() {
        return currentRoomCode;
    }

    /// <summary>
    /// 現在のルームから退出する
    /// ゲーム終了時やメニューに戻る時に使用
    /// </summary>
    public void LeaveRoom() {
        if (PhotonNetwork.InRoom) {
            PhotonNetwork.LeaveRoom(); // Photonのルーム退出処理
        }
    }

    /// <summary>
    /// ルーム退出が完了した時に呼ばれる
    /// ルームコードをリセットして初期状態に戻す
    /// </summary>
    public override void OnLeftRoom() {
        Debug.Log("ルームを退出しました");
        currentRoomCode = ""; // ルームコードをクリア
    }
}
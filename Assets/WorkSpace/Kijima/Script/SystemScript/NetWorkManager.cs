using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;

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
    public static event Action OnConnectionStatusChanged;   // 接続状態変更時

    //自身のインスタンス
    public static NetworkManager instance;

    [Header("デバッグ用設定")]
    [SerializeField] private string debugRoomCode = "TEST2"; // インスペクターで部屋番号を入力するためのフィールド（有効文字のみ使用）
    [SerializeField] private bool autoConnectOnStart = true; // 開始時に自動接続するか
    [SerializeField] private bool editorAutoHost = true; // エディター実行時は自動でホストになる
    [SerializeField] private TextMeshProUGUI connectionStatusText; // 接続状態表示用テキスト（オプション）
    //ルームナンバー
    [SerializeField]
    private TextMeshProUGUI roomNumber;

    /// <summary>
    /// 接続状態を取得するプロパティ
    /// </summary>
    public bool IsConnectedAndReady => PhotonNetwork.IsConnectedAndReady;
    
    /// <summary>
    /// UI操作が可能かどうか（接続済みかつ処理中でない）
    /// </summary>
    public bool CanPerformNetworkActions => IsConnectedAndReady && !isCreatingRoom && !isJoiningRoom;

    //自身の名前
    private string playerName = "未登録";

    //[デバッグ用]ホストかどうか
    [SerializeField]
    private TextMeshProUGUI IsHost;
    //[デバッグ用]自分のプレイヤーナンバーはいくつ？
    [SerializeField]
    private TextMeshProUGUI myNumber;

    void Awake() {
        // シングルトン設定
        if (instance == null) {
            instance = this;
            
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲーム開始時の初期化処理
    /// Photonサーバーへの接続を開始する
    /// </summary>
    void Start() {
        if (autoConnectOnStart) {
            ConnectingServer();
        }
    }

    /// <summary>
    /// 接続状態表示を更新する
    /// </summary>
    /// <param name="status">表示するステータス文字列</param>
    private void UpdateConnectionStatus(string status) {
        if (connectionStatusText != null) {
            connectionStatusText.text = status;
        }
        Debug.Log($"接続状態: {status}");
        OnConnectionStatusChanged?.Invoke();
    }

    /// <summary>
    /// Photonのマスターサーバーに接続完了時に呼ばれる
    /// この時点でルーム作成・参加が可能になる
    /// </summary>
    public override void OnConnectedToMaster() {
        
        UpdateConnectionStatus("サーバーに接続完了！ルーム操作が可能です");
        
        
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

        //画面上の表示をルーム番号の表示にする
        UpdateConnectionStatus("ルーム番号:"+currentRoomCode+"\nあなたはホストです");
        //名づける
        MakeName();
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

        //名づける
        MakeName();

        //画面上の表示をルーム番号の表示にする
        UpdateConnectionStatus("ルーム番号:" + currentRoomCode + "\nあなたはクライアントです");
    }

    /// <summary>
    /// ルーム作成が成功した時に呼ばれる
    /// UIにルームコードを表示するためのイベントを発火
    /// </summary>
    public override void OnCreatedRoom() {
        Debug.Log($"ルーム作成成功: {currentRoomCode}");
        OnRoomCodeGenerated?.Invoke(currentRoomCode); // UIにルームコードを通知

        //ルームコードを表示させる
        if(TitleManager.instance != null) {
            TitleManager.instance.SetRoomCode();
        }
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
            UpdateConnectionStatus("部屋が既に存在します。新しいコードで再試行中...");
            CreateRandomRoom(); // 自動的に新しいコードで再試行
        } else {
            UpdateConnectionStatus($"ルーム作成失敗: {message}");
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

        //UIを更新
        if(PhotonNetwork.IsMasterClient) {
            IsHost.text = "ホストです";
        }
        else {
            IsHost.text = "ホストではないです";
        }
        //UIを更新
        myNumber.text = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        //ルームコードを表示させる
        if (TitleManager.instance != null) {
            TitleManager.instance.SetRoomCode();
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
        
        // TEST2部屋が存在しない場合は自動で作成
        if (returnCode == ErrorCode.GameDoesNotExist && currentRoomCode == "TEST2") {
            Debug.Log("TEST2部屋が存在しないため、自動で作成します");
            UpdateConnectionStatus("TEST2部屋が存在しないため作成中...");
            CreateTestRoom();
            return;
        }
        
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
    /// インスペクターで設定したデバッグ用ルームコードでルームに参加
    /// テスト用途で使用
    /// </summary>
    [ContextMenu("デバッグ用ルーム参加")]
    public void JoinDebugRoom() {
        if (!string.IsNullOrEmpty(debugRoomCode)) {
            Debug.Log($"デバッグ用ルーム参加: {debugRoomCode}");
            JoinRoomWithCode(debugRoomCode);
        } else {
            Debug.LogWarning("デバッグ用ルームコードが設定されていません");
        }
    }

    /// <summary>
    /// 接続状態をチェックして、UI操作可能かどうかを返す
    /// ボタンの有効/無効切り替えに使用
    /// </summary>
    /// <returns>UI操作可能な場合true</returns>
    public bool CanUseNetworkUI() {
        return CanPerformNetworkActions;
    }

    /// <summary>
    /// テスト用の固定部屋を作成
    /// デバッグやテスト時に使用
    /// </summary>
    [ContextMenu("テスト用部屋作成")]
    public void CreateTestRoom() {
        if (!PhotonNetwork.IsConnectedAndReady) {
            Debug.LogWarning("Photonに接続されていません");
            return;
        }

        string testRoomCode = string.IsNullOrEmpty(debugRoomCode) ? "TEST2" : debugRoomCode;
        currentRoomCode = testRoomCode;
        isCreatingRoom = true;
        
        RoomOptions roomOptions = new RoomOptions {
            MaxPlayers = GameConst.PLAYER_MAX,
            IsVisible = true,
            IsOpen = true
        };
        
        Debug.Log($"テスト用ルームを作成中: {testRoomCode}");
        UpdateConnectionStatus($"テスト用ルーム '{testRoomCode}' を作成中...");
        PhotonNetwork.CreateRoom(testRoomCode, roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// 現在存在するルーム一覧を表示（デバッグ用）
    /// </summary>
    [ContextMenu("ルーム一覧を表示")]
    public void ShowRoomList() {
        if (PhotonNetwork.IsConnectedAndReady) {
            Debug.Log("=== 現在のルーム一覧 ===");
            Debug.Log($"実行環境: {(Application.isEditor ? "Unity エディター" : "ビルド版")}");
            
            if (PhotonNetwork.CurrentRoom != null) {
                Debug.Log($"現在のルーム: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}人)");
            } else {
                Debug.Log("現在ルームに参加していません");
            }
            
            // ロビー情報も表示
            Debug.Log($"ロビー: {PhotonNetwork.CurrentLobby?.Name ?? "なし"}");
            Debug.Log($"接続状態: {PhotonNetwork.NetworkClientState}");
        } else {
            Debug.Log("Photonに接続されていません");
        }
    }

    /// <summary>
    /// 実行環境を表示（エディター or ビルド）
    /// </summary>
    [ContextMenu("実行環境を確認")]
    public void CheckEnvironment() {
        string environment = Application.isEditor ? "Unity エディター" : "ビルド版";
        string role = "";
        
        if (Application.isEditor && editorAutoHost) {
            role = " (自動ホスト)";
        } else if (!Application.isEditor) {
            role = " (ゲスト推奨)";
        }
        
        Debug.Log($"実行環境: {environment}{role}");
        UpdateConnectionStatus($"実行環境: {environment}{role}");
    }

    /// <summary>
    /// 強制的にTEST2部屋を作成（既存の場合は参加）
    /// </summary>
    [ContextMenu("🚀 強制TEST2部屋作成")]
    public void ForceCreateOrJoinTEST2() {
        Debug.Log($"🔍 現在のPhoton状態: {PhotonNetwork.NetworkClientState}");
        
        // ルーム内にいる場合は完全リセット
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined) {
            Debug.Log("🔄 ルーム内にいるため、完全リセットします");
            ResetPhotonConnection();
            return;
        }
        
        if (!PhotonNetwork.IsConnectedAndReady) {
            Debug.LogWarning("Photonに接続されていません");
            UpdateConnectionStatus("Photonに接続中...");
            return;
        }

        Debug.Log("🚀 TEST2部屋を強制作成/参加します");
        UpdateConnectionStatus("TEST2部屋を作成/参加中...");
        
        // まず参加を試す
        currentRoomCode = "TEST2";
        isJoiningRoom = true;
        PhotonNetwork.JoinRoom("TEST2");
    }

    /// <summary>
    /// Photon接続を完全にリセット
    /// </summary>
    [ContextMenu("🔄 Photon完全リセット")]
    public void ResetPhotonConnection() {
        Debug.Log("🔄 Photon接続を完全にリセットします");
        UpdateConnectionStatus("Photon接続をリセット中...");
        
        // フラグをリセット
        isCreatingRoom = false;
        isJoiningRoom = false;
        currentRoomCode = "";
        
        // Photonから完全に切断
        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.Disconnect();
        } else {
            // 既に切断されている場合は直接再接続
            OnDisconnected(Photon.Realtime.DisconnectCause.DisconnectByClientLogic);
        }
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

    /// <summary>
    /// Photonから切断された時に呼ばれる
    /// </summary>
    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause) {
        Debug.Log($"Photonから切断されました: {cause}");
        UpdateConnectionStatus("Photonから切断されました");
        
        
    }

    /// <summary>
    /// インプットフィールドを取得したものから名前を付ける
    /// </summary>
    private void MakeName() {
        //自身の名前をインプットフィールドから取得
        if(ButtonManager.instance != null) {
            playerName = ButtonManager.instance.GetNameInput();
            PhotonNetwork.LocalPlayer.NickName = playerName;
        }

        //名前がない時は名無しにする
        if (playerName == "") {
            playerName = "名無しトリ";
        }
    }

    /// <summary>
    /// 名前
    /// </summary>
    /// <returns></returns>
    public string GetName() {
        return playerName;
    }

    /// <summary>
    /// ゲームを落とす時に部屋から抜ける
    /// </summary>
    private void OnApplicationQuit() {
        // アプリケーションが終了するとき（ビルド実行時）
        // 接続状態に応じて適切な処理を行う
        if (PhotonNetwork.IsConnected) {
            if (PhotonNetwork.InRoom) {
                // 部屋にいる場合は部屋を抜ける
                PhotonNetwork.LeaveRoom();
            }
            else {
                // ロビーやマスターサーバーにいる場合は切断
                PhotonNetwork.Disconnect();
            }
        }
    }

    /// <summary>
    /// Photonの鯖から完全に退出
    /// </summary>
    public void DisconnectingServer() {
        PhotonNetwork.Disconnect();
    }

    /// <summary>
    /// サーバーに接続
    /// </summary>
    public void ConnectingServer() {
        UpdateConnectionStatus("Photonに接続中...");
        PhotonNetwork.ConnectUsingSettings(); // Photon設定を使用して接続開始
        
    }
}
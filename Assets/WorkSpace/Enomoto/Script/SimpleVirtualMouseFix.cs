/**
 * @file SimpleVirtualMouseFix.cs
 * @brief VirtualMouseManagerの簡単な修正版
 * 各プレイヤーが自分のカーソルのみ操作可能
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

/// <summary>
/// VirtualMouseManagerの問題を簡単に修正するスクリプト
/// 既存のVirtualMouseManagerと一緒に使用
/// </summary>
public class SimpleVirtualMouseFix : MonoBehaviourPunCallbacks
{
    [Header("設定")]
    [SerializeField] private bool enableFix = true; // 修正を有効にするか
    
    private VirtualMouseManager originalManager;
    private bool isMyTurn = false; // 自分の操作ターンかどうか

    void Start()
    {
        originalManager = VirtualMouseManager.instance;
        
        if (enableFix && PhotonNetwork.IsConnected)
        {
            // オンライン時のみ修正を適用
            SetupOnlineMode();
        }
    }

    /// <summary>
    /// オンラインモードの設定
    /// </summary>
    private void SetupOnlineMode()
    {
        // 最初に入った人（ActorNumber=1）のみ操作可能
        isMyTurn = PhotonNetwork.LocalPlayer.ActorNumber == 1;
        
        Debug.Log($"🎮 プレイヤー{PhotonNetwork.LocalPlayer.ActorNumber}: 操作可能={isMyTurn}");
        
        if (!isMyTurn)
        {
            // 自分の番でない場合は入力を無効化
            DisableMyInput();
        }
    }

    /// <summary>
    /// 自分の入力を無効化
    /// </summary>
    private void DisableMyInput()
    {
        var playerInputManager = FindObjectOfType<PlayerInputManager>();
        if (playerInputManager != null)
        {
            // 新しいプレイヤーの参加を停止
            playerInputManager.enabled = false;
            Debug.Log("⚠️ 入力を無効化しました（他のプレイヤーが操作中）");
        }
    }

    /// <summary>
    /// 操作権を次のプレイヤーに渡す
    /// </summary>
    [ContextMenu("操作権を渡す")]
    public void PassControl()
    {
        if (PhotonNetwork.IsConnected && isMyTurn)
        {
            photonView.RPC("ChangeActivePlayer", RpcTarget.All, GetNextPlayerID());
        }
    }

    /// <summary>
    /// アクティブプレイヤーを変更
    /// </summary>
    [PunRPC]
    private void ChangeActivePlayer(int newActivePlayerID)
    {
        bool wasMyTurn = isMyTurn;
        isMyTurn = PhotonNetwork.LocalPlayer.ActorNumber == newActivePlayerID;
        
        Debug.Log($"🔄 操作権変更: プレイヤー{newActivePlayerID} → 自分の番={isMyTurn}");
        
        var playerInputManager = FindObjectOfType<PlayerInputManager>();
        if (playerInputManager != null)
        {
            playerInputManager.enabled = isMyTurn;
        }
        
        // UI表示更新
        UpdateUI();
    }

    /// <summary>
    /// 次のプレイヤーIDを取得
    /// </summary>
    private int GetNextPlayerID()
    {
        var players = PhotonNetwork.PlayerList;
        int currentIndex = -1;
        
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                currentIndex = i;
                break;
            }
        }
        
        int nextIndex = (currentIndex + 1) % players.Length;
        return players[nextIndex].ActorNumber;
    }

    /// <summary>
    /// UI表示を更新
    /// </summary>
    private void UpdateUI()
    {
        // ここでUI表示を更新（例：「あなたの番です」など）
        if (isMyTurn)
        {
            Debug.Log("✅ あなたの操作番です");
        }
        else
        {
            Debug.Log("⏳ 他のプレイヤーの操作中...");
        }
    }

    /// <summary>
    /// プレイヤー参加時の処理
    /// </summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"👥 プレイヤー{newPlayer.ActorNumber}が参加しました");
        
        // 新しいプレイヤーに現在の状態を同期
        if (PhotonNetwork.IsMasterClient)
        {
            int activePlayerID = GetCurrentActivePlayerID();
            photonView.RPC("ChangeActivePlayer", newPlayer, activePlayerID);
        }
    }

    /// <summary>
    /// 現在のアクティブプレイヤーIDを取得
    /// </summary>
    private int GetCurrentActivePlayerID()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            // 簡単のため、最初のプレイヤーをアクティブとする
            return PhotonNetwork.PlayerList[0].ActorNumber;
        }
        return 1;
    }

    void OnGUI()
    {
        if (!enableFix || !PhotonNetwork.IsConnected) return;
        
        // デバッグ用UI
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"プレイヤー: {PhotonNetwork.LocalPlayer.ActorNumber}");
        GUILayout.Label($"操作可能: {(isMyTurn ? "はい" : "いいえ")}");
        
        if (isMyTurn && GUILayout.Button("操作権を渡す"))
        {
            PassControl();
        }
        GUILayout.EndArea();
    }
}
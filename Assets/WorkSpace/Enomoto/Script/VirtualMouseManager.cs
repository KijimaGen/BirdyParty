/**
 * @file VirtualMouseManager.cs
 * @brief オンライン対応バーチャルマウス管理クラス
 * コントローラーでマウスカーソルを操作し、複数プレイヤーに対応
 * @author Enomoto
 * @date 2025/10/20
 */

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using Photon.Realtime;

/// <summary>
/// オンライン対応バーチャルマウス管理クラス
/// 各プレイヤーが自分のカーソルのみ操作可能
/// </summary>
public class VirtualMouseManager : MonoBehaviourPunCallbacks
{
    [Header("UI設定")]
    [SerializeField] private RectTransform _root; // カーソルの親となるCanvas
    [SerializeField] private VirtualMouseInput[] _cursorPrefabs; // プレイヤーごとのカーソルプレハブ
    
    [Header("入力設定")]
    [SerializeField] private string _moveActionName = "Move"; // 移動アクション名
    [SerializeField] private string _leftButtonActionName = "LeftButton"; // クリックアクション名

    // カーソル管理
    private readonly List<VirtualMouseInput> _cursors = new(); // 全カーソルのリスト
    private readonly Dictionary<int, VirtualMouseInput> _playerCursors = new(); // プレイヤーID → カーソル
    private VirtualMouseInput _myCursor; // 自分のカーソル
    private int _myPlayerID; // 自分のプレイヤーID

    // インスタンス
    public static VirtualMouseManager instance;

    /// <summary>
    /// インスタンスを即座に作成
    /// </summary>
    private void Awake() {
        instance = this;
        
        // オンライン時は自分のプレイヤーIDを取得
        if (PhotonNetwork.IsConnected) {
            _myPlayerID = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log($"🎮 自分のプレイヤーID: {_myPlayerID}");
        } else {
            _myPlayerID = 1; // オフライン時はデフォルト
        }
    }

    private void LateUpdate()
    {
        foreach (var cursor in _cursors)
        {
            if (cursor == null) continue;
            RectTransform cursorRect = cursor.GetComponent<RectTransform>();
            if (cursorRect == null) continue;

            Vector2 canvasSize = _root.rect.size;
            Vector2 cursorSize = cursorRect.rect.size;
            float minX = cursorSize.x * cursorRect.pivot.x;
            float maxX = canvasSize.x - cursorSize.x * (1f - cursorRect.pivot.x);
            float minY = cursorSize.y * cursorRect.pivot.y;
            float maxY = canvasSize.y - cursorSize.y * (1f - cursorRect.pivot.y);

            // UI座標のClamp（画面内に制限）
            Vector2 pos = cursorRect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            cursorRect.anchoredPosition = pos;

            // ---- ここが重要 ----
            // 仮想マウスの座標をUI位置と同期させる
            if (cursor.virtualMouse != null)
            {
                // 自分のものでなければスキップ（重要な修正！）
                if(!cursor.GetComponent<PhotonView>().IsMine) continue;

                // 仮想マウスのスクリーン座標をUI座標に合わせて再設定
                var virtualMouse = cursor.virtualMouse;
                var mousePos = virtualMouse.position.ReadValue();

                // UI座標をスクリーン座標に変換
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, cursorRect.position);

                // Clamp後の位置に強制セット
                InputState.Change(virtualMouse.position, screenPos);
            }
        }
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        print($"プレイヤー#{playerInput.playerIndex + 1}が参加しました。");

        int playerIndex = playerInput.playerIndex;
        if (playerIndex < 0 || playerIndex >= _cursorPrefabs.Length)
        {
            //Debug.LogError("参加できるプレイヤー数を超えています");
            return;
        }

        var cursor = Instantiate(_cursorPrefabs[playerIndex], _root);
        cursor.name = $"Cursor#{playerIndex}";
        _cursors.Add(cursor);

        // VirtualMouseInput にアクション設定を紐付け
        var actions = playerInput.actions;
        var moveAction = actions.FindAction(_moveActionName);
        var leftButtonAction = actions.FindAction(_leftButtonActionName);

        if (moveAction != null)
            cursor.stickAction = new InputActionProperty(moveAction);
        if (leftButtonAction != null)
            cursor.leftButtonAction = new InputActionProperty(leftButtonAction);
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        print($"プレイヤー#{playerInput.playerIndex + 1}が退出しました。");

        int playerIndex = playerInput.playerIndex;
        var cursor = _cursors.Find(c => c != null && c.name == $"Cursor#{playerIndex}");
        if (cursor == null) return;

        _cursors.Remove(cursor);
        Destroy(cursor.gameObject);
    }
}
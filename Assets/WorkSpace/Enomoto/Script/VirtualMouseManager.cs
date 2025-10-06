using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class VirtualMouseManager : MonoBehaviour
{
    // カーソルの親オブジェクト
    [SerializeField] private RectTransform _root;

    // プレイヤーのカーソルPrefab一覧（複数対応）
    [SerializeField] private VirtualMouseInput[] _cursorPrefabs;

    // 移動のAction名
    [SerializeField] private string _moveActionName = "Move";

    // 左クリックAction名
    [SerializeField] private string _leftButtonActionName = "LeftButton";

    // 生成されたカーソル一覧
    private readonly List<VirtualMouseInput> _cursors = new();

    // プレイヤーの参加時に呼びだし
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        print($"プレイヤー#{playerInput.playerIndex}が参加しました。");

        // インデックスチェック
        var playerIndex = playerInput.playerIndex;
        if (playerIndex < 0 || playerIndex >= _cursorPrefabs.Length)
        {
            Debug.LogError("参加できるプレイヤー数を超えています");
            return;
        }

        // カーソル生成
        var cursor = Instantiate(_cursorPrefabs[playerIndex], _root);
        cursor.name = $"Cursor#{playerIndex}";

        // カーソルをリストに追加（管理リスト）
        _cursors.Add(cursor);

        // InputActionを取得
        var actions = playerInput.actions;

        var moveAction = actions.FindAction(_moveActionName);
        var leftButtonAction = actions.FindAction(_leftButtonActionName);

        // ActionPropatyの設定
        if (moveAction != null)
        {
            cursor.stickAction = new InputActionProperty(moveAction);
        }

        if (leftButtonAction != null)
        {
            cursor.leftButtonAction = new InputActionProperty(leftButtonAction);
        }
    }

    // プレイヤーの離脱時に呼びだし
    public void OnPlayerLeft(PlayerInput playerInput)
    {
        print($"プレイヤー#{playerInput.playerIndex}が離脱しました。");

        // カーソルをリストから削除（管理リスト）
        var playerIndex = playerInput.playerIndex;

        // 生成されたカーソルを取得
        var cursor = _cursors.Find(c => c != null && c.name == $"Cursor#{playerIndex}");
        if (cursor == null) return;

        // 取得したカーソルを削除
        _cursors.Remove(cursor);
        Destroy(cursor.gameObject);
    }
}

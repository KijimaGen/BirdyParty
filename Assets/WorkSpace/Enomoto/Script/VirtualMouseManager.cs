using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

public class VirtualMouseManager : MonoBehaviour
{
    [SerializeField] private RectTransform _root; 
    [SerializeField] private VirtualMouseInput[] _cursorPrefabs;
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _leftButtonActionName = "LeftButton";

    private readonly List<VirtualMouseInput> _cursors = new();

    // カーソル移動速度
    [SerializeField] private float cursorSpeed = 1000f;

    private void LateUpdate()
    {
        foreach (var cursor in _cursors)
        {
            if (cursor == null) continue;

            RectTransform cursorRect = cursor.GetComponent<RectTransform>();
            if (cursorRect == null) continue;

            Vector2 pos = cursorRect.anchoredPosition;
            Vector2 canvasSize = _root.rect.size;
            Vector2 cursorSize = cursorRect.rect.size;

            float minX = cursorSize.x * cursorRect.pivot.x;
            float maxX = canvasSize.x - cursorSize.x * (1f - cursorRect.pivot.x);
            float minY = cursorSize.y * cursorRect.pivot.y;
            float maxY = canvasSize.y - cursorSize.y * (1f - cursorRect.pivot.y);

            float margin = 0f;

            pos.x = Mathf.Clamp(pos.x, minX + margin, maxX - margin);
            pos.y = Mathf.Clamp(pos.y, minY + margin, maxY - margin);

            cursorRect.anchoredPosition = pos;
        }
    }


    public void OnPlayerJoined(PlayerInput playerInput)
    {
        print($"プレイヤー#{playerInput.playerIndex + 1}が参加しました。");

        int playerIndex = playerInput.playerIndex;
        if (playerIndex < 0 || playerIndex >= _cursorPrefabs.Length)
        {
            Debug.LogError("参加できるプレイヤー数を超えています");
            return;
        }

        var cursor = Instantiate(_cursorPrefabs[playerIndex], _root);
        cursor.name = $"Cursor#{playerIndex}";
        _cursors.Add(cursor);

        // VirtualMouseInput にアクションを紐づけ
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
        print($"プレイヤー#{playerInput.playerIndex + 1}が離脱しました。");

        int playerIndex = playerInput.playerIndex;
        var cursor = _cursors.Find(c => c != null && c.name == $"Cursor#{playerIndex}");
        if (cursor == null) return;

        _cursors.Remove(cursor);
        Destroy(cursor.gameObject);
    }
}
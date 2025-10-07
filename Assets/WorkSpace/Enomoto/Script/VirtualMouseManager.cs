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

            // UI��̍��WClamp
            Vector2 pos = cursorRect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            cursorRect.anchoredPosition = pos;

            // ---- �������d�v ----
            // ���z�}�E�X�̍��W��UI�̈ʒu�Ɠ���������
            if (cursor.virtualMouse != null)
            {
                // ���z�}�E�X�̃X�N���[�����W��UI���W�ɍ��킹�čĐݒ�
                var virtualMouse = cursor.virtualMouse;
                var mousePos = virtualMouse.position.ReadValue();

                // UI���W���X�N���[���ɕϊ�
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, cursorRect.position);

                // Clamp��̈ʒu�ɋ����Z�b�g
                InputState.Change(virtualMouse.position, screenPos);
            }
        }
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        print($"�v���C���[#{playerInput.playerIndex + 1}���Q�����܂����B");

        int playerIndex = playerInput.playerIndex;
        if (playerIndex < 0 || playerIndex >= _cursorPrefabs.Length)
        {
            Debug.LogError("�Q���ł���v���C���[���𒴂��Ă��܂�");
            return;
        }

        var cursor = Instantiate(_cursorPrefabs[playerIndex], _root);
        cursor.name = $"Cursor#{playerIndex}";
        _cursors.Add(cursor);

        // VirtualMouseInput �ɃA�N�V������R�Â�
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
        print($"�v���C���[#{playerInput.playerIndex + 1}�����E���܂����B");

        int playerIndex = playerInput.playerIndex;
        var cursor = _cursors.Find(c => c != null && c.name == $"Cursor#{playerIndex}");
        if (cursor == null) return;

        _cursors.Remove(cursor);
        Destroy(cursor.gameObject);
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class VirtualMouseManager : MonoBehaviour
{
    // �J�[�\���̐e�I�u�W�F�N�g
    [SerializeField] private RectTransform _root;

    // �v���C���[�̃J�[�\��Prefab�ꗗ�i�����Ή��j
    [SerializeField] private VirtualMouseInput[] _cursorPrefabs;

    // �ړ���Action��
    [SerializeField] private string _moveActionName = "Move";

    // ���N���b�NAction��
    [SerializeField] private string _leftButtonActionName = "LeftButton";

    // �������ꂽ�J�[�\���ꗗ
    private readonly List<VirtualMouseInput> _cursors = new();

    // �v���C���[�̎Q�����ɌĂт���
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        print($"�v���C���[#{playerInput.playerIndex}���Q�����܂����B");

        // �C���f�b�N�X�`�F�b�N
        var playerIndex = playerInput.playerIndex;
        if (playerIndex < 0 || playerIndex >= _cursorPrefabs.Length)
        {
            Debug.LogError("�Q���ł���v���C���[���𒴂��Ă��܂�");
            return;
        }

        // �J�[�\������
        var cursor = Instantiate(_cursorPrefabs[playerIndex], _root);
        cursor.name = $"Cursor#{playerIndex}";

        // �J�[�\�������X�g�ɒǉ��i�Ǘ����X�g�j
        _cursors.Add(cursor);

        // InputAction���擾
        var actions = playerInput.actions;

        var moveAction = actions.FindAction(_moveActionName);
        var leftButtonAction = actions.FindAction(_leftButtonActionName);

        // ActionPropaty�̐ݒ�
        if (moveAction != null)
        {
            cursor.stickAction = new InputActionProperty(moveAction);
        }

        if (leftButtonAction != null)
        {
            cursor.leftButtonAction = new InputActionProperty(leftButtonAction);
        }
    }

    // �v���C���[�̗��E���ɌĂт���
    public void OnPlayerLeft(PlayerInput playerInput)
    {
        print($"�v���C���[#{playerInput.playerIndex}�����E���܂����B");

        // �J�[�\�������X�g����폜�i�Ǘ����X�g�j
        var playerIndex = playerInput.playerIndex;

        // �������ꂽ�J�[�\�����擾
        var cursor = _cursors.Find(c => c != null && c.name == $"Cursor#{playerIndex}");
        if (cursor == null) return;

        // �擾�����J�[�\�����폜
        _cursors.Remove(cursor);
        Destroy(cursor.gameObject);
    }
}

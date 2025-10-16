/**
 * @file GameConst.cs
 * @brief �萔��`
 * @author Sum1r3
 * @date 2025/10/6
 */

using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameConst {
    // �v���C���[�̍ő吔
    public static readonly int PLAYER_MAX = 4;

    //�v���C���[�̃X�L���̎��
    public enum SkinVariation {
        None,   //�X�L����
        Straw   //���m�̃g���B
    }

    #region �~�j�Q�[���̃V�[�����ꗗ
    public static readonly string RACEGAME_SCENE_NAME = "Race"; 
    public static readonly string DROPGAME_SCENE_NAME = "DropBird";
    #endregion

    #region �~�j�Q�[���̃A�N�V�����}�b�v���ꗗ
    public static readonly string DROPGAME_ACTION_NAME = "DropGame";
    #endregion

}

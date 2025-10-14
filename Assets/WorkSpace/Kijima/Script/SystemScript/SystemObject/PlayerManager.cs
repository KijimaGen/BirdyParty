/**
 * @file PlayerManager.cs
 * @brief �v���C���[�̏�������Ă�������(���˂Ȋ�])
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameConst;

public class PlayerManager : SystemObject {
    //���g�̃C���X�^���X
    public static PlayerManager instance;
    //�v���C���[����
    List<PlayerInfomation> playerList = new List<PlayerInfomation>(PLAYER_MAX);

    public override async UniTask Initialize() {
        instance = this;
        //�v���C���[���X�g��PLAYER_MAX��null���l�ߍ���
        playerList = Enumerable.Repeat<PlayerInfomation>(null, PLAYER_MAX).ToList();


        await UniTask.CompletedTask;
    }

    /// <summary>
    /// �v���C���[��ǉ�
    /// </summary>
    /// <param Name="player"></param>
    public void AddPlayer(PlayerInfomation player) {
        //�ꉞ�ő吔�𒴂�������������K��
        if (playerList.Count + 1 > PLAYER_MAX) return;
        //�v���C���[���X�g�ɒǉ�
        playerList.Add(player);
    }

    public List<PlayerInfomation> GetPlayerList(){
        return playerList;

    }
}

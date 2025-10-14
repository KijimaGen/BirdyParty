/**
 * @file PlayerManager.cs
 * @brief �v���C���[�̏�������Ă�������(���˂Ȋ�])
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameConst;

public class PlayerManager : SystemObject {
    //���g�̃C���X�^���X
    public static PlayerManager instance;
    //�v���C���[����
    [SerializeField]
    private List<PlayerInfomation> playerList = new List<PlayerInfomation>(PLAYER_MAX);

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
        //�v���C���[���X�g�ɒǉ�
        for(int i = 0; i < playerList.Count; i++) {
            if (playerList[i] == null) {
                playerList[i] = player;
                return;
            }
        }
    }

    /// <summary>
    /// �v���C���[���X�g�������n��
    /// </summary>
    /// <returns></returns>
    public List<PlayerInfomation> GetPlayerList(){
        return playerList;
    }
}

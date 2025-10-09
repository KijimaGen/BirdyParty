/**
 * @file ObstacleManager.cs
 * @brief �F�����R���݂ɂ������
 * @author Sum1r3
 * @date 2025/10/7
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour{
    //��Q���Ɖ����A�C�e��
    [SerializeField]
    List<GameObject> itemList;

    //�x�N�^�[3�̉�]
    private readonly Vector3 spawnRot = new Vector3(-90, 0, -90);

    //��Q����u���ɓ�����A�K�v�ȃ����_���̏�����C��
    private const float MAX_Z = 10;
    private const float MIN_Z = -20;
    //�X�^�[�g���C��(�����ƃ}�[�W���͎���Ă܂�)
    private const float MAX_X = 315;
    private const float MIN_X = -60;
    //�A�C�e���̐��̃}�b�N�X
    private const int ITEM_MAX = 100;
    private const int ITEM_MIN = 50;

    /// <summary>
    /// �ŏ��ɍs������
    /// </summary>
    void Start(){
        _ = InstantiateItem();
    }

    private async UniTask InstantiateItem() {
        //�A�C�e���̐��������_���Ɍ���
        int itemCount = Random.Range(ITEM_MIN, ITEM_MAX);
        for(int i = 0,max = itemCount; i < max; i++) {

            //�A�C�e���̏o�����W�������_���ɐ���
            Vector3 itemSpawnPos = Vector3.zero;
            itemSpawnPos.x = Random.Range(MIN_X, MAX_X);
            itemSpawnPos.z = Random.Range(MIN_Z, MAX_Z);
            itemSpawnPos.y = 0.05f;

            //���ۂɃA�C�e���𐶐�
            int rand = Random.Range(0, itemList.Count);
            transform.SpawnChildWorld(itemList[rand], itemSpawnPos, spawnRot);
            
            
        }

        await UniTask.CompletedTask;
    }
}

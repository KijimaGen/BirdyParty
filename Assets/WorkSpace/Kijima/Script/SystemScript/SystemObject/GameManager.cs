/**
 * @file GameManager.cs
 * @brief �Q�[���S�̂ŊǗ����������̊Ǘ���
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SystemObject{
    //�I�����C�����ǂ���
    private bool isOnline;
    //


    public override async UniTask Initialize() {
        isOnline = false;


        await UniTask.CompletedTask;
    }

    /// <summary>
    /// �I�����C�����ǂ���
    /// </summary>
    /// <returns></returns>
    public bool IsOnline() {
        return isOnline;
    }

    /// <summary>
    /// �I�����C�����ǂ������Z�b�g
    /// </summary>
    public void SetIsOnline(bool t) {
        isOnline = t;
    }

}

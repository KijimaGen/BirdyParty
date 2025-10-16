/**
 * @file DashBoad.cs
 * @brief ���[�X�Q�[���̃_�b�V���{�[�h(�����A�C�e��)
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashBoard: MonoBehaviour{
    /// <summary>
    /// ������
    /// </summary>
    /// <param Name="other"></param>
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Player") {
            other.gameObject.GetComponent<RacePlayer>().Boost();
        }
    }
}

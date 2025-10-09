/**
 * @file Hurdle.cs
 * @brief ���[�X�Q�[���̃n�[�h��(��Q��)
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hurdle : MonoBehaviour{
    /// <summary>
    /// ������
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Player") {
            other.gameObject.GetComponent<RacePlayer>().Slow();
        }
    }
}

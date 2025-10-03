/**
 * @file RacePlayer.cs
 * @brief ���[�X�Q�[���̃}�l�[�W���[
 * @author Sum1r3
 * @date 2025/10/03
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RaceManager : SystemObject{
    //�Q�[���J�n���̃J�E���g�_�E��
    private const float GameStartCount = 3.0f;
    //����
    private List<GameObject> Ranking;
    //�X�^�[�g������
    public bool isStart;
    //���g�̃C���X�^���X
    public static RaceManager instance;
    void Start(){
        
    }

    void Update(){
        
    }

    /// <summary>
    /// �Q�[���J�n���̃J�E���g�_�E���������ōs��
    /// </summary>
    private async UniTask StartCountDown() {
        float time = GameStartCount;
        _ = AudioManager.instance.PlaySE(2);
        while(time > 0) {
            time -= Time.deltaTime;
        }
        //�X�^�[�g
        isStart = true;
        await UniTask.CompletedTask;
    }

    public override UniTask Initialize() {
        instance = this;


        isStart = false;
        _ = StartCountDown();
        return UniTask.CompletedTask;
    }
}

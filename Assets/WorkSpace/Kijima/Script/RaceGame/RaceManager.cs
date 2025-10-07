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
    private List<GameObject> Ranking = new List<GameObject>();
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
        //�v���C���[�������܂ő҂�
        Camera camera = Camera.main;
        
        while (camera.gameObject.GetComponent<RaceCameraController>().GetRacer() < 2) {


            await UniTask.DelayFrame(100);
        }


        _ = AudioManager.instance.PlaySE(2);
        await UniTask.Delay(3000);
        //�X�^�[�g
        isStart = true;
        await UniTask.CompletedTask;
    }

    public override async UniTask Initialize() {
        instance = this;

        await FadeManager.instance.FadeIn();

        isStart = false;
        await StartCountDown();
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="player"></param>
    public void AddRanking(GameObject player) {
        Ranking.Add(player);
    }

    public int GetRankingCount(GameObject player) {
        return Ranking.IndexOf(player);
    }
}

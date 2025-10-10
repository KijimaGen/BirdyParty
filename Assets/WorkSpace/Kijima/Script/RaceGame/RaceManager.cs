/**
 * @file RacePlayer.cs
 * @brief ���[�X�Q�[���̃}�l�[�W���[
 * @author Sum1r3
 * @date 2025/10/03
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class RaceManager : SystemObject{

    //����
    private List<GameObject> Ranking = new List<GameObject>();
    //�X�^�[�g������
    public bool isStart;
    //���g�̃C���X�^���X
    public static RaceManager instance;

    //�e�v���C���[������Ă���
    private List<RacePlayer> racers = new List<RacePlayer>();

    //�e�v���C���[�̊J�n�ʒu
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-65f, 1.2f, 1f),
        new Vector3 (-65, 1.2f, -3.2f),
        new Vector3(-65, 1.2f, -7),
        new Vector3 (-65, 1.2f, -11)
    };

    private readonly Vector3[] RankingPositions = new Vector3[]
    {
        new Vector3 (-3.6f, 6, -96f),
        new Vector3 (-1.6f, 5, -96f),
        new Vector3 (0.4f, 4, -96f),
        new Vector3(2.4f, 3, -96f)
    };

    public bool isGoal { get; private set; } = false;

    private bool isStandby = false; 

    void Start(){
        
    }

    void Update(){
        //�����L���O�ɓ����Ă���l�� == ���[�T�[�̐l�� = �S���S�[�����Ă���
        if(racers.Count == Ranking.Count && isStart) {
            PlayerGoalPosSet();
            isGoal = true;
        }
    }

    /// <summary>
    /// �Q�[���J�n���̃J�E���g�_�E���������ōs��
    /// </summary>
    private async UniTask StartCountDown() {
        //�v���C���[�������܂ő҂�

        while (!isStandby) {
            //�v���C���[���X�^�[�g���C���ɒu���Ă���
            PlayerStartPosSet();
            await UniTask.DelayFrame(100);
        }

        //�v���C���[���X�^�[�g���C���ɒu���Ă���(OneMore)
        PlayerStartPosSet();

        _ = AudioManager.instance.PlaySE(2);
        await UniTask.Delay(3000);
        //�X�^�[�g
        isStart = true;
        await UniTask.CompletedTask;
    }

    public override async UniTask Initialize() {
        instance = this;

        await FadeManager.instance.FadeIn();

        isGoal = false;
        isStart = false;
        isStandby = false;
        await StartCountDown();
        
    }

    /// <summary>
    /// �����L���O�ɒǉ�
    /// </summary>
    /// <param name="player"></param>
    public void AddRanking(GameObject player) {
        Ranking.Add(player);
    }

    /// <summary>
    /// ����̃Q�[���I�u�W�F�N�g�������L���O�̂ǂ��ɂ��邩��Ԃ�
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public int GetRankingCount(GameObject player) {
        return Ranking.IndexOf(player);
    }

    /// <summary>
    /// ���[�T�[�̃��X�g�Ƀv���C���[������
    /// </summary>
    /// <param name="player"></param>
    public void AddRacers(RacePlayer player) {
        racers.Add(player);
    }

    /// <summary>
    /// �v���C���[���ŏ��̈ʒu�ɒ�������
    /// </summary>
    public void PlayerStartPosSet() {
        for (int i = 0, max = racers.Count; i < max; i++) {
            if (racers[i] != null || racers != null)
                racers[i].SetPosition(spawnPositions[i]);
        }
    }

    /// <summary>
    /// ���������Ԗڂɓ����Ă����v���C���[����n��
    /// </summary>
    /// <param name="racePlayer"></param>
    /// <returns></returns>
    public int GetPlayerNumber(RacePlayer racePlayer) {
        return racers.IndexOf(racePlayer);
    }

    /// <summary>
    /// �v���C���[���S�[����̈ʒu�ɒ�������
    /// </summary>
    public void PlayerGoalPosSet() {
        for (int i = 0, max = racers.Count; i < max; i++) {
            if (racers[i] != null || racers != null)
                racers[i].SetPosition(RankingPositions[i]);
        }
    }

    public void StandbyOK() {
        isStandby = true;
    }

}

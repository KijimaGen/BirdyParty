/**
 * @file PlayerInfomation.cs
 * @brief �v���C���[�̊e�X�̏��
 * @author Sum1r3
 * @date 2025/10/14
 */
using Photon.Pun;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConst;

public class PlayerInfomation:MonoBehaviour{
    //�������Ă���|�C���g
    public int point;
    //���݂̏���
    public int rank;
    //�����̖��O
    public string Name;
    //������skin
    public SkinVariation mySkin;
    //�����̔ԍ�
    public int myNumber;

    //���g�̃t�H�g���r���[
    PhotonView photonView;
    //���g�̃��f���������ꏊ
    [SerializeField]
    private Transform myObjectRoot;

    /// <summary>
    /// �X�^�[�g
    /// </summary>
    void Start() {
        //�|�C���g��������
        point = 0;
        //���g�̃t�H�g���r���[�擾
        photonView = GetComponent<PhotonView>();
        //���g�̔ԍ����擾
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        //�v���C���[�Ǘ��N���X�ɓo�^
        PlayerManager.instance.AddPlayer(this);
        // �V�[���ǂݍ��ݎ��̃R�[���o�b�N�o�^
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    //���g��������Ƃ��ɃR�[���o�b�N���~�߂�
    private void OnDisable() {
        // �Y�ꂸ�ɉ���
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //�������I�u�W�F�N�g��j��
        DestroySelectedChildren();
        if (scene.name == RACEGAME_SCENE_NAME) {

        }
    }
    
    /// <summary>
    /// ���g�̎����I�u�W�F�N�g�̒��g��j��
    /// </summary>
    public void DestroySelectedChildren() {
        if (myObjectRoot == null) return;

        // �q�I�u�W�F�N�g�̑������Ԃɔj��
        foreach (Transform grandChild in myObjectRoot) {
            if (grandChild != null)
                Destroy(grandChild.gameObject);
        }
    }

    public void LoadRaceScene() {

    }

    


    #region �e��Q�b�^�[�ƃZ�b�^�[
    // Point
    public int GetPoint() { return point; }
    public void SetPoint(int value) { point = value; }

    // Rank
    public int GetRank() { return rank; }
    public void SetRank(int value) { rank = value; }

    // Name
    public string GetName() { return Name; }
    public void SetName(string value) { Name = value; }

    // Skin
    public SkinVariation GetMySkin() { return mySkin; }
    public void SetMySkin(SkinVariation value) { mySkin = value; }

    // Number
    public int GetMyNumber() { return myNumber; }
    public void SetMyNumber(int value) { myNumber = value; }

    #endregion
}


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceCameraController : MonoBehaviour {
    [Header("�v���C���[�B")]
    public List<Transform> racers = new List<Transform>();

    [Header("�J�����ݒ�")]
    public float smoothTime = 0.3f;
    public float minZoom = 10f;
    public float maxZoom = 30f;
    public Vector3 offset = new Vector3(0, 15, -15);

    private Vector3 velocity;

    private readonly Vector3 isGoalPosition = new Vector3(0,3.5f,-103);
    private readonly Vector3 NormalRotation = new Vector3(45,0, 0);

    void LateUpdate() {
        if (racers == null || racers.Count == 0)
            return;

        if (RaceManager_PUN.instance != null && RaceManager_PUN.instance.isGoal) {
            transform.position = isGoalPosition;
            transform.eulerAngles = Vector3.zero;
            return;
        }

        // ====== 1. �őO�ƍŌ��X����Ŏ擾 ======
        Transform first = racers.OrderByDescending(r => r.position.x).First();
        Transform last = racers.OrderBy(r => r.position.x).First();

        // ====== 2. ���S�_�����߂�iX�������ǂ��j ======
        float centerX = (first.position.x + last.position.x) / 2f;
        Vector3 center = new Vector3(centerX, 0, 0);

        // ====== 3. �����𑪂��ăY�[���␳ ======
        float distance = Mathf.Abs(first.position.x - last.position.x);
        float zoomFactor = Mathf.Clamp01(distance / 30f); // �������₷��
        float zoom = Mathf.Lerp(1f, 2f, zoomFactor); // 1�`2�{�ɃX�P�[�����O

        // ====== 4. �I�t�Z�b�g���X�P�[�����O���Ďg�� ======
        Vector3 scaledOffset = offset * zoom;

        // ====== 5. �J�����ʒu�����߂�iX�����ρj ======
        Vector3 desiredPosition = new Vector3(center.x, 0, 0) + scaledOffset;
        desiredPosition.y = scaledOffset.y; // Y�Œ�
        desiredPosition.z = scaledOffset.z; // Z�Œ�

        // ====== 6. �X���[�Y�ړ� ======
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // ======�V.��]�̌Œ�
        transform.eulerAngles = NormalRotation;


    }

    /// <summary>
    /// �J��������̂��߂Ƀv���C���[�̐�������Ă���
    /// </summary>
    /// <param Name="racerTransform"></param>
    public void AddRacer(Transform racerTransform) {
        if (!racers.Contains(racerTransform))
            racers.Add(racerTransform);
    }

    /// <summary>
    /// �v���C���[�̐���Ԃ�
    /// </summary>
    /// <returns></returns>
    public int GetRacer() {
        return racers.Count;
    }
}

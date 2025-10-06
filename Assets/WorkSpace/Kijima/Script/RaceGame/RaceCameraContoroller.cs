/**
 * @file RaceCameraContoroller.cs
 * @brief ���[�X�Q�[���̃J�����R���g���[���[
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceCameraContoroller : MonoBehaviour{
    [Header("�v���C���[�B")]
    public List<Transform> racers;

    [Header("�J�����ݒ�")]
    public float smoothTime = 0.3f;
    public float minZoom = 10f;
    public float maxZoom = 25f;
    public Vector3 offset = new Vector3(0, 10, -15);

    private Vector3 velocity;

    void LateUpdate() {
        if (racers == null || racers.Count == 0) return;

        // ====== 1�ʂƃr�����擾�iZ�������O��j======
        Transform first = racers.OrderByDescending(r => r.position.z).First();
        Transform last = racers.OrderBy(r => r.position.z).First();

        // ====== 2. ���S�_�����߂� ======
        Vector3 center = (first.position + last.position) / 2f;

        // ====== 3. �����𑪂��ăY�[���␳ ======
        float distance = Vector3.Distance(first.position, last.position);
        float zoom = Mathf.Lerp(minZoom, maxZoom, distance / 40f);

        // ====== 4. �J�����̖ڕW�ʒu ======
        Vector3 desiredPosition = center + offset.normalized * zoom;

        // ====== 5. �X���[�Y�Ɉړ� ======
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // ====== 6. �J�����̌����i�R�[�X�����ɌŒ�j ======
        // �R�[�X��Z�������Ȃ̂ŁAXZ���ʂŌ����𐧌�
        Vector3 lookPoint = center;
        lookPoint.y = center.y + 1f; // �������������
        transform.LookAt(lookPoint);
    }

    /// <summary>
    /// ���[�T�[�̒ǉ�
    /// </summary>
    /// <param name="racerTransform"></param>
    public void AddRacers(Transform racerTransform) {
        if (racers.Count < GameConst.PLAYER_MAX)
            racers.Add(racerTransform);
    }
}

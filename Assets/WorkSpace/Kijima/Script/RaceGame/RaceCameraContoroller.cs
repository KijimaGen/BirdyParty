/**
 * @file RaceCameraContoroller.cs
 * @brief レースゲームのカメラコントローラー
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceCameraContoroller : MonoBehaviour{
    [Header("プレイヤー達")]
    public List<Transform> racers;

    [Header("カメラ設定")]
    public float smoothTime = 0.3f;
    public float minZoom = 10f;
    public float maxZoom = 25f;
    public Vector3 offset = new Vector3(0, 10, -15);

    private Vector3 velocity;

    void LateUpdate() {
        if (racers == null || racers.Count == 0) return;

        // ====== 1位とビリを取得（Z軸方向前提）======
        Transform first = racers.OrderByDescending(r => r.position.z).First();
        Transform last = racers.OrderBy(r => r.position.z).First();

        // ====== 2. 中心点を求める ======
        Vector3 center = (first.position + last.position) / 2f;

        // ====== 3. 距離を測ってズーム補正 ======
        float distance = Vector3.Distance(first.position, last.position);
        float zoom = Mathf.Lerp(minZoom, maxZoom, distance / 40f);

        // ====== 4. カメラの目標位置 ======
        Vector3 desiredPosition = center + offset.normalized * zoom;

        // ====== 5. スムーズに移動 ======
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // ====== 6. カメラの向き（コース方向に固定） ======
        // コースがZ軸方向なので、XZ平面で向きを制御
        Vector3 lookPoint = center;
        lookPoint.y = center.y + 1f; // 少し上を見せる
        transform.LookAt(lookPoint);
    }

    /// <summary>
    /// レーサーの追加
    /// </summary>
    /// <param name="racerTransform"></param>
    public void AddRacers(Transform racerTransform) {
        if (racers.Count < GameConst.PLAYER_MAX)
            racers.Add(racerTransform);
    }
}

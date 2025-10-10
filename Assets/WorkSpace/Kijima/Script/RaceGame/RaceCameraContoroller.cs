using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceCameraController : MonoBehaviour {
    [Header("プレイヤー達")]
    public List<Transform> racers = new List<Transform>();

    [Header("カメラ設定")]
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

        // ====== 1. 最前と最後をX軸基準で取得 ======
        Transform first = racers.OrderByDescending(r => r.position.x).First();
        Transform last = racers.OrderBy(r => r.position.x).First();

        // ====== 2. 中心点を求める（X軸だけ追う） ======
        float centerX = (first.position.x + last.position.x) / 2f;
        Vector3 center = new Vector3(centerX, 0, 0);

        // ====== 3. 距離を測ってズーム補正 ======
        float distance = Mathf.Abs(first.position.x - last.position.x);
        float zoomFactor = Mathf.Clamp01(distance / 30f); // 調整しやすく
        float zoom = Mathf.Lerp(1f, 2f, zoomFactor); // 1～2倍にスケーリング

        // ====== 4. オフセットをスケーリングして使う ======
        Vector3 scaledOffset = offset * zoom;

        // ====== 5. カメラ位置を決める（Xだけ可変） ======
        Vector3 desiredPosition = new Vector3(center.x, 0, 0) + scaledOffset;
        desiredPosition.y = scaledOffset.y; // Y固定
        desiredPosition.z = scaledOffset.z; // Z固定

        // ====== 6. スムーズ移動 ======
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // ======７.回転の固定
        transform.eulerAngles = NormalRotation;


    }

    /// <summary>
    /// カメラ操作のためにプレイヤーの数を取っておく
    /// </summary>
    /// <param name="racerTransform"></param>
    public void AddRacer(Transform racerTransform) {
        if (!racers.Contains(racerTransform))
            racers.Add(racerTransform);
    }

    /// <summary>
    /// プレイヤーの数を返す
    /// </summary>
    /// <returns></returns>
    public int GetRacer() {
        return racers.Count;
    }
}

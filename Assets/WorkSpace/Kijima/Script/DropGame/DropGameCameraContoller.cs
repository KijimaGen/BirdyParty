/**
 * @file DropGameCameraContoller.cs
 * @brief ドロップゲームカメラ
 * @author Sum1r3
 * @date 2025/10/16
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropGameCameraContoller : MonoBehaviour {
    [Header("追従対象プレイヤーたち")]
    public List<Transform> targets = new List<Transform>();

    [Header("カメラ移動速度")]
    public float smoothSpeed = 0.3f;

    [Header("全体が見える距離の調整")]
    public float zoomLimiter = 50f;
    public float minZoom = 40f;
    public float maxZoom = 10f;

    [Header("カメラの高さとオフセット")]
    public Vector3 offset = new Vector3(0, 20f, -20f);

    //終わった後に行く場所
    [SerializeField]
    private readonly Vector3 endPos = new Vector3(-12.5f,3.5f,-18f);

    private Vector3 velocity;
    private Camera cam;

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void LateUpdate() {
        //舞フレームプレイヤーが破壊されてないかを確認
        //途中がnullになっているとヤヴァイので逆順ループ
        for (int i = targets.Count - 1; i >= 0; i--) {
            if (targets[i] == null) {
                targets.RemoveAt(i);
            }
        }

        //ターゲットがないなら動かない
        if (targets.Count == 0)
            return;

        //ゲーム中
        if (!DropGameManager.instance.isEnd) {
            Move();
            Zoom();
        }
        //ゲームが終わっているときの処理
        else {
            transform.position = endPos;
            transform.eulerAngles = Vector3.zero;
        }
        
    }

    void Move() {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 newPosition = centerPoint + offset;

        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothSpeed);
    }

    void Zoom() {
        float newZoom = Mathf.Lerp(maxZoom, minZoom, GetGreatestDistance() / zoomLimiter);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
    }

    float GetGreatestDistance() {
        if (targets.Count <= 1) return 0f;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (var t in targets) {
            bounds.Encapsulate(t.position);
        }
        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    Vector3 GetCenterPoint() {
        if (targets.Count == 1) {
            return targets[0].position;
        }

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (var t in targets) {
            bounds.Encapsulate(t.position);
        }
        return bounds.center;
    }

    /// <summary>
    /// 外部からプレイヤー追加
    /// </summary>
    public void AddTarget(Transform target) {
        if (!targets.Contains(target))
            targets.Add(target);
    }

    /// <summary>
    /// 外部からプレイヤー削除
    /// </summary>
    public void RemoveTarget(Transform target) {
        if (targets.Contains(target))
            targets.Remove(target);
    }
}

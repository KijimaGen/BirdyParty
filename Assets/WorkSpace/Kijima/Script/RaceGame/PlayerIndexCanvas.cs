using TMPro;
using UnityEngine;

/// <summary>
/// 常にカメラを向くUI（Y軸のねじれを抑制）
/// </summary>
public class PlayerIndexCanvas : MonoBehaviour {
    Canvas canvas;
    int myNumber;
    //
    [SerializeField]
    TextMeshProUGUI text;


    public void InitializeCanvas() {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        //自身の祖先から自身のナンバーを取得
        myNumber = GetComponentInParent<PlayerInfomation>().GetMyNumber() + 1;

        text.text = myNumber.ToString() + "P";
    }

    void LateUpdate() {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); // 反転対策
    }
}


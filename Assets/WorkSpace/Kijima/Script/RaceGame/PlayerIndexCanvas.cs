using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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
        myNumber = GetComponentInParent<PlayerInfomation>().GetMyNumber();

        text.text = myNumber.ToString() + "P";
    }
}


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
        //自身の親を取得
        GameObject player = transform.parent.gameObject;
        myNumber = player.GetComponent<RacePlayer>().GetMyNumber() + 1;

        text.text = myNumber.ToString() + "P";
    }
}


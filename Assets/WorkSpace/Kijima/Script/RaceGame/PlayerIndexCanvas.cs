using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;

/// <summary>
/// ��ɃJ����������UI�iY���̂˂����}���j
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
        //���g�̑c�悩�玩�g�̃i���o�[���擾
        myNumber = GetComponentInParent<PlayerInfomation>().GetMyNumber();

        text.text = myNumber.ToString() + "P";
    }
}


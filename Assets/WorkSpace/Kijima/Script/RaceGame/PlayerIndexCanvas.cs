using TMPro;
using UnityEngine;

/// <summary>
/// ��ɃJ����������UI�iY���̂˂����}���j
/// </summary>
public class PlayerIndexCanvas : MonoBehaviour {
    Canvas canvas;
    int myNumber;
    //
    [SerializeField]
    TextMeshProUGUI text;

    void Start() {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        //���g�̐e���擾
        GameObject player = transform.parent.gameObject;
        myNumber = player.GetComponent<RacePlayer>().GetMyNumber() +1;

        text.text = myNumber.ToString() + "P";
    }

    void LateUpdate() {
        
    }
}


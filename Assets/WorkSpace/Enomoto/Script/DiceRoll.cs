using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceRoll : MonoBehaviour
{
    // サイコロのゲームオブジェクト
    [SerializeField] private GameObject dice;

    private int rotateX;
    private int rotateY;
    private int rotateZ;

    void Update()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            rotateX = Random.Range(0, 360);
            rotateY = Random.Range(0, 360);
            rotateZ = Random.Range(0, 360);

        }
    }
}

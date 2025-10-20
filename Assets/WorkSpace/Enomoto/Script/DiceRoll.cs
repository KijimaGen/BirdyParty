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
            
            dice.transform.position = new Vector3(8.52f, 10.54f, 10.79f);
            dice.GetComponent<Rigidbody>().AddForce(-transform.right * 30);
            dice.transform.Rotate(rotateX, rotateY, rotateZ);
        }
    }
}

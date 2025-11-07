using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceFace : MonoBehaviour
{
    public string faceName;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            transform.GetComponentInParent<DiceRoll>()?.SetBottomFace(faceName);
            transform.GetComponentInParent<BABADiceRoll>()?.SetBottomFace(faceName);
        }
    }
}
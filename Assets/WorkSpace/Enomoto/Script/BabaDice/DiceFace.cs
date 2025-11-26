using UnityEngine;

public class DiceFace : MonoBehaviour
{
    [Tooltip("この面が上を向いた際の数字")]
    public int faceNumber;

    [Tooltip("Dice本体のスクリプト")]
    public DiceObject diceParent;

    [SerializeField]
    private LayerMask groundLayer;
}
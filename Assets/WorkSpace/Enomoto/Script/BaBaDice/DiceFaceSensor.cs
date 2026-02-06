using UnityEngine;

public class DiceFaceSensor : MonoBehaviour
{
    [Range(1, 6)] public int faceValue = 1;
    private DiceActor owner;

    public void Init(DiceActor dice) => owner = dice;

    private void OnTriggerStay(Collider other)
    {
        if (!owner) return;
        if (other.CompareTag("Ground"))
        {
            // ‚±‚Ì–Ê‚ª°‚ÉG‚ê‚Ä‚¢‚é = ã–Ê‚Í”½‘Î‚Ì–Ê
            owner.NotifyFaceTouchingGround(faceValue);
        }
    }
}

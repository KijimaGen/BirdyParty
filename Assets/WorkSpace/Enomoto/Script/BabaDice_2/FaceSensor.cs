using UnityEngine;

public class FaceSensor : MonoBehaviour
{
    [SerializeField, Range(1, 6)] private int faceValue = 1;
    [SerializeField] private DiceFaceDetector detector;
    [SerializeField] private string groundTag = "Ground";

    private void Awake()
    {
        if (detector == null) detector = GetComponentInParent<DiceFaceDetector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(groundTag)) return;
        Debug.Log($"[FaceSensor] ENTER face={faceValue} hit={other.name}");
        detector.NotifyFaceContact(faceValue);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(groundTag)) return;
        detector.NotifyFaceContact(faceValue);
    }
}

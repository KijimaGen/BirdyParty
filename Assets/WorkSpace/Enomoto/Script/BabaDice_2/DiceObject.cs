using UnityEngine;
using Photon.Pun;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class DiceObject : MonoBehaviourPun
{
    [Header("ÉTÉCÉRÉçÇÃê›íË")]
    [SerializeField] private float rollFace = 10f;
    [SerializeField] private float torqueFace = 20f;
    [SerializeField] private Renderer meshRenderer;

    private Rigidbody rb;
    private bool isRolling = false;
    private DiceFace[] faces;

    public int OwnerPlayerNumber { get; private set; } = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        faces = GetComponentsInChildren<DiceFace>();

        rb.useGravity = false;
    }

    public void Initialize(int playerNumber, Material mat)
    {
        OwnerPlayerNumber = playerNumber;
        if (meshRenderer != null && mat != null)
        {
            meshRenderer.material = mat;
        }
    }

    public void RollDice()
    {
        if (isRolling) return;

        isRolling = true;
        rb.isKinematic = false;

        Vector3 randomDir = new Vector3(Random.Range(-1,1), 1f, Random.Range(-1,1)).normalized;
        Vector3 randomTorque = new Vector3(Random.Range(-1,1),Random.Range(-1,1), Random.Range(-1,1)) * torqueFace;

        rb.AddForce(randomDir * rollFace, ForceMode.Impulse);
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    public bool IsSleeping()
    {
        return rb.velocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f;
    }

    public int GetResult()
    {
        if (faces == null && faces.Length == 0) return 0;

        var topFace = faces.OrderByDescending(f => f.transform.position.y).First();
        return topFace.faceNumber;
    }

    public void StopPhysics()
    {
        isRolling = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}

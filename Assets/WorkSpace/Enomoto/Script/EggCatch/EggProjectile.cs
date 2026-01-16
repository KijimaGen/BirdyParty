using UnityEngine;

public class EggProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 6f;
    private int ownerPlayerId = -1;
    public int OwnerPlayerId => ownerPlayerId;

    public void SetOwner(int playerId) => ownerPlayerId = playerId;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerIdentityBinder : MonoBehaviour
{
    private void Awake()
    {
        var pi = GetComponent<PlayerInput>();
        var shooter = GetComponent<BirdShooter>();
        if (shooter != null)
        {
            shooter.SetupPlayer(pi.playerIndex);
        }
    }
}

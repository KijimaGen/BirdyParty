using Photon.Pun;
using UnityEngine;

public class DiceNetToken : MonoBehaviourPun
{
    public int OwnerNumber { get; private set; } = -1;
    public int MaterialIndex { get; private set; } = 0;

    // master‚ª¶¬’¼Œã‚É glocal seth ‚µ‚ÄOKiAllBuffered‚Å“¯Šú‚µ‚½‚¢‚È‚çRPC‰»‚·‚éj
    public void SetOwnerNumberLocal(int n) => OwnerNumber = n;
    public void SetMaterialIndexLocal(int m) => MaterialIndex = m;
}

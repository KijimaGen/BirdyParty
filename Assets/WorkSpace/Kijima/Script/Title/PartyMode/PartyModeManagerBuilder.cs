/**
* @file PartyModeManagerBuilder.cs
* @brief パーティモードのマネージャー作成者
* @author Sum1r3
* @date 2026/01/21
*/
using Photon.Pun;
using UnityEngine;

public class PartyModeManagerBuilder : MonoBehaviour{
    //オンラインぱもま
    [SerializeField]
    private GameObject PartyModeManagerOnline;
    //オフラインぱもま
    [SerializeField]
    private GameObject PartyModeManagerOffline;

    /// <summary>
    /// パーティモードマネージャーを作成
    /// </summary>
    public void BuildPartyModeManager() {
        
       
        Instantiate(PartyModeManagerOffline);
        
        Destroy(gameObject);
    }
}

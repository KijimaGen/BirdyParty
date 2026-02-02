using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class PartyModeRoomSync : MonoBehaviourPunCallbacks
{
    // PartyModeManager と同じキー文字列
    private const string ROOMPROP_CHOICED_LIST = "ChoicedGameList";
    private const string ROOMPROP_CHOICED_INDEX = "ChoicedGameIndexList";

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // PartyModeManager がいないなら何もしない
        if (PartyModeManager.instance == null) return;

        // シーン名リスト受信
        if (propertiesThatChanged.ContainsKey(ROOMPROP_CHOICED_LIST))
        {
            var arr = (string[]) propertiesThatChanged[ROOMPROP_CHOICED_LIST];
            PartyModeManager.instance.ApplySyncedSceneList(arr);
            PartyModeManager.instance.DebugPrintPartyGameList(" (Synced From Room)");
            Debug.Log($"[PartyModeRoomSync] Received ChoicedGameList count={arr.Length}");
        }

        // indexリスト受信
        if (propertiesThatChanged.ContainsKey(ROOMPROP_CHOICED_INDEX))
        {
            var idxArr = (int[]) propertiesThatChanged[ROOMPROP_CHOICED_INDEX];

            if (PartyModeGamePicker.instance != null)
            {
                PartyModeGamePicker.instance.SetGameIndexList(idxArr);
            }

            Debug.Log($"[PartyModeRoomSync] Received ChoicedGameIndexList count={idxArr.Length}");
        }
    }
}


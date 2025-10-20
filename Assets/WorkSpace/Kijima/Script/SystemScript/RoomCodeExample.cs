/**
 * @file RoomCodeExample.cs
 * @brief Example usage of the room code system
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using Photon.Pun;

public class RoomCodeExample : MonoBehaviour
{
    [Header("Example Usage")]
    [SerializeField] private bool autoGenerateOnStart = false;
    
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        
        // Subscribe to events for demonstration
        NetworkManager.OnRoomCodeGenerated += OnExampleRoomCreated;
        NetworkManager.OnRoomJoined += OnExampleRoomJoined;
        
        if (autoGenerateOnStart)
        {
            // Wait a bit for Photon to connect, then create room
            Invoke(nameof(CreateExampleRoom), 3f);
        }
    }
    
    void OnDestroy()
    {
        NetworkManager.OnRoomCodeGenerated -= OnExampleRoomCreated;
        NetworkManager.OnRoomJoined -= OnExampleRoomJoined;
    }
    
    /// <summary>
    /// Example: Create a room with random code
    /// </summary>
    [ContextMenu("Create Example Room")]
    public void CreateExampleRoom()
    {
        if (networkManager != null)
        {
            Debug.Log("Creating example room...");
            networkManager.CreateRandomRoom();
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }
    
    /// <summary>
    /// Example: Join a specific room
    /// </summary>
    /// <param name="roomCode">5-character room code</param>
    public void JoinExampleRoom(string roomCode)
    {
        if (networkManager != null)
        {
            Debug.Log($"Joining room with code: {roomCode}");
            networkManager.JoinRoomWithCode(roomCode);
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }
    
    /// <summary>
    /// Example: Generate room codes for testing
    /// </summary>
    [ContextMenu("Generate Test Codes")]
    public void GenerateTestCodes()
    {
        Debug.Log("Generated room codes:");
        for (int i = 0; i < 5; i++)
        {
            string code = RoomCodeGenerator.GenerateRoomCode();
            Debug.Log($"Code {i + 1}: {code}");
        }
    }
    
    private void OnExampleRoomCreated(string roomCode)
    {
        Debug.Log($"Example: Room created with code {roomCode}");
        Debug.Log($"Share this code with other players: {roomCode}");
    }
    
    private void OnExampleRoomJoined(string roomCode)
    {
        Debug.Log($"Example: Successfully joined room {roomCode}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
    
    /// <summary>
    /// Example: Test room code validation
    /// </summary>
    [ContextMenu("Test Validation")]
    public void TestValidation()
    {
        string[] testCodes = { "ABCD5", "12345", "HELLO", "AB@DE", "abcde", "" };
        
        Debug.Log("Testing room code validation:");
        foreach (string code in testCodes)
        {
            bool isValid = RoomCodeGenerator.IsValidRoomCode(code);
            Debug.Log($"'{code}' -> {(isValid ? "Valid" : "Invalid")}");
        }
    }
}
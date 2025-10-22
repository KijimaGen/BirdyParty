/**
 * @file RoomCodeGenerator.cs
 * @brief Random room code generation utility for PUN2
 * @author Kiro
 * @date 2025/10/20
 */

using System.Text;
using UnityEngine;

public static class RoomCodeGenerator
{
    // Characters to use for room codes (excluding confusing characters like 0, O, I, l)
    // テスト用に「1」も追加（本来は紛らわしいので除外推奨）
    private static readonly char[] ROOM_CODE_CHARS = {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 
        'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    /// <summary>
    /// Generate a random 5-character room code
    /// </summary>
    /// <returns>5-character string suitable for room names</returns>
    public static string GenerateRoomCode()
    {
        StringBuilder roomCode = new StringBuilder(5);
        
        for (int i = 0; i < 5; i++)
        {
            int randomIndex = Random.Range(0, ROOM_CODE_CHARS.Length);
            roomCode.Append(ROOM_CODE_CHARS[randomIndex]);
        }
        
        return roomCode.ToString();
    }

    /// <summary>
    /// Generate a random room code with custom length
    /// </summary>
    /// <param name="length">Length of the room code</param>
    /// <returns>Random string of specified length</returns>
    public static string GenerateRoomCode(int length)
    {
        if (length <= 0) return string.Empty;
        
        StringBuilder roomCode = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            int randomIndex = Random.Range(0, ROOM_CODE_CHARS.Length);
            roomCode.Append(ROOM_CODE_CHARS[randomIndex]);
        }
        
        return roomCode.ToString();
    }

    /// <summary>
    /// Validate if a room code contains only allowed characters
    /// </summary>
    /// <param name="roomCode">Room code to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidRoomCode(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode)) return false;
        
        foreach (char c in roomCode)
        {
            bool isValid = false;
            foreach (char validChar in ROOM_CODE_CHARS)
            {
                if (c == validChar)
                {
                    isValid = true;
                    break;
                }
            }
            if (!isValid) return false;
        }
        
        return true;
    }
}
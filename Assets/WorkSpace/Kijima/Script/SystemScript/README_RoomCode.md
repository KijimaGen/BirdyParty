# PUN2 Random Room Code System

This system provides a complete solution for generating and managing random 5-character room codes in PUN2 (Photon Unity Networking 2).

## Features

- **Random 5-character room code generation**
- **User-friendly character set** (excludes confusing characters like 0, O, I, l)
- **Room code validation**
- **Complete UI integration**
- **Event-driven architecture**
- **Error handling and retry logic**

## Components

### 1. RoomCodeGenerator.cs
Static utility class for generating and validating room codes.

**Key Methods:**
- `GenerateRoomCode()` - Generates a random 5-character code
- `GenerateRoomCode(int length)` - Generates code with custom length
- `IsValidRoomCode(string code)` - Validates room code format

### 2. NetworkManager.cs (Enhanced)
Enhanced PUN2 network manager with room code support.

**Key Methods:**
- `CreateRandomRoom()` - Creates room with random code
- `JoinRoomWithCode(string code)` - Joins room using code
- `GetCurrentRoomCode()` - Returns current room code
- `LeaveRoom()` - Leaves current room

**Events:**
- `OnRoomCodeGenerated` - Fired when room is created
- `OnRoomJoined` - Fired when successfully joined room
- `OnRoomJoinFailed` - Fired when join fails
- `OnConnectedToServer` - Fired when connected to Photon

### 3. RoomCodeUI.cs
UI manager for room code display and input.

**Features:**
- Room code display for hosts
- Room code input for joining
- Copy to clipboard functionality
- Input validation
- Status messages

### 4. NetworkButtonManager.cs
Enhanced button manager with network integration.

**Features:**
- Host/Join game selection
- Dynamic UI configuration
- Button state management
- Status updates

## Setup Instructions

### 1. Basic Setup
1. Add `NetworkManager` to your scene
2. Assign the player prefab in the NetworkManager
3. Ensure Photon is properly configured

### 2. UI Setup (Optional)
1. Create UI panels for network menu and room code
2. Add `RoomCodeUI` or `NetworkButtonManager` to your scene
3. Assign UI references in the inspector

### 3. Integration with Existing Code
```csharp
// Get reference to NetworkManager
NetworkManager networkManager = FindObjectOfType<NetworkManager>();

// Create a room
networkManager.CreateRandomRoom();

// Join a room
networkManager.JoinRoomWithCode("ABCD5");

// Subscribe to events
NetworkManager.OnRoomCodeGenerated += (code) => {
    Debug.Log($"Room created: {code}");
};
```

## Usage Examples

### Creating a Room
```csharp
public void HostGame()
{
    NetworkManager networkManager = FindObjectOfType<NetworkManager>();
    if (networkManager != null)
    {
        networkManager.CreateRandomRoom();
    }
}
```

### Joining a Room
```csharp
public void JoinGame(string roomCode)
{
    NetworkManager networkManager = FindObjectOfType<NetworkManager>();
    if (networkManager != null)
    {
        networkManager.JoinRoomWithCode(roomCode);
    }
}
```

### Generating Test Codes
```csharp
// Generate a single code
string code = RoomCodeGenerator.GenerateRoomCode();
Debug.Log($"Generated code: {code}");

// Generate multiple codes
for (int i = 0; i < 5; i++)
{
    string testCode = RoomCodeGenerator.GenerateRoomCode();
    Debug.Log($"Code {i + 1}: {testCode}");
}
```

## Character Set

The room codes use the following characters to avoid confusion:
- **Letters:** A-Z (excluding I, O)
- **Numbers:** 2-9 (excluding 0, 1)

This ensures codes are easy to read and communicate verbally.

## Error Handling

The system handles various error scenarios:
- **Duplicate room codes:** Automatically generates new code
- **Invalid input:** Validates and filters input
- **Network errors:** Provides user-friendly error messages
- **Connection issues:** Proper state management

## Events System

The system uses C# events for loose coupling:

```csharp
// Subscribe to events
NetworkManager.OnRoomCodeGenerated += OnRoomCreated;
NetworkManager.OnRoomJoined += OnRoomJoined;
NetworkManager.OnRoomJoinFailed += OnJoinFailed;

// Event handlers
private void OnRoomCreated(string roomCode)
{
    // Handle room creation
    DisplayRoomCode(roomCode);
}

private void OnRoomJoined(string roomCode)
{
    // Handle successful join
    StartGame();
}

private void OnJoinFailed(string error)
{
    // Handle join failure
    ShowError(error);
}
```

## Testing

Use `RoomCodeExample.cs` for testing:
1. Add the script to a GameObject
2. Use context menu options in the inspector
3. Enable `autoGenerateOnStart` for automatic testing

## Integration with Existing ButtonManager

To integrate with your existing `ButtonManager.cs`:

```csharp
public class ButtonManager : MonoBehaviour
{
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
    }
    
    public void OnHostGameClicked()
    {
        if (networkManager != null)
        {
            networkManager.CreateRandomRoom();
        }
    }
    
    public void OnJoinGameClicked(string roomCode)
    {
        if (networkManager != null)
        {
            networkManager.JoinRoomWithCode(roomCode);
        }
    }
}
```

## Notes

- Room codes are case-insensitive (automatically converted to uppercase)
- Maximum room capacity is defined in `GameConst.PLAYER_MAX`
- The system automatically handles Photon connection states
- All Japanese text can be easily localized by modifying the strings in the scripts

## Troubleshooting

**Room creation fails:**
- Check Photon connection status
- Verify GameConst.PLAYER_MAX is set correctly
- Check Photon dashboard for room limits

**Join fails:**
- Verify room code is exactly 5 characters
- Check if room exists and has space
- Ensure Photon connection is stable

**UI not updating:**
- Verify event subscriptions in Start()
- Check UI references are assigned
- Ensure NetworkManager is in the scene
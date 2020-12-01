using UnityEngine;

public enum LobbyState
{
    PlayerName,
    MainMenu,
    Host,
    Join,
    Settings,
    ChangeServer,
    Quit
}

public enum LobbyJoinMethod
{
    DirectToMap = 1,
    WaitingRoom = 2,
}
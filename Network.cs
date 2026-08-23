using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

namespace ZexQoLMenu
{
    /// <summary>
    /// Photon network callbacks and event handling
    /// </summary>
    public partial class Plugin
    {
        // Photon Callbacks - implement in derived class
        public virtual void OnConnected() { }
        public virtual void OnConnectedToPhoton() { }
        public virtual void OnDisconnected(DisconnectCause cause) { }
        public virtual void OnRegionListReceived(RegionHandler regionHandler) { }
        public virtual void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public virtual void OnCustomAuthenticationFailed(string debugMessage) { }
        public virtual void OnJoinedLobby() { }
        public virtual void OnLeftLobby() { }
        public virtual void OnRoomListUpdate(List<RoomInfo> roomList) { }
        public virtual void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }
        public virtual void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public virtual void OnCreatedRoom() { }
        public virtual void OnCreateRoomFailed(short returnCode, string message) { }
        public virtual void OnJoinedRoom() { }
        public virtual void OnJoinRoomFailed(short returnCode, string message) { }
        public virtual void OnJoinRandomFailed(short returnCode, string message) { }
        public virtual void OnLeftRoom() { }
        public virtual void OnPlayerEnteredRoom(Player newPlayer) { }
        public virtual void OnPlayerLeftRoom(Player otherPlayer) { }
        public virtual void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
        public virtual void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
        public virtual void OnMasterClientSwitched(Player newMasterClient) { }
        public virtual void OnEventReceived(EventData photonEvent) { }
    }
}

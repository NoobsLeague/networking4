using shared;

namespace server
{
	/**
	 * The LoginRoom is the first room clients 'enter' until the client identifies himself with a PlayerJoinRequest. 
	 * If the client sends the wrong type of request, it will be kicked.
	 *
	 * A connected client that never sends anything will be stuck in here for life,
	 * unless the client disconnects (that will be detected in due time).
	 */ 
	class LoginRoom : SimpleRoom
	{
		//arbitrary max amount just to demo the concept
		private const int MAX_MEMBERS = 50;

		public LoginRoom(TCPGameServer pOwner) : base(pOwner)
		{
		}

		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);

			//notify the client that (s)he is now in the login room, clients can wait for that before doing anything else
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.LOGIN_ROOM;
			pMember.SendMessage(roomJoinedEvent);
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is PlayerJoinRequest)
			{
				handlePlayerJoinRequest(pMessage as PlayerJoinRequest, pSender);
			}
			else //if member sends something else than a PlayerJoinRequest
			{
				Log.LogInfo("Declining client, auth request not understood", this);

				//don't provide info back to the member on what it is we expect, just close and remove
				removeAndCloseMember(pSender);
			}
		}

		/**
		 * check the name, if no duplicates
		 * Tell the client he is accepted and move the client to the lobby room.
		 */
		private void handlePlayerJoinRequest (PlayerJoinRequest pMessage, TcpMessageChannel pSender)
		{
            // Check for duplicate names
            bool alreadyExists = _server
                .GetPlayerInfo(pi => pi.name == pMessage.name)
                .Any();

            var response = new PlayerJoinResponse();
            if (alreadyExists)
            {
                response.result = PlayerJoinResponse.RequestResult.Name_Is_Already_Used;
                pSender.SendMessage(response);
                // stay in login room
                return;
            }

            // accept
            response.result = PlayerJoinResponse.RequestResult.ACCEPTED;
            pSender.SendMessage(response);

            // record name on server
            var info = _server.GetPlayerInfo(pSender);
            info.name = pMessage.name;

            removeMember(pSender);
            _server.GetLobbyRoom().AddMember(pSender);
        }

	}
}

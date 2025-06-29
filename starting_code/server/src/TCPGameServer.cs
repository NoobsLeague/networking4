using System;
using System.Net.Sockets;
using System.Net;
using shared;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace server
{

    /**
	 * Basic TCPGameServer that runs our game.
	 * 
	 * Server is made up out of different rooms that can hold different members.
	 * Each member is identified by a TcpMessageChannel, which can also be used for communication.
	 * In this setup each client is only member of ONE room, but you could change that of course.
	 * 
	 * Each room is responsible for cleaning up faulty clients (since it might involve gameplay, status changes etc).
	 * 
	 * As you can see this setup is limited/lacking:
	 * - only 1 game can be played at a time
	 */
    class TCPGameServer
    {

        public static void Main(string[] args)
        {
            TCPGameServer tcpGameServer = new TCPGameServer();
            tcpGameServer.run();
        }

        //we have 3 different rooms at the moment (aka simple but limited)

        private LoginRoom _loginRoom;   //this is the room every new user joins
        private LobbyRoom _lobbyRoom;   //this is the room a user moves to after a successful 'login'
        private readonly List<GameRoom> _gameRoom;

        private readonly Dictionary<TcpMessageChannel, PlayerInfo> _playerInfo;
        //hearbeat stuff
        private readonly Dictionary<TcpMessageChannel, DateTime> _lastHeartbeats;
        private DateTime _lastHeartbeatSent;

        //initialize
        private TCPGameServer()
        {
            //we have only one instance of each room, this is especially limiting for the game room (since this means you can only have one game at a time).
            _loginRoom = new LoginRoom(this);
            _lobbyRoom = new LobbyRoom(this);
            _gameRoom = new List<GameRoom>();
            _playerInfo = new Dictionary<TcpMessageChannel, PlayerInfo>();
            _lastHeartbeats = new Dictionary<TcpMessageChannel, DateTime>();
            _lastHeartbeatSent = DateTime.MinValue;
        }

        private void run()
        {
            Log.LogInfo("Starting server on port 55555", this, ConsoleColor.Gray);

            //start listening for incoming connections (with max 50 in the queue)
            //we allow for a lot of incoming connections, so we can handle them
            //and tell them whether we will accept them or not instead of bluntly declining them
            TcpListener listener = new TcpListener(IPAddress.Any, 55555);
            listener.Start(50);

            while (true)
            {
                //check for new members	
                if (listener.Pending())
                {
                    //get the waiting client
                    Log.LogInfo("Accepting new client...", this, ConsoleColor.White);
                    TcpClient client = listener.AcceptTcpClient();
                    //and wrap the client in an easier to use communication channel
                    TcpMessageChannel channel = new TcpMessageChannel(client);
                    //and add it to the login room for further 'processing'
                    _loginRoom.AddMember(channel);

                    CheckHeartBeat(channel);
                }
                DateTime now = DateTime.UtcNow;

                if ((now - _lastHeartbeatSent).TotalSeconds >= 5)
                {
                    foreach (var ch in _lastHeartbeats.Keys.ToList())
                        if (ch.Connected)
                            ch.SendMessage(new HeartbeatSend());
                    _lastHeartbeatSent = now;
                }

                foreach (var kv in _lastHeartbeats.ToList())
                {
                    if ((now - kv.Value).TotalSeconds >= 10)
                    {
                        var ch = kv.Key;
                        _lastHeartbeats.Remove(ch);
                        _playerInfo.Remove(ch);
                        ch.Close();
                        Log.LogInfo($"Heartbeat timeout, removed client {ch.GetRemoteEndPoint()}", this);
                    }
                }

                //update rooms
                _loginRoom.Update();
                _lobbyRoom.Update();
                foreach (var gr in _gameRoom.ToList())
                {
                    gr.Update();
                    if (!gr.IsGameInPlay && gr.MemberCount == 0)
                    {
                        Log.LogInfo($"Destroying GameRoom #{gr.RoomId}", this);
                        _gameRoom.Remove(gr);
                    }
                }

                Thread.Sleep(100);
            }

        }

        public void CheckHeartBeat(TcpMessageChannel channel)
        {
            _lastHeartbeats[channel] = DateTime.UtcNow;
        }

        //provide access to the different rooms on the server 
        public LoginRoom GetLoginRoom() { return _loginRoom; }
        public LobbyRoom GetLobbyRoom() { return _lobbyRoom; }

        /**
		 * Returns a handle to the player info for the given client 
		 * (will create new player info if there was no info for the given client yet)
		 */
        public PlayerInfo GetPlayerInfo(TcpMessageChannel pClient)
        {
            if (!_playerInfo.ContainsKey(pClient))
            {
                _playerInfo[pClient] = new PlayerInfo();
            }

            return _playerInfo[pClient];
        }

        /**
		 * Returns a list of all players that match the predicate, e.g. to get a list of 
		 * all players named bob, you would do:
		 *	GetPlayerInfo((playerInfo) => playerInfo.name == "bob");
		 */
        public List<PlayerInfo> GetPlayerInfo(Predicate<PlayerInfo> pPredicate)
        {
            return _playerInfo.Values.ToList<PlayerInfo>().FindAll(pPredicate);
        }

        /**
		 * Should be called by a room when a member is closed and removed.
		 */
        public void RemovePlayerInfo(TcpMessageChannel pClient)
        {
            _playerInfo.Remove(pClient);
        }

        //send the bois to the room
        public void StartGame(TcpMessageChannel p1, TcpMessageChannel p2)
        {
            // CRITICAL: Verify these players are not already in a game
            foreach (var existingRoom in _gameRoom)
            {
                if (existingRoom.IsGameInPlay && (existingRoom.HasPlayer(p1) || existingRoom.HasPlayer(p2)))
                {
                    Log.LogInfo($"ERROR: Tried to start game with players already in game! P1: {GetPlayerInfo(p1).name}, P2: {GetPlayerInfo(p2).name}", this, ConsoleColor.Red);
                    return;
                }
            }

            // Double-check that these players are not still in the lobby
            if (_lobbyRoom.HasMember(p1) || _lobbyRoom.HasMember(p2))
            {
                Log.LogInfo($"ERROR: Players still in lobby when starting game! P1 in lobby: {_lobbyRoom.HasMember(p1)}, P2 in lobby: {_lobbyRoom.HasMember(p2)}", this, ConsoleColor.Red);
                // Force remove them from lobby
                _lobbyRoom.ForceRemoveMember(p1);
                _lobbyRoom.ForceRemoveMember(p2);
            }

            int id = _gameRoom.Count + 1;
            var room = new GameRoom(this, id);
            _gameRoom.Add(room);

            Log.LogInfo($"Starting new game #{id} with {GetPlayerInfo(p1).name} vs {GetPlayerInfo(p2).name}", this, ConsoleColor.Green);
            Log.LogInfo($"P1 Channel: {p1.GetRemoteEndPoint()}, P2 Channel: {p2.GetRemoteEndPoint()}", this, ConsoleColor.Green);

            room.StartGame(p1, p2);
        }

        // Method to find which room a player is currently in
        public string GetPlayerLocation(TcpMessageChannel player)
        {
            if (_loginRoom.HasMember(player)) return "LoginRoom";
            if (_lobbyRoom.HasMember(player)) return "LobbyRoom";

            foreach (var gameRoom in _gameRoom)
            {
                if (gameRoom.HasPlayer(player)) return $"GameRoom #{gameRoom.RoomId}";
            }

            return "Unknown";
        }
    }
}
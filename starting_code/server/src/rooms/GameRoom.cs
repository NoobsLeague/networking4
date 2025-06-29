using shared;
using System;
using System.Collections.Generic;

namespace server
{
    /**
     * This room runs a single Game (at a time). 
     * Now with proper turn-based gameplay.
     */
    class GameRoom : Room
    {
        public bool IsGameInPlay { get; private set; }

        // Keep track of the member count, to make sure its not empty
        public int MemberCount => base.memberCount;
        // Get the room id
        public int RoomId { get; }

        // Wraps the board to play on...
        private TicTacToeBoard _board;

        // To keep track of who is in a game - IMPORTANT: Use the exact same players that were passed in
        private readonly List<TcpMessageChannel> _players = new List<TcpMessageChannel>();

        public GameRoom(TCPGameServer pOwner, int id) : base(pOwner)
        {
            // Pass the room id
            RoomId = id;
        }

        // Method to check if a specific player is in this game
        public bool HasPlayer(TcpMessageChannel player)
        {
            return _players.Contains(player);
        }

        public void StartGame(TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
        {
            if (pPlayer1 == null || pPlayer2 == null)
            {
                Log.LogInfo("ERROR: Cannot start game with null players!", this, ConsoleColor.Red);
                return;
            }

            if (pPlayer1 == pPlayer2)
            {
                Log.LogInfo("ERROR: Cannot start game with the same player twice!", this, ConsoleColor.Red);
                return;
            }

            IsGameInPlay = true;
            // Initialize the board here so it can be repeated (not only 1 game)
            _board = new TicTacToeBoard();
            // Clear to make sure
            _players.Clear();
            // Add players in the EXACT ORDER they were passed in
            _players.Add(pPlayer1);
            _players.Add(pPlayer2);

            // Get player names - use the EXACT players we stored
            var p1Name = _server.GetPlayerInfo(pPlayer1).name;
            var p2Name = _server.GetPlayerInfo(pPlayer2).name;

            Log.LogInfo($"Starting game between {p1Name} (Player 1) and {p2Name} (Player 2)", this);
            Log.LogInfo($"Player 1 channel: {pPlayer1.GetRemoteEndPoint()}", this);
            Log.LogInfo($"Player 2 channel: {pPlayer2.GetRemoteEndPoint()}", this);

            // Add them to the room AFTER storing them in _players to maintain order
            addMember(pPlayer1);
            addMember(pPlayer2);

            // Verify the room membership
            Log.LogInfo($"GameRoom #{RoomId} now has {memberCount} members", this);

            // Send player names to both clients - CRITICAL: Use the stored names
            Log.LogInfo($"Sending PlayerNames: Player1={p1Name}, Player2={p2Name}", this);
            sendToAll(new PlayerNames { player1Name = p1Name, player2Name = p2Name });

            // Send initial board state with player 1's turn
            MakeMoveResult initialState = new MakeMoveResult();
            initialState.whoMadeTheMove = 0; // No move made yet
            initialState.boardData = _board.GetBoardData();
            Log.LogInfo($"Sending initial board state", this);
            sendToAll(initialState);
        }

        protected override void addMember(TcpMessageChannel pMember)
        {
            base.addMember(pMember);

            // CRITICAL: Only add to room members if they're one of the game players
            if (!_players.Contains(pMember))
            {
                Log.LogInfo($"ERROR: Trying to add {_server.GetPlayerInfo(pMember).name} to GameRoom but they're not a game player!", this, ConsoleColor.Red);
                removeMember(pMember); // Remove them immediately
                return;
            }

            // Notify client they have joined a game room 
            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
            roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
            pMember.SendMessage(roomJoinedEvent);

            Log.LogInfo($"Added {_server.GetPlayerInfo(pMember).name} to GameRoom #{RoomId}", this);
        }

        public override void Update()
        {
            // Demo of how we can tell people have left the game...
            int oldMemberCount = memberCount;
            base.Update();
            int newMemberCount = memberCount;

            if (oldMemberCount != newMemberCount)
            {
                Log.LogInfo("People left the game...", this);

                // If someone disconnected and the game is still in play,
                // consider it a concede
                if (IsGameInPlay && newMemberCount < 2)
                {
                    Log.LogInfo("Player disconnected, ending game", this);
                    // If we still have one player connected, they win
                    if (newMemberCount == 1)
                    {
                        // Get the remaining player from our _players list
                        TcpMessageChannel remainingPlayer = null;
                        foreach (var player in _players)
                        {
                            if (player.Connected)
                            {
                                remainingPlayer = player;
                                break;
                            }
                        }

                        if (remainingPlayer != null)
                        {
                            gameFinished(remainingPlayer);
                        }
                    }
                    else
                    {
                        // Everyone disconnected, just mark game as over
                        IsGameInPlay = false;
                    }
                }
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            if (pMessage is MakeMoveRequest)
            {
                handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
            }
            else if (pMessage is ConcedeRequest)
            {
                // Find the winner from our _players list
                var winner = _players[0] == pSender ? _players[1] : _players[0];
                gameFinished(winner);
            }
            else if (pMessage is ReturnToLobby lobby)
            {
                Log.LogInfo($"[Server] got ReturnToLobbyRequest from {pSender.GetRemoteEndPoint()}", this);
                removeMember(pSender);
                _server.GetLobbyRoom().backToTheLobby(pSender);
                var chat = new ChatMessage
                {
                    message = lobby.areYaWinninSon
                        ? "I won the game!"
                        : "I lost the game."
                };
                pSender.SendMessage(chat);
                return;
            }
        }

        private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
        {
            // Get the player index (0 or 1) from our stored _players list
            int playerIndex = _players.IndexOf(pSender);
            if (playerIndex == -1)
            {
                Log.LogInfo($"Received move from player not in this game! Sender: {pSender.GetRemoteEndPoint()}", this);
                Log.LogInfo($"Expected players: {_players[0]?.GetRemoteEndPoint()} and {_players[1]?.GetRemoteEndPoint()}", this);
                return;
            }

            // Convert to player ID (1 or 2)
            int playerID = playerIndex + 1;

            // Check if it's this player's turn
            TicTacToeBoardData boardData = _board.GetBoardData();
            Log.LogInfo($"Current board state: {boardData.ToString()}", this);
            Log.LogInfo($"Current turn: {boardData.currentTurn}, Player making move: {playerID} (Index: {playerIndex})", this);
            Log.LogInfo($"Player {playerID} name: {_server.GetPlayerInfo(pSender).name}", this);

            if (boardData.currentTurn != playerID)
            {
                Log.LogInfo($"Ignoring move from player {playerID} - not their turn!", this);
                return;
            }

            // Log the move
            Log.LogInfo($"Player {playerID} making move at position {pMessage.move}", this);

            // Try to make the requested move
            bool moveSuccessful = _board.TryMakeMove(pMessage.move, playerID);
            Log.LogInfo($"Move successful: {moveSuccessful}", this);

            if (moveSuccessful)
            {
                // Update board data after the move
                boardData = _board.GetBoardData();
                Log.LogInfo($"Board after move: {boardData.ToString()}", this);

                // Send the updated board to all clients
                MakeMoveResult makeMoveResult = new MakeMoveResult();
                makeMoveResult.whoMadeTheMove = playerID;
                makeMoveResult.boardData = boardData;
                Log.LogInfo($"Sending MakeMoveResult: {makeMoveResult}", this);
                sendToAll(makeMoveResult);

                // Check for win/draw
                int winnerId = boardData.WhoHasWon();
                if (winnerId > 0)
                {
                    Log.LogInfo($"Game has a result: {winnerId}", this);
                    if (winnerId == 3) // Draw
                    {
                        // Handle draw - both players get a draw message
                        gameFinishedDraw();
                    }
                    else // Player 1 or 2 won
                    {
                        // Get the winner's channel from our _players list
                        TcpMessageChannel winner = _players[winnerId - 1];
                        gameFinished(winner);
                    }
                }
            }
            else
            {
                // Move was invalid, could send an error message here if you want
                Log.LogInfo($"Invalid move from player {playerID} at position {pMessage.move}", this);
            }
        }

        private void gameFinished(TcpMessageChannel winner)
        {
            // Find the loser from our _players list
            var loser = _players[0] == winner ? _players[1] : _players[0];
            var data = _board.GetBoardData();

            Log.LogInfo($"Game finished! Winner: {_server.GetPlayerInfo(winner).name}, Loser: {_server.GetPlayerInfo(loser).name}", this);

            // Send appropriate messages to each player
            if (winner.Connected)
            {
                winner.SendMessage(new GameFinished { boardData = data, YesDadImWinnin = true });
            }

            if (loser.Connected)
            {
                loser.SendMessage(new GameFinished { boardData = data, YesDadImWinnin = false });
            }

            IsGameInPlay = false;
        }

        private void gameFinishedDraw()
        {
            var data = _board.GetBoardData();

            Log.LogInfo("Game finished in a draw!", this);

            // Send draw result to both players from our _players list
            foreach (var player in _players)
            {
                if (player.Connected)
                {
                    player.SendMessage(new GameFinished
                    {
                        boardData = data,
                        YesDadImWinnin = false, // Neither player wins
                        IsDraw = true
                    });
                }
            }

            IsGameInPlay = false;
        }
    }
}
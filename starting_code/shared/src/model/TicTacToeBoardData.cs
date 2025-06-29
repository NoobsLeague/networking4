using System;

namespace shared
{
    /**
     * Super simple board model for TicTacToe that contains the minimal data to actually represent the board. 
     * It now includes turn tracking and proper win condition logic.
     */
    public class TicTacToeBoardData : ASerializable
    {
        //board representation in 1d array, one element for each cell
        //0 is empty, 1 is player 1, 2 is player 2
        public int[] board = new int[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Track whose turn it is (1 for player 1, 2 for player 2)
        public int currentTurn = 1; // Player 1 starts by default

        /**
         * Returns who has won.
         * 
         * 0 = no winner yet
         * 1 = player 1 won
         * 2 = player 2 won
         * 3 = draw
         */
        public int WhoHasWon()
        {
            // Check rows
            for (int i = 0; i < 9; i += 3)
            {
                if (board[i] != 0 && board[i] == board[i + 1] && board[i] == board[i + 2])
                {
                    return board[i]; // Return the player number (1 or 2)
                }
            }

            // Check columns
            for (int i = 0; i < 3; i++)
            {
                if (board[i] != 0 && board[i] == board[i + 3] && board[i] == board[i + 6])
                {
                    return board[i]; // Return the player number (1 or 2)
                }
            }

            // Check diagonals
            if (board[0] != 0 && board[0] == board[4] && board[0] == board[8])
            {
                return board[0]; // Return the player number (1 or 2)
            }

            if (board[2] != 0 && board[2] == board[4] && board[2] == board[6])
            {
                return board[2]; // Return the player number (1 or 2)
            }

            // Check for draw (board is full)
            bool boardIsFull = true;
            foreach (int cell in board)
            {
                if (cell == 0)
                {
                    boardIsFull = false;
                    break;
                }
            }

            if (boardIsFull)
            {
                return 3; // Draw
            }

            return 0; // No winner yet
        }

        /**
         * Switches to the next player's turn
         */
        public void SwitchTurn()
        {
            currentTurn = (currentTurn == 1) ? 2 : 1;
            Console.WriteLine($"Turn switched to player {currentTurn}");
        }

        /**
         * Make a move if the cell is empty
         * Returns true if the move was valid and made
         */
        public bool TryMakeMove(int cellIndex, int playerNumber)
        {
            if (cellIndex < 0 || cellIndex >= 9) return false;
            if (board[cellIndex] != 0) return false;
            if (playerNumber != currentTurn) return false;

            Console.WriteLine($"Player {playerNumber} making move at {cellIndex}");
            board[cellIndex] = playerNumber;
            Console.WriteLine($"Before switching: currentTurn={currentTurn}");
            SwitchTurn();
            Console.WriteLine($"After switching: currentTurn={currentTurn}");
            return true;
        }

        public override void Serialize(Packet pPacket)
        {
            // Serialize the board
            for (int i = 0; i < board.Length; i++)
            {
                pPacket.Write(board[i]);
            }

            // Serialize current turn
            Console.WriteLine($"Serializing turn: {currentTurn}");
            pPacket.Write(currentTurn);
        }

        public override void Deserialize(Packet pPacket)
        {
            // Deserialize the board
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = pPacket.ReadInt();
            }

            // Deserialize current turn
            currentTurn = pPacket.ReadInt();
            Console.WriteLine($"Deserialized turn: {currentTurn}");
        }

        public override string ToString()
        {
            return GetType().Name + ":" + string.Join(",", board) + " Turn:" + currentTurn;
        }
    }
}
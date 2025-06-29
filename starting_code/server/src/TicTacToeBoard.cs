
using shared;

namespace server
{
    /**
     * Server-side wrapper around TicTacToeBoardData.
     * Implements game logic for TicTacToe.
     */
    public class TicTacToeBoard
    {
        private TicTacToeBoardData _boardData;

        public TicTacToeBoard()
        {
            _boardData = new TicTacToeBoardData();
            _boardData.currentTurn = 1; // Player 1 starts
        }

        public TicTacToeBoardData GetBoardData()
        {
            return _boardData;
        }

        /**
         * Try to make a move for the specified player.
         * Returns true if the move was successful.
         */
        public bool TryMakeMove(int cellIndex, int playerNumber)
        {
            return _boardData.TryMakeMove(cellIndex, playerNumber);
        }

        /**
         * Make a move for the specified player.
         * This method doesn't check if it's the player's turn,
         * and is kept for compatibility with existing code.
         */
        public void MakeMove(int cellIndex, int playerNumber)
        {
            // Check if cell is already occupied
            if (cellIndex < 0 || cellIndex >= 9 || _boardData.board[cellIndex] != 0)
            {
                return;
            }

            _boardData.board[cellIndex] = playerNumber;
            _boardData.SwitchTurn();
        }
    }
}
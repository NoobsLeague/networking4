using shared;
using UnityEngine;
using System;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    // Keep track of how many times a player clicked the board
    private int player1MoveCount = 0;
    private int player2MoveCount = 0;
    
    // Player names
    private string player1Name = "Player 1";
    private string player2Name = "Player 2";
    
    // Current player ID (1 or 2)
    private int currentPlayerId = 1; // Player 1 starts by default
    
    // Our local username to identify which player we are
    private string myName;
    
    // A fixed player ID for this client
    private int myFixedPlayerId = 0;
    
    // Last move played by this client
    private int myLastMove = -1;

    public override void EnterState()
    {
        base.EnterState();
        
        // Store our username (from the login screen)
        myName = PlayerPrefs.GetString("Username", "Unknown");
        Debug.Log("My username is: " + myName);
        
        view.gameBoard.OnCellClicked += _onCellClicked;
        view.QuitGameButton.onClick.AddListener(QuitGame);
        
        // Reset game state
        player1MoveCount = 0;
        player2MoveCount = 0;
        currentPlayerId = 1; // Player 1 starts by default
        myFixedPlayerId = 0; // Reset player ID
        myLastMove = -1;
        
        Debug.Log("Game state entered. Waiting for player names...");
    }

    private void _onCellClicked(int pCellIndex)
    {
        Debug.Log($"Cell clicked: {pCellIndex}, myFixedPlayerId: {myFixedPlayerId}, currentPlayerId: {currentPlayerId}");
        
        // If we haven't determined our player ID yet, but it's player 1's turn, try to make a move
        // This will work for player 1's first move
        if (myFixedPlayerId == 0 && currentPlayerId == 1)
        {
            Debug.Log("First move of the game - attempting as player 1");
            // Track this as my move so we can identify ourselves when we get the result
            myLastMove = pCellIndex;
            MakeMoveRequest req = new MakeMoveRequest();
            req.move = pCellIndex;
            fsm.channel.SendMessage(req);
            return;
        }
        
        // For all other moves, we need to know our player ID
        if (myFixedPlayerId == 0)
        {
            Debug.Log("Cannot make move - player ID not determined yet");
            return;
        }
        
        // If it's not my turn, don't allow the move
        if (myFixedPlayerId != currentPlayerId)
        {
            Debug.Log($"Not your turn! Your ID: {myFixedPlayerId}, Current turn: {currentPlayerId}");
            return;
        }
        
        // Send the move request
        myLastMove = pCellIndex; // Track this as my move
        MakeMoveRequest request = new MakeMoveRequest();
        request.move = pCellIndex;
        fsm.channel.SendMessage(request);
    }

    private void QuitGame()
    {
        // Send concede request to the server
        fsm.channel.SendMessage(new ConcedeRequest());
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
        view.QuitGameButton.onClick.RemoveListener(QuitGame);
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
        
        // Update UI to indicate whose turn it is
        UpdateTurnIndicator();
    }
    
    private void UpdateTurnIndicator()
    {
        // Add visual indicator for whose turn it is
        if (view.playerLabel1 != null && view.playerLabel2 != null)
        {
            string player1Text = $"{player1Name} (Moves: {player1MoveCount})";
            string player2Text = $"{player2Name} (Moves: {player2MoveCount})";
            
            // Add turn indicator
            if (currentPlayerId == 1)
            {
                player1Text = "→ " + player1Text + " ←";
            }
            else if (currentPlayerId == 2)
            {
                player2Text = "→ " + player2Text + " ←";
            }
            
            // Add "YOU" indicator to show which player you are
            if (myFixedPlayerId == 1)
            {
                player1Text += " (YOU)";
            }
            else if (myFixedPlayerId == 2)
            {
                player2Text += " (YOU)";
            }
            
            view.playerLabel1.text = player1Text;
            view.playerLabel2.text = player2Text;
        }
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is MakeMoveResult makeMoveResult)
        {
            handleMakeMoveResult(makeMoveResult);
        }
        else if (pMessage is PlayerNames playerNames)
        {
            handlePlayerNames(playerNames);
        }
        else if (pMessage is GameFinished gameFinished)
        {
            handleGameFinished(gameFinished);
        }
    }

    private void handlePlayerNames(PlayerNames playerNames)
    {
        player1Name = playerNames.player1Name;
        player2Name = playerNames.player2Name;
        
        Debug.Log($"Received player names - Player 1: {player1Name}, Player 2: {player2Name}");
        
        // Try to determine which player we are by name matching
        if (string.Equals(myName, player1Name, StringComparison.OrdinalIgnoreCase))
        {
            myFixedPlayerId = 1;
            Debug.Log("I am Player 1 based on name matching");
        }
        else if (string.Equals(myName, player2Name, StringComparison.OrdinalIgnoreCase))
        {
            myFixedPlayerId = 2;
            Debug.Log("I am Player 2 based on name matching");
        }
        else
        {
            Debug.LogWarning($"Unable to determine player ID from names! myName: {myName}, player1Name: {player1Name}, player2Name: {player2Name}");
            // We'll determine our player ID based on move results
        }
        
        UpdateTurnIndicator();
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        Debug.Log("Received MakeMoveResult: " + pMakeMoveResult);
        
        // If we don't know our player ID yet, try to determine it
        if (myFixedPlayerId == 0)
        {
            // If we made the last move (cell index matches) and we know who made this move,
            // then that's our player ID
            if (myLastMove >= 0 && myLastMove == findLastMovePosition(pMakeMoveResult.boardData.board))
            {
                myFixedPlayerId = pMakeMoveResult.whoMadeTheMove;
                Debug.Log($"Determined I am Player {myFixedPlayerId} based on move matching");
            }
            else if (currentPlayerId == 1 && pMakeMoveResult.whoMadeTheMove == 1)
            {
                // If we're still at the beginning and player 1 just moved, we must be player 2
                myFixedPlayerId = 2;
                Debug.Log("Determined I am Player 2 (player 1 just moved)");
            }
        }
        
        // Validate board data
        if (pMakeMoveResult.boardData == null)
        {
            Debug.LogError("MakeMoveResult has null board data!");
            return;
        }
        
        // Log the board data for debugging
        Debug.Log("Board data received: " + pMakeMoveResult.boardData.ToString());
        Debug.Log("Board array: " + (pMakeMoveResult.boardData.board != null ? 
                                    string.Join(",", pMakeMoveResult.boardData.board) : "NULL"));
        Debug.Log("Current turn from board: " + pMakeMoveResult.boardData.currentTurn);
        
        // Update the board visual representation
        if (view != null && view.gameBoard != null)
        {
            Debug.Log("Calling SetBoardData on the gameBoard");
            view.gameBoard.SetBoardData(pMakeMoveResult.boardData);
        }
        else
        {
            Debug.LogError("GameView or GameBoard is null! view: " + (view != null) + 
                        ", gameBoard: " + (view != null && view.gameBoard != null));
        }
        
        // Update turn tracking
        int oldTurn = currentPlayerId;
        currentPlayerId = pMakeMoveResult.boardData.currentTurn;
        
        Debug.Log($"Turn switched from {oldTurn} to {currentPlayerId}");
        Debug.Log($"Move result received. Who made move: {pMakeMoveResult.whoMadeTheMove}, " +
                "Next turn: {currentPlayerId}");
        
        // Increment move counter for the player who moved
        if (pMakeMoveResult.whoMadeTheMove == 1)
        {
            player1MoveCount++;
        }
        else if (pMakeMoveResult.whoMadeTheMove == 2)
        {
            player2MoveCount++;
        }
        
        UpdateTurnIndicator();
    }
    
    // Helper method to find the position of the last move
    private int findLastMovePosition(int[] board)
    {
        // Count how many non-zero cells to determine which move number we're on
        int moveCount = 0;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != 0) moveCount++;
        }
        
        // For the first move
        if (moveCount == 1)
        {
            // Find the position of the 1 (player 1's first move)
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 1) return i;
            }
        }
        // For the second move
        else if (moveCount == 2)
        {
            // Find the position of the 2 (player 2's first move)
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 2) return i;
            }
        }
        // For later moves, we'd need to compare with the previous board state
        // For now, return -1 which won't match myLastMove
        return -1;
    }

    private void handleGameFinished(GameFinished gameFinished)
    {
        string resultMessage;
        
        if (gameFinished.IsDraw)
        {
            resultMessage = "Game ended in a draw!";
            Debug.Log(resultMessage);
        }
        else
        {
            resultMessage = gameFinished.YesDadImWinnin ? "You won!" : "You lost!";
            Debug.Log("Game finished! Result: " + resultMessage);
        }
        
        // Switch to Results state
        Results resultsState = FindResultsState();
        if (resultsState != null)
        {
            resultsState.InitializeEnd(gameFinished.boardData, gameFinished.YesDadImWinnin, gameFinished.IsDraw);
            fsm.ChangeState<Results>();
        }
        else
        {
            Debug.LogError("Results state not found!");
        }
    }
    
    private Results FindResultsState()
    {
        // Try multiple approaches to find the Results component
        
        // 1. Look for a Results component on the same GameObject as this script
        Results resultsState = GetComponent<Results>();
        if (resultsState != null) 
        {
            Debug.Log("Found Results component on the same GameObject");
            return resultsState;
        }
        
        // 2. Try to find it as a direct component of the FSM
        resultsState = fsm.GetComponent<Results>();
        if (resultsState != null)
        {
            Debug.Log("Found Results component on the FSM GameObject");
            return resultsState;
        }
        
        // 3. Try to find it from sibling GameObjects (other states)
        Transform parent = transform.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                resultsState = child.GetComponent<Results>();
                if (resultsState != null)
                {
                    Debug.Log("Found Results component on sibling: " + child.name);
                    return resultsState;
                }
            }
        }
        
        // 4. As a last resort, try to find it anywhere in the scene
        resultsState = UnityEngine.Object.FindObjectOfType<Results>();
        if (resultsState != null)
        {
            Debug.Log("Found Results component in scene: " + resultsState.name);
            return resultsState;
        }
        
        Debug.LogError("Could not find Results component anywhere!");
        return null;
    }
}
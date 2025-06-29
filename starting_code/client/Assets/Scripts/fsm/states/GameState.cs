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
    
    // A fixed player ID for this client (1 or 2, relative to THIS game)
    private int myPlayerId = 0;
    
    // Track if the game has ended
    private bool gameHasEnded = false;
    
    // Store the final board data
    private TicTacToeBoardData finalBoardData = null;
    
    // Track if we've received player names
    private bool receivedPlayerNames = false;

    public override void EnterState()
    {
        base.EnterState();
        
        // FIXED: Get username from FSM instead of PlayerPrefs
        myName = fsm.GetUsername();
        
        Debug.Log($"[GameState] My username from FSM: '{myName}'");
        Debug.Log($"[GameState] PlayerPrefs username (for comparison): '{PlayerPrefs.GetString("Username", "Unknown")}'");
        
        view.gameBoard.OnCellClicked += _onCellClicked;
        view.QuitGameButton.onClick.AddListener(QuitGame);
        
        // Reset game state
        player1MoveCount = 0;
        player2MoveCount = 0;
        currentPlayerId = 1; // Player 1 starts by default
        myPlayerId = 0; // Reset player ID
        gameHasEnded = false;
        finalBoardData = null;
        receivedPlayerNames = false;
        
        // Clear the board when entering a new game
        if (view.gameBoard != null)
        {
            view.gameBoard.ClearBoard();
        }
        
        Debug.Log($"[GameState] Game state entered. My name: '{myName}'. Waiting for player names...");
    }

    private void _onCellClicked(int pCellIndex)
    {
        // Don't allow moves if the game has ended
        if (gameHasEnded)
        {
            Debug.Log("Game has ended, no more moves allowed");
            return;
        }
        
        // Don't allow moves until we know who we are
        if (!receivedPlayerNames || myPlayerId == 0)
        {
            Debug.Log("Cannot make move - player identity not established yet");
            return;
        }
        
        Debug.Log($"Cell clicked: {pCellIndex}, myPlayerId: {myPlayerId}, currentPlayerId: {currentPlayerId}");
        
        // If it's not my turn, don't allow the move
        if (myPlayerId != currentPlayerId)
        {
            Debug.Log($"Not your turn! Your ID: {myPlayerId}, Current turn: {currentPlayerId}");
            return;
        }
        
        // Send the move request
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
            
            // Only show turn indicator if game hasn't ended
            if (!gameHasEnded)
            {
                // Add turn indicator
                if (currentPlayerId == 1)
                {
                    player1Text = "→ " + player1Text + " ←";
                }
                else if (currentPlayerId == 2)
                {
                    player2Text = "→ " + player2Text + " ←";
                }
            }
            
            // Add "YOU" indicator to show which player you are
            if (myPlayerId == 1)
            {
                player1Text += " (YOU)";
            }
            else if (myPlayerId == 2)
            {
                player2Text += " (YOU)";
            }
            
            view.playerLabel1.text = player1Text;
            view.playerLabel2.text = player2Text;
        }
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        // Add debugging to see what messages this client is receiving
        Debug.Log($"[GameState] {myName} received message: {pMessage.GetType().Name}");
        
        if (pMessage is MakeMoveResult makeMoveResult)
        {
            Debug.Log($"[GameState] {myName} received MakeMoveResult");
            handleMakeMoveResult(makeMoveResult);
        }
        else if (pMessage is PlayerNames playerNames)
        {
            Debug.Log($"[GameState] {myName} received PlayerNames: P1={playerNames.player1Name}, P2={playerNames.player2Name}");
            handlePlayerNames(playerNames);
        }
        else if (pMessage is GameFinished gameFinished)
        {
            Debug.Log($"[GameState] {myName} received GameFinished");
            handleGameFinished(gameFinished);
        }
        else
        {
            Debug.Log($"[GameState] {myName} received unknown message: {pMessage.GetType().Name}");
        }
    }

    private void handlePlayerNames(PlayerNames playerNames)
    {
        player1Name = playerNames.player1Name;
        player2Name = playerNames.player2Name;
        receivedPlayerNames = true;
        
        Debug.Log($"[GameState] Received player names - Player 1: {player1Name}, Player 2: {player2Name}");
        Debug.Log($"[GameState] My name is: '{myName}'");
        Debug.Log($"[GameState] My name length: {myName.Length}");
        Debug.Log($"[GameState] Player1 name length: {player1Name.Length}");
        Debug.Log($"[GameState] Player2 name length: {player2Name.Length}");
        
        // Determine which player we are based on exact name matching
        if (string.Equals(myName, player1Name, StringComparison.Ordinal))
        {
            myPlayerId = 1;
            Debug.Log($"[GameState] I am Player 1 (exact match)");
        }
        else if (string.Equals(myName, player2Name, StringComparison.Ordinal))
        {
            myPlayerId = 2;
            Debug.Log($"[GameState] I am Player 2 (exact match)");
        }
        else
        {
            Debug.LogError($"[GameState] CRITICAL: Unable to determine player ID!");
            Debug.LogError($"[GameState] My name: '{myName}' (length: {myName.Length})");
            Debug.LogError($"[GameState] Player1: '{player1Name}' (length: {player1Name.Length})");
            Debug.LogError($"[GameState] Player2: '{player2Name}' (length: {player2Name.Length})");
            Debug.LogError($"[GameState] This suggests I'm receiving messages for a game I'm not in!");
            
            // Check if this client should even be receiving this message
            Debug.LogError($"[GameState] FSM Username: '{fsm.GetUsername()}'");
            Debug.LogError($"[GameState] PlayerPrefs Username: '{PlayerPrefs.GetString("Username", "Unknown")}'");
            
            return; // Don't proceed if we can't identify ourselves
        }
        
        UpdateTurnIndicator();
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        Debug.Log($"[GameState] {myName} received MakeMoveResult: " + pMakeMoveResult);
        
        // Validate board data
        if (pMakeMoveResult.boardData == null)
        {
            Debug.LogError("MakeMoveResult has null board data!");
            return;
        }
        
        // Log the board data for debugging
        Debug.Log("Board data received: " + pMakeMoveResult.boardData.ToString());
        Debug.Log("Board array: " + string.Join(",", pMakeMoveResult.boardData.board));
        Debug.Log("Current turn from board: " + pMakeMoveResult.boardData.currentTurn);
        
        // Update the board visual representation
        if (view != null && view.gameBoard != null)
        {
            Debug.Log("Calling SetBoardData on the gameBoard");
            view.gameBoard.SetBoardData(pMakeMoveResult.boardData);
        }
        else
        {
            Debug.LogError("GameView or GameBoard is null!");
        }
        
        // Update turn tracking
        int oldTurn = currentPlayerId;
        currentPlayerId = pMakeMoveResult.boardData.currentTurn;
        
        Debug.Log($"Turn switched from {oldTurn} to {currentPlayerId}");
        Debug.Log($"Move result received. Who made move: {pMakeMoveResult.whoMadeTheMove}, Next turn: {currentPlayerId}");
        
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

    private void handleGameFinished(GameFinished gameFinished)
    {
        gameHasEnded = true;
        finalBoardData = gameFinished.boardData;
        
        // Make sure the final board state is displayed
        if (finalBoardData != null && view != null && view.gameBoard != null)
        {
            view.gameBoard.SetBoardData(finalBoardData);
            Debug.Log($"Final board state set in GameState: {string.Join(",", finalBoardData.board)}");
        }
        
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
        
        // Update the turn indicator to show game has ended
        UpdateTurnIndicator();
        
        // IMPORTANT: Initialize the Results state BEFORE changing to it
        Results resultsState = FindResultsState();
        if (resultsState != null)
        {
            // Pass the board data to Results state before the transition
            Debug.Log($"Passing board data to Results state: {string.Join(",", gameFinished.boardData.board)}");
            resultsState.InitializeEnd(gameFinished.boardData, gameFinished.YesDadImWinnin, gameFinished.IsDraw);
            
            // Small delay to ensure the data is set before state change
            StartCoroutine(DelayedStateChange());
        }
        else
        {
            Debug.LogError("Results state not found!");
        }
    }
    
    private System.Collections.IEnumerator DelayedStateChange()
    {
        // Wait one frame to ensure InitializeEnd has been processed
        yield return null;
        fsm.ChangeState<Results>();
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
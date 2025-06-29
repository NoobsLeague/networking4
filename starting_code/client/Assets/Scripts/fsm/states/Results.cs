using UnityEngine;
using shared;
using System.Collections.Generic;

public class Results : ApplicationStateWithView<ResultsView>
{
    private TicTacToeBoardData _boardData;
    private bool _win;
    private bool _isDraw = false;

    // Called by GameState before the switch
    public void InitializeEnd(TicTacToeBoardData boardData, bool win, bool isDraw = false)
    {
        Debug.Log("Results: InitializeEnd called");
        Debug.Log("Results: received boardData: " + boardData.ToString());
        _boardData = boardData;
        _win = win;
        _isDraw = isDraw;
    }

    public override void EnterState()
    {
        base.EnterState();
        
        Debug.Log("Entering Results state");
        Debug.Log($"Board data is null: {_boardData == null}");
        
        // Make sure we have valid board data
        if (_boardData == null)
        {
            Debug.LogError("Board data is null in Results state! This shouldn't happen.");
            _boardData = new TicTacToeBoardData(); // Create empty board data to avoid crashes
        }
        else
        {
            Debug.Log($"Board data present with values: {string.Join(",", _boardData.board)}");
        }
        
        // Start coroutine to set board data after view is fully initialized
        StartCoroutine(SetBoardDataDelayed());
        
        // Set the result text
        if (_isDraw)
        {
            view.resultText.text = "Game ended in a draw!";
        }
        else if (_win)
        {
            view.resultText.text = "You win! 🎉";
        }
        else
        {
            view.resultText.text = "You lost. Better luck next time!";
        }

        view.returnButton.onClick.AddListener(ReturnLobby);
    }
    
    private System.Collections.IEnumerator SetBoardDataDelayed()
    {
        // Wait for end of frame to ensure view is fully active
        yield return new WaitForEndOfFrame();
        
        // Display the final board state
        if (view != null && view.gameBoard != null && _boardData != null)
        {
            Debug.Log($"Setting board data after delay: {string.Join(",", _boardData.board)}");
            view.gameBoard.SetBoardData(_boardData);
        }
        else
        {
            Debug.LogError($"Something is null - view: {view != null}, gameBoard: {view?.gameBoard != null}, boardData: {_boardData != null}");
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        view.returnButton.onClick.RemoveListener(ReturnLobby);
        
        // Clear the stored board data when leaving
        _boardData = null;
        _win = false;
        _isDraw = false;
    }

    private void ReturnLobby()
    {
        Debug.Log("Returning to lobby...");
        // Send the result to the server whether you win or not
        fsm.channel.SendMessage(new ReturnToLobby { areYaWinninSon = _win });
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }
    
    // Checks if the player is sent to the lobby
    protected override void handleNetworkMessage(ASerializable msg)
    {
        if (msg is RoomJoinedEvent rje && rje.room == RoomJoinedEvent.Room.LOBBY_ROOM)
        {
            Debug.Log("Received RoomJoinedEvent for LOBBY_ROOM, changing state...");
            fsm.ChangeState<LobbyState>();
        }
    }
}
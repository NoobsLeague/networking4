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
        Debug.Log("EndState: received boardData: " + boardData.ToString());
        _boardData = boardData;
        _win = win;
        _isDraw = isDraw;
    }

    public override void EnterState()
    {
        base.EnterState();
        
        Debug.Log("Entering Results state");
        
        // Make sure we have valid board data
        if (_boardData == null)
        {
            Debug.LogWarning("Board data is null in Results state!");
            _boardData = new TicTacToeBoardData(); // Create empty board data to avoid crashes
        }
        
        view.gameBoard.SetBoardData(_boardData);
        
        if (_isDraw)
        {
            view.resultText.text = "Game ended in a draw!";
        }
        else if (_win)
        {
            view.resultText.text = "You win!";
        }
        else
        {
            view.resultText.text = "You lost.";
        }

        view.returnButton.onClick.AddListener(ReturnLobby);
    }

    public override void ExitState()
    {
        base.ExitState();
        view.returnButton.onClick.RemoveListener(ReturnLobby);
    }

    private void ReturnLobby()
    {
        Debug.Log("Returning to lobby...");
        //send the result to the server whether you win or not
        fsm.channel.SendMessage(new ReturnToLobby{areYaWinninSon = _win});
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }
    
    //checks if the player is sent to the lobby
    protected override void handleNetworkMessage(ASerializable msg)
    {
        if (msg is RoomJoinedEvent rje && rje.room == RoomJoinedEvent.Room.LOBBY_ROOM)
        {
            Debug.Log("Received RoomJoinedEvent for LOBBY_ROOM, changing state...");
            fsm.ChangeState<LobbyState>();
        }
    }
}
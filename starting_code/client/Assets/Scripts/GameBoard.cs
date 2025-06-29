using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using shared;



/**
 * Simple checkerbased gameboard that could be reused for other 1 piece based 2 player games.
 * (For multi piece based 2 player games, you'll probably have to make some edits, 
 * but to avoid even more code clutter I didn't go that far).
 */
public class GameBoard : MonoBehaviour, IPointerClickHandler
{
    //sprites for each seperate cellstate, highly dependent on your type of game and boardmodel!
    //in case of tictactoe we have empty, cross and nought.
    [SerializeField] private List<Sprite> _cellStateSprites = new List<Sprite>();

    //All boards cells, stored as images so we can change their sprites
    //These cells are automatically gathered at board start
    private List<Image> _cells = new List<Image>();

    //called when we click on a cell
    public event Action<int> OnCellClicked = delegate { };

    private void Awake()
    {
        //GetComponentsInChildren<Image>() also includes images on the parent #$&#!@&*^!*&^@#
        //So we roll a custom loop to gather child cells.
        foreach (Transform child in transform) _cells.Add(child.GetComponent<Image>());
        Debug.Log(_cells.Count + " cells on the board found.");

        // Log the sprites for debugging
        Debug.Log($"Sprite list count: {_cellStateSprites.Count}");
        for (int i = 0; i < _cellStateSprites.Count; i++)
        {
            Debug.Log($"Sprite {i}: {(_cellStateSprites[i] != null ? _cellStateSprites[i].name : "NULL")}");
        }
    }

    /**
     * Updates the whole board view to reflect the given board data.
     */
    public void SetBoardData(TicTacToeBoardData pBoardData)
    {
        if (pBoardData == null)
        {
            Debug.LogError("SetBoardData: Board data is null!");
            return;
        }

        Debug.Log("Setting board data: " + pBoardData.ToString());

        //pass the whole board to our view
        int[] boardData = pBoardData.board;

        if (boardData == null)
        {
            Debug.LogError("SetBoardData: boardData array is null!");
            return;
        }

        int cellsToSet = Mathf.Min(boardData.Length, _cells.Count);
        Debug.Log($"Updating {cellsToSet} cells");

        for (int i = 0; i < cellsToSet; i++)
        {
            int cellState = boardData[i];

            // Validate cell state
            if (cellState < 0 || cellState >= _cellStateSprites.Count)
            {
                Debug.LogError($"Invalid cell state {cellState} for cell {i}. Sprite list has {_cellStateSprites.Count} sprites.");
                continue;
            }

            if (_cells[i] == null)
            {
                Debug.LogError($"Cell {i} is null!");
                continue;
            }

            if (_cellStateSprites[cellState] == null)
            {
                Debug.LogError($"No sprite for cell state {cellState}!");
                continue;
            }

            // Update the sprite
            _cells[i].sprite = _cellStateSprites[cellState];
            Debug.Log($"Set cell {i} to state {cellState} with sprite {_cellStateSprites[cellState].name}");
        }
    }

    /**
     * Automatically called by the Unity UI system since we have implemented the IPointerClickHandler interface
     */
    public void OnPointerClick(PointerEventData eventData)
    {
        //check whether we clicked on a cell
        int clickedCellIndex = _cells.IndexOf(eventData.pointerPressRaycast.gameObject.GetComponent<Image>());
        Debug.Log("Clicked cell index:" + clickedCellIndex);
        //and if we actually clicked on a cell, trigger our event
        if (clickedCellIndex > -1) OnCellClicked(clickedCellIndex);
    }

}
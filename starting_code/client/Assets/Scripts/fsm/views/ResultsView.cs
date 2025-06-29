using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ResultsView : View
{
    [SerializeField] private GameBoard _gameBoard;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button Return;
    public GameBoard gameBoard => _gameBoard;
    public TMP_Text resultText => _resultText;
    public Button returnButton => Return;
}

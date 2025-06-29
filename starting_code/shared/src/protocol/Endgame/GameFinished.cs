using System;
using System.Collections.Generic;
using System.Text;
using shared;

public class GameFinished : ASerializable
{
    // Board data at the end of the game
    public TicTacToeBoardData boardData;

    // Whether this player won
    public bool YesDadImWinnin;

    // Whether the game ended in a draw
    public bool IsDraw;

    public override void Serialize(Packet p)
    {
        p.Write(boardData);
        p.Write(YesDadImWinnin);
        p.Write(IsDraw);
    }

    public override void Deserialize(Packet p)
    {
        boardData = p.Read<TicTacToeBoardData>();
        YesDadImWinnin = p.ReadBool();
        IsDraw = p.ReadBool();
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using shared;

namespace shared
{
    /**
     * Send from SERVER to all CLIENTS in response to a client's MakeMoveRequest
     */
    public class MakeMoveResult : ASerializable
    {
        public int whoMadeTheMove;
        public TicTacToeBoardData boardData;

        public override void Serialize(Packet pPacket)
        {
            Console.WriteLine($"Serializing MakeMoveResult - whoMadeTheMove: {whoMadeTheMove}");
            pPacket.Write(whoMadeTheMove);
            Console.WriteLine($"Serializing MakeMoveResult - boardData: {boardData}");
            pPacket.Write(boardData);
        }

        public override void Deserialize(Packet pPacket)
        {
            whoMadeTheMove = pPacket.ReadInt();
            Console.WriteLine($"Deserialized MakeMoveResult - whoMadeTheMove: {whoMadeTheMove}");
            boardData = pPacket.Read<TicTacToeBoardData>();
            Console.WriteLine($"Deserialized MakeMoveResult - boardData: {boardData}");
        }
    }
}

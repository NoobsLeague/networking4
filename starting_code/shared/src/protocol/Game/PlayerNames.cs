using System;
using System.Collections.Generic;
using System.Text;

namespace shared
{
    //this shi is to serialize the player names
    public class PlayerNames : ASerializable
    {

        public string player1Name;
        public string player2Name;
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(player1Name);
            pPacket.Write(player2Name);
        }

        public override void Deserialize(Packet pPacket)
        {
            player1Name = pPacket.ReadString();
            player2Name = pPacket.ReadString();
        }
    }
}

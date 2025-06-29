using System;
using System.Collections.Generic;
using System.Text;
namespace shared
{
    //another packetize moment
    public class HeartbeatSend : ASerializable
    {
        public override void Serialize(Packet packet) { }
        public override void Deserialize(Packet packet) { }
    }
}


using System;
using System.Collections.Generic;
using System.Text;
namespace shared
{
   //yeah i think you know whats this for... pain
    public class HeartbeatResponse : ASerializable
    {
        public override void Serialize(Packet packet) { }
        public override void Deserialize(Packet packet) { }
    }
}

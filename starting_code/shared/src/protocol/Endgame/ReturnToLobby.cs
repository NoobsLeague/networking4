using System;
using System.Collections.Generic;
using System.Text;
using shared;

public class ReturnToLobby : ASerializable
{
    public bool areYaWinninSon;

    public override void Serialize(Packet p)
    {
        p.Write(areYaWinninSon);
    }

    public override void Deserialize(Packet p)
    {
        areYaWinninSon = p.ReadBool();
    }
}

using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace FeverstoneWilds.Commands
{
[ProtoContract]
    public class FswCompanionMapPacket
    {
        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public double X;
        [ProtoMember(3)] public double Y;
        [ProtoMember(4)] public double Z;
        [ProtoMember(5)] public int Dimension;
        [ProtoMember(6)] public bool Remove;
        [ProtoMember(7)] public string Name;
        [ProtoMember(8)] public float Yaw;
    }
}

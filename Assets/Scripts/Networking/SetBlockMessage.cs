using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public class SetBlockMessage : MessageBase
{
    public NetworkIdentity client;
    public Vector3Int position;
    public BlockType type;
}

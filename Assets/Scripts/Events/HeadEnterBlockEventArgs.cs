using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class HeadEnterBlockEventArgs : EventArgs
{
    public BlockData block;
    public Vector3Int blockPosition;
}

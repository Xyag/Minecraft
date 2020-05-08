using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CommandTP : Command
{
    public CommandTP() : base("tp"){}
    public override bool execute(World world, Entity user, string[] args)
    {
        if (args.Length != 4)
            return false;
        try
        {
            Vector3 dest = new Vector3(Convert.ToSingle(args[1]), Convert.ToSingle(args[2]), Convert.ToSingle(args[3]));
            user.transform.position = dest;
            Debug.Log(dest);
            return true;
        }
        catch
        {
            return false;
        }
    }

}

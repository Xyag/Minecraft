using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Command
{
    public readonly string commandName;
    public Command(string commandName)
    {
        this.commandName = commandName;
    }
    //args includes command name
    public abstract bool execute(World world, Entity user, string[] args);
}

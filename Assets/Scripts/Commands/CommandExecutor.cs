using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandExecutor
{
    public List<Command> commands = new List<Command>
    {
        new CommandTP(),
    };

    public Entity user;
    public World world;

    public CommandExecutor(Entity user, World world)
    {
        this.user = user;
        this.world = world;
    }

    public bool runCommand(string line)
    {
        string[] args = line.Split(' ');
        if (args.Length < 1)
        {
            return false;
        }
        Command toRun = getCommand(args[0]);
        if (toRun == null)
        {
            return false;
        }
        return toRun.execute(world, user, args);
    }
    public Command getCommand(string name)
    {
        foreach(Command c in commands)
        {
            if (c.commandName == name)
            {
                return c;
            }
        }
        return null;
    }
}

namespace prototype_server.Config
{
    public enum LogTypes
    {
        Error,
        Assert,
        Warning,
        Log,
        Exception
    }
    
    public enum StackTraceTypes
    {
        None,
        ScriptOnly,
        Full
    }
    
    public enum PacketTypes
    {
        Position,
        Positions
    }

    public enum ActionTypes
    {
        Spawn,
        Move
    }

    public enum ObjectTypes
    {
        Player,
        Enemy
    }
}
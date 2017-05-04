namespace LibNetCat
{
    /// <summary>
    ///     Décrit les différentes instructions du protocole.
    /// </summary>
    public enum Instructions : ushort
    {
        Ping,
        BasicMessage,
        FileTransfert,
        Pong,
        Disconnect
    }
}
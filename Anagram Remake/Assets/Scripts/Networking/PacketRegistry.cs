namespace tehelee.networking
{
    public enum PacketRegistry : ushort
    {
        Invalid,
        PacketBundle,
        Heartbeat,
        Letter,
        Word,
        Round,
        ValidWord,
        GetLetter,
        ServerClose,
        SendServerClose,
        CanPlay,
        RecieveClientID,
        GetClientID,
        ShowLetterPlacement,
        GetLetterPlacement,
        Timer,
        Score,
        PlayerReady
    }
}
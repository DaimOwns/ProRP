using System;
namespace Reality.Communication.Outgoing
{
    public static class TradeFinalizedComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.ROOM_TRADE_FINALIZED);
        }
    }
}

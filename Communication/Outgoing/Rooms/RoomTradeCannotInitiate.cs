using System;

namespace Reality.Communication.Outgoing
{
    public static class RoomTradeCannotInitiate
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.ROOM_TRADE_CANNOT_INITIATE);
        }
    }
}

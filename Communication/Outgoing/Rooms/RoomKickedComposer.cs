using System;

namespace Reality.Communication.Outgoing
{
    public static class RoomKickedComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.ROOM_KICKED);
        }
    }
}

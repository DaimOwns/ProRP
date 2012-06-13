using System;

namespace Reality.Communication.Outgoing
{
    public static class RoomOpenFlatComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.ROOM_OPEN_FLAT);
        }
    }
}

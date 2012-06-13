using System;

namespace Reality.Communication.Outgoing
{
    public static class RoomDoorbellNoResponseComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.ROOM_DOORBELL_NO_RESPONSE);
        }
    }
}

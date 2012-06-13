using System;

namespace Reality.Communication.Outgoing
{
    public static class RoomJoinErrorComposer
    {
        public static ServerMessage Compose(int ErrorCode)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.ROOM_JOIN_ERROR);
            Message.AppendInt32(ErrorCode);
            return Message;
        }
    }
}

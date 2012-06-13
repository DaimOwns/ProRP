using System;

namespace Reality.Communication.Outgoing
{
    public static class PingComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.PING);
        }
    }
}

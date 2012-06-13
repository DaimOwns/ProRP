﻿using System;

namespace Reality.Communication.Outgoing
{
    public static class ClientConfigComposer
    {
        public static ServerMessage Compose(int Volume, bool UnknownConfigValue1)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.CLIENT_CONFIG);
            Message.AppendInt32(Volume);
            Message.AppendBoolean(UnknownConfigValue1);
            return Message;
        }
    }
}

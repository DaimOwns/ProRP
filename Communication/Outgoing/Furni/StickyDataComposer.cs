﻿using System;

using Reality.Game.Items;

namespace Reality.Communication.Outgoing
{
    public static class StickyDataComposer
    {
        public static ServerMessage Compose(Item Item)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.STICKY_DATA);
            Message.AppendStringWithBreak(Item.Id.ToString());
            Message.AppendStringWithBreak(Item.Flags);
            return Message;
        }
    }
}

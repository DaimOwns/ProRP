﻿using System;
using System.Collections.Generic;

using Reality.Specialized;
using Reality.Game.Rights;

namespace Reality.Communication.Outgoing
{
    public static class RoomUserBadgesComposer
    {
        public static ServerMessage Compose(uint CharacterId, SortedDictionary<int, Badge> Badges)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.ROOM_USER_BADGES);
            Message.AppendUInt32(CharacterId);
            Message.AppendInt32(Badges.Count);

            foreach (Badge Badge in Badges.Values)
            {
                Message.AppendUInt32(Badge.Id);
                Message.AppendStringWithBreak(Badge.Code);
            }

            return Message;
        }
    }
}

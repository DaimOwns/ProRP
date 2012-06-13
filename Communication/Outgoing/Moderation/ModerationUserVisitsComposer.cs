using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Reality.Util;
using Reality.Game.Moderation;
using Reality.Game.Rooms;

namespace Reality.Communication.Outgoing
{
    public static class ModerationUserVisitsComposer
    {
        public static ServerMessage Compose(uint UserId, ReadOnlyCollection<ModerationRoomVisit> Visits)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.MODERATION_ROOM_VISITS);
            Message.AppendUInt32(UserId);
            Message.AppendStringWithBreak(CharacterResolverCache.GetNameFromUid(UserId));
            Message.AppendInt32(Visits.Count);

            foreach (ModerationRoomVisit Visit in Visits)
            {
                RoomInfo Info = RoomInfoLoader.GetRoomInfo(Visit.RoomId);
                DateTime Time = UnixTimestamp.GetDateTimeFromUnixTimestamp(Visit.TimestampEntered);

                Message.AppendBoolean(Info != null && Info.Type == RoomType.Public);
                Message.AppendUInt32(Info != null ? Info.Id : 0);
                Message.AppendStringWithBreak(Info != null ? Info.Name : "Unknown Room");
                Message.AppendInt32(Time.Hour);
                Message.AppendInt32(Time.Minute);
            }

            return Message;
        }
    }
}

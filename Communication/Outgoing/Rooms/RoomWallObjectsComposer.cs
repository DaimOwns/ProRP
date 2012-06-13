using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Reality.Game.Items;

namespace Reality.Communication.Outgoing
{
    public static class RoomWallObjectsComposer
    {
        public static ServerMessage Compose(ReadOnlyCollection<Item> Items)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.ROOM_WALL_OBJECTS);
            Message.AppendInt32(Items.Count);

            foreach (Item Item in Items)
            {
                RoomItemComposer.SerializeWallItem(Message, Item);
            }

            return Message;
        }
    }
}

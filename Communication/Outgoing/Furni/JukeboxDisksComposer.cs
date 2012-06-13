﻿using System;

using Reality.Game.Sessions;
using System.Collections.Generic;
using Reality.Game.Items;

namespace Reality.Communication.Outgoing
{
    public static class JukeboxDisksComposer
    {
        public static ServerMessage Compose(Session Session)
        {
            List<Item> Disks = Session.InventoryCache.GetSongDisks();

            ServerMessage Message = new ServerMessage(OpcodesOut.JUKEBOX_DISKS);
            Message.AppendInt32(Disks.Count);

            foreach (Item SongDisk in Disks)
            {
                uint SongId = 0;
                uint.TryParse(SongDisk.DisplayFlags, out SongId);

                Message.AppendUInt32(SongDisk.Id);
                Message.AppendUInt32(SongId);
            }
            
            return Message;
        }
    }
}

﻿using System;
using Reality.Game.Items;
using Reality.Game.Items.Wired;
using Reality.Game.Rooms;
using System.Collections.Generic;

namespace Reality.Communication.Outgoing
{
    public static class WiredFurniTriggerComposer
    {
        public static ServerMessage Compose(Item Item, RoomInstance Instance)
        {   // com.sulake.habbo.communication.messages.incoming.userdefinedroomevents.WiredFurniTriggerEvent;
            ServerMessage Message = new ServerMessage(650);
            Message.AppendInt32(0);
            Message.AppendInt32(5);

            if (Item.WiredData.Data1.Contains("|"))
            {
                String[] Selected = Item.WiredData.Data1.Split('|');
                Message.AppendInt32(Selected.Length - 1);
                foreach (String selected in Selected)
                {
                    if (selected == "")
                    {
                        continue;
                    }
                    int result;
                    Int32.TryParse(selected, out result);
                    Message.AppendInt32(result);
                }
            }
            else
            {
                Message.AppendInt32(0);
            }

            Message.AppendUInt32(Item.Definition.SpriteId);
            Message.AppendInt32((int)Item.Id);
            Message.AppendStringWithBreak(Item.WiredData.Data1);
            Message.AppendInt32(1);
            Message.AppendInt32(Item.WiredData.Data2);
            Message.AppendInt32(0);
            Message.AppendInt32(Item.Definition.BehaviorData);
            List<Item> Items = Instance.WiredManager.TriggerRequiresActor(Item.Definition.BehaviorData, Item.RoomPosition.GetVector2());
            Message.AppendInt32(Items.Count); // Contains Event that needs a User, but there is a trigger, that isn't triggered by a User
            foreach (Item Blocked in Items)
            {
                Message.AppendUInt32(Blocked.Definition.SpriteId);
            }
            return Message;
        }
    }
}
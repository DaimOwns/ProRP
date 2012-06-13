using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reality.Communication.Outgoing;
using Reality.Game.Sessions;
using Reality.Communication.Incoming;
using Reality.Communication;
using Reality.Game.Rooms;
using Reality.Storage;
using Reality.Game.Items.Wired;
using Reality.Specialized;

namespace Reality.Game.Items.DefaultBehaviorHandlers
{
    public static class WiredHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredTrigger, new ItemEventHandler(HandleWired));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredEffect, new ItemEventHandler(HandleWired));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredCondition, new ItemEventHandler(HandleWired));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.Switchable, new ItemEventHandler(HandleSwitch));
        }

        private static bool HandleWired(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.Interact:
                    if (!Instance.CheckUserRights(Session))
                    {
                        break;
                    }
                    {
                        switch (Item.Definition.Behavior)
                        {
                            case ItemBehavior.WiredTrigger:
                                Session.SendData(WiredFurniTriggerComposer.Compose(Item, Instance));
                                break;

                            case ItemBehavior.WiredEffect:
                                Session.SendData(WiredFurniActionComposer.Compose(Item, Instance));
                                break;

                            case ItemBehavior.WiredCondition:
                                Session.SendData(WiredFurniConditionComposer.Compose());
                                break;
                        }
                    }
                    break;
                case ItemEventType.Placed:
                case ItemEventType.InstanceLoaded:
                    Item.WiredData = Instance.WiredManager.LoadWired(Item.Id, Item.Definition.BehaviorData);
                    if (WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.periodically)
                    {
                        Item.RequestUpdate(Item.WiredData.Data2);
                    }
                    break;
                case ItemEventType.Removing:
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        Instance.WiredManager.RemoveWired(Item.Id, MySqlClient);
                    }

                    Instance.WiredManager.DeRegisterWalkItem(Item.Id);
                    break;
                case ItemEventType.UpdateTick:
                    if (Item.Definition.Behavior == ItemBehavior.WiredTrigger)
                    {
                        switch (WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData))
                        {
                            case WiredTriggerTypes.periodically:
                                Instance.WiredManager.ExecuteActions(Item, null);
                                Item.RequestUpdate(Item.WiredData.Data2);
                                break;
                            case WiredTriggerTypes.at_given_time:
                                Instance.WiredManager.ExecuteActions(Item, null);
                                break;
                        }
                        return true;
                    }
                    Item.BroadcastStateUpdate(Instance);
                    break;
            }
            return true;
        }

        private static bool HandleSwitch(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            if (Event != ItemEventType.Interact)
            {
                return true;
            }
            RoomActor actor = Instance.GetActor(Session.CharacterId);
            if (actor == null)
            {
                return true;
            }

            foreach (Item item in Instance.GetFloorItems())
            {
                if (item.Definition.Behavior != ItemBehavior.WiredTrigger || WiredTypesUtil.TriggerFromInt(item.Definition.BehaviorData) != WiredTriggerTypes.state_changed)
                {
                    continue;
                }

                String[] Selected = item.WiredData.Data1.Split('|');

                if (Selected.Contains(Item.Id.ToString()))
                {
                    Instance.WiredManager.ExecuteActions(item, actor);
                }
            }
            return true;
        }

    }
}
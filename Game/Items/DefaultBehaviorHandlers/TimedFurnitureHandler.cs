using System;

using Reality.Game.Sessions;
using Reality.Game.Rooms;
using Reality.Communication.Outgoing;

namespace Reality.Game.Items.DefaultBehaviorHandlers
{
    public static class TimedFurnitureHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.TimedAlert, new ItemEventHandler(HandleAlert));
        }

        private static bool HandleAlert(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.InstanceLoaded:
                case ItemEventType.Placed:
                case ItemEventType.Removing:

                    Item.Flags = "0";
                    Item.DisplayFlags = "0";
                    break;

                case ItemEventType.Interact:

                    if (!Instance.CheckUserRights(Session) || Item.Flags == "1")
                    {
                        break;
                    }

                    Item.Flags = "1";
                    Item.DisplayFlags = "1";

                    Item.RequestUpdate(4);
                    Item.BroadcastStateUpdate(Instance);
                    break;

                case ItemEventType.UpdateTick:

                    if (Item.Flags != "1")
                    {
                        break;
                    }

                    Item.Flags = "0";
                    Item.DisplayFlags = "0";

                    Item.BroadcastStateUpdate(Instance);
                    break;
            }

            return true;
        }
    }
}

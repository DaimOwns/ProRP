using System;
using System.Collections.Generic;

using Reality.Game.Sessions;
using Reality.Game.Rooms;
using Reality.Specialized;
using Reality.Storage;
using Reality.Game.Misc;

namespace Reality.Game.Items.DefaultBehaviorHandlers
{
    public static class SwitchHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.Switch, new ItemEventHandler(HandleFixedSwitchSwitch));
        }

        private static bool HandleFixedSwitchSwitch(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.Interact:

                    if (!Instance.CheckUserRights(Session))
                    {
                        break;
                    }

                    List<Vector2> SwitchTiles = Instance.CalculateAffectedTiles(Item, Item.RoomPosition.GetVector2(), Item.RoomRotation);

                    RoomActor Actor = Instance.GetActorByReferenceId(Session.CharacterId);

                    foreach (uint RefId in Item.TemporaryInteractionReferenceIds.Values)
                    {
                        RoomActor InteractingActor = Instance.GetActor(RefId);
                    }

                    int CurrentState = 0;
                    int.TryParse(Item.Flags, out CurrentState);

                    if (CurrentState >= (Item.Definition.BehaviorData - 1))
                    {
                        CurrentState = 0;
                        Item.Flags = CurrentState.ToString();
                        Item.DisplayFlags = CurrentState.ToString();
                    }
                    else
                    {
                        int NewState = CurrentState + 1;
                        Item.Flags = NewState.ToString();
                        Item.DisplayFlags = NewState.ToString();
                    }
                    RoomManager.MarkWriteback(Item, true);

                    Item.BroadcastStateUpdate(Instance);
                    Instance.WiredManager.HandleToggleState(Actor, Item);

                    Instance.RegenerateRelativeHeightmap();
                    break;
            }

            return true;
        }
    }
}

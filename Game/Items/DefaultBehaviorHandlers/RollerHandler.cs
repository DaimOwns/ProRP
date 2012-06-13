﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reality.Game.Sessions;
using Reality.Game.Rooms;
using Reality.Communication.Outgoing;
using Reality.Specialized;
using System.Collections.ObjectModel;

namespace Reality.Game.Items.DefaultBehaviorHandlers
{
    public static class RollerHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.Roller, new ItemEventHandler(HandleRoller));
        }

        private static bool HandleRoller(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.UpdateTick:

                    List<RoomActor> ActorsToMove = Instance.GetActorsOnPosition(Item.RoomPosition.GetVector2());
                    List<Item> ItemsToMove = new List<Item>();
                    List<Item> ItemsToUpdate = new List<Item>();
                    ItemsToMove.AddRange(Instance.GetItemsOnPosition(Item.RoomPosition.GetVector2()));

                    if (ActorsToMove != null)
                    {
                        foreach (RoomActor Actor in ActorsToMove)
                        {
                            if (Actor.IsMoving)
                            {
                                continue;
                            }

                            if (Instance.IsValidStep(Actor.Position.GetVector2(), Item.SquareInFront, true))
                            {
                                Actor.PositionToSet = Item.SquareInFront;
                                Instance.BroadcastMessage(RollerEventComposer.Compose(Actor.Position, new Vector3(
                                    Actor.PositionToSet.X, Actor.PositionToSet.Y,
                                    Instance.GetUserStepHeight(Actor.PositionToSet)), Item.Id, Actor.Id, 0));
                            }
                        }
                    }
                    if (ItemsToMove.Count != 0)
                    {
                        foreach (Item item in ItemsToMove)
                        {
                            if (item != Item)
                            {

                                if (Item.RoomPosition.X == item.RoomPosition.X && Item.RoomPosition.Y == item.RoomPosition.Y)
                                {
                                    Vector3 NewPosition = new Vector3(Item.SquareInFront.X, Item.SquareInFront.Y, Item.RoomPosition.Z);
                                    Vector2 NewPosition1 = new Vector2(Item.SquareInFront.X, Item.SquareInFront.Y);
                                    int NewRotation = item.RoomRotation;
                                    Vector3 FinalizedPosition = Instance.SetRollerFloorItem(Session, item, NewPosition1, NewRotation, Instance);
                                    Vector3 oldpos = item.RoomPosition;

                                    if (FinalizedPosition != null)
                                    {
                                        Instance.BroadcastMessage(RollerEventComposer.Compose(oldpos, FinalizedPosition, Item.Id, 0, item.Id));
                                        ItemEventDispatcher.InvokeItemEventHandler(Session, item, Instance, ItemEventType.Moved, 0);
                                        RoomManager.MarkWriteback(item, false);
                                        item.MoveToRoom(null, Instance.RoomId, FinalizedPosition, NewRotation, string.Empty);
                                    }
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(1000);
                    Instance.RegenerateRelativeHeightmap();

                    goto case ItemEventType.InstanceLoaded;

                case ItemEventType.InstanceLoaded:
                case ItemEventType.Placed:

                    Item.RequestUpdate(18);
                    break;
            }

            return true;
        }
    }
}
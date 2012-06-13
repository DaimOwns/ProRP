using System;
using System.Collections.Generic;
using System.Data;

using Reality.Game.Sessions;
using Reality.Game.Rooms;
using Reality.Specialized;
using Reality.Storage;
using Reality.Game.Misc;
using Reality.Game.Characters;
using Reality.Communication.Outgoing;

namespace Reality.Game.Items.DefaultBehaviorHandlers
{
    public static class GateHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.Gate, new ItemEventHandler(HandleFixedGateSwitch));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.OneWayGate, new ItemEventHandler(HandleOneWayGate));
        }

        private static bool HandleFixedGateSwitch(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.Interact:

                    if (!Instance.CheckUserRights(Session))
                    {
                        return true;
                    }

                    List<Vector2> GateTiles = Instance.CalculateAffectedTiles(Item, Item.RoomPosition.GetVector2(), Item.RoomRotation);

                    foreach (Vector2 Tile in GateTiles)
                    {
                        if (Instance.GetActorsOnPosition(Tile).Count > 0)
                        {
                            return true;
                        }
                    }

                    int CurrentState = 0;
                    int.TryParse(Item.Flags, out CurrentState);

                    Item.Flags = (CurrentState == 0 ? 1 : 0).ToString();
                    Item.DisplayFlags = Item.Flags;

                    RoomManager.MarkWriteback(Item, true);

                    Item.BroadcastStateUpdate(Instance);
                    Instance.RegenerateRelativeHeightmap();
                    break;
            }

            return true;
        }

        private static bool HandleOneWayGate(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.InstanceLoaded:
                case ItemEventType.Moved:
                case ItemEventType.Placed:

                    if (Item.DisplayFlags != "0")
                    {
                        Item.DisplayFlags = "0";
                        Item.BroadcastStateUpdate(Instance);
                    }

                    foreach (uint ActorId in Item.TemporaryInteractionReferenceIds.Values)
                    {
                        RoomActor ActorToUnlock = Instance.GetActor(ActorId);

                        if (ActorToUnlock != null)
                        {
                            ActorToUnlock.UnblockWalking();
                        }
                    }

                    Item.TemporaryInteractionReferenceIds.Clear();
                    break;

                case ItemEventType.Interact:
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                        DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                        DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                        int room_id = (int)Row1["roomid"];
                        bool isPolice = false;
                        if ((string)Row3["type"] == "police" && Session.CharacterInfo.Working == 1)
                        {
                            isPolice = true;
                        }
                        RoomActor Actor = Instance.GetActorByReferenceId(Session.CharacterId);

                    if (Actor == null)
                    {
                        break;
                    }

                    if (Actor.Position.X != Item.SquareInFront.X || Actor.Position.Y != Item.SquareInFront.Y)
                    {
                        Actor.MoveToItemAndInteract(Item, RequestData, Opcode);
                        break;
                    }
                        if (Item.Definition.SpriteId == 2597 && Session.CharacterInfo.Dead == 1 || Session.CharacterInfo.Jailed == 1)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "You are either dead or jailed, You cannot escape!", 0, ChatType.Whisper));
                            return false;
                        }
                        if (!isPolice)
                        {
                            if (Item.Definition.SpriteId == 2600 && Session.CharacterInfo.Working == 0 || Item.Definition.SpriteId == 2600 && room_id == 0 || Item.Definition.SpriteId == 2600 && room_id != Session.CurrentRoomId)
                            {
                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "You either dont work here or you're not working!", 0, ChatType.Whisper));
                                return false;
                            }
                        }

                        if (Item.TemporaryInteractionReferenceIds.Count == 0 && Instance.IsValidStep(Item.RoomPosition.GetVector2(),
                            Item.SquareBehind, true) && Item.DisplayFlags == "0")
                        {
                            Actor.BlockWalking();
                            Actor.MoveTo(Item.RoomPosition.GetVector2(), true, true, true);
                            Item.TemporaryInteractionReferenceIds.Add(1, Actor.Id);
                            Item.RequestUpdate(1);
                        }
                    }

                    break;

                case ItemEventType.UpdateTick:

                    RoomActor UpdateActor = null;

                    if (Item.TemporaryInteractionReferenceIds.ContainsKey(1))
                    {
                        UpdateActor = Instance.GetActor(Item.TemporaryInteractionReferenceIds[1]);
                    }

                    if (UpdateActor == null || !Instance.IsValidStep(Item.RoomPosition.GetVector2(), Item.SquareBehind, true)
                        || ((UpdateActor.Position.X != Item.RoomPosition.X || UpdateActor.Position.Y !=
                        Item.RoomPosition.Y) && (UpdateActor.Position.X != Item.SquareInFront.X ||
                        UpdateActor.Position.Y != Item.SquareInFront.Y)))
                    {
                        if (Item.DisplayFlags != "0")
                        {
                            Item.DisplayFlags = "0";
                            Item.BroadcastStateUpdate(Instance);
                        }

                        if (Item.TemporaryInteractionReferenceIds.Count > 0)
                        {
                            Item.TemporaryInteractionReferenceIds.Clear();
                        }

                        if (UpdateActor != null)
                        {
                            UpdateActor.UnblockWalking();
                        }

                        break;
                    }

                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(Instance);

                    UpdateActor.MoveTo(Item.SquareBehind, true, true, true);

                    Item.RequestUpdate(2);
                    break;
            }

            return true;
        }
    }
}

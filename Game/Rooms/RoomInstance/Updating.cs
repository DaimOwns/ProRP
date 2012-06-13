﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Data;

using Reality.Specialized;
using Reality.Game.Sessions;
using Reality.Communication.Outgoing;
using Reality.Game.Items;
using Reality.Game.Pathfinding;
using Reality.Game.Rooms;
using Reality.Game.Bots;
using Reality.Game.Achievements;
using Reality.Config;
using Reality.Game.Items.Wired;
using Reality.Storage;
using Reality.Game.Misc;

namespace Reality.Game.Rooms
{
    public partial class RoomInstance : IDisposable
    {
        private Timer mUpdater;

        public void PerformUpdate(object state)
        {
            try
            {
                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId); // use this only to debug threadpooling...

                List<RoomActor> ActorsToUpdate = new List<RoomActor>();
                List<RoomActor> ActorsToRemove = new List<RoomActor>();

                // Copy collection
                Dictionary<uint, RoomActor> ActorsCopy = new Dictionary<uint, RoomActor>();
                List<RoomActor>[,] NewUserGrid = new List<RoomActor>[mCachedModel.Heightmap.SizeX, mCachedModel.Heightmap.SizeY];

                lock (mActorSyncRoot)
                {
                    foreach (KeyValuePair<uint, RoomActor> CopyItem in mActors)
                    {
                        ActorsCopy.Add(CopyItem.Key, CopyItem.Value);
                    }
                }

                foreach (RoomActor Actor in ActorsCopy.Values)
                {
                    if (Actor != null)
                    {
                        // If the room is unloaded, allow no actors
                        if (mUnloaded)
                        {
                            ActorsToRemove.Add(Actor);
                            continue;
                        }

                        // The idle time is increased in every step.
                        if (Actor.Type == RoomActorType.UserCharacter)
                        {
                            Actor.IncreaseIdleTime(Actor.ReferenceId == Info.OwnerId);
                        }

                        // If this is a bot, allow the brain to process this update tick.
                        if (Actor.Type == RoomActorType.AiBot)
                        {
                            ((Bot)Actor.ReferenceObject).Brain.PerformUpdate(this);
                        }

                        // Remove any walking statusses (they will be re-applied below if neccessary)
                        if (Actor.UserStatusses.ContainsKey("mv"))
                        {
                            Actor.RemoveStatus("mv");
                            Actor.UpdateNeeded = true;
                        }

                        // Update actor position if neccessary
                        if (Actor.PositionToSet != null)
                        {
                            // Check if the new position is on an arrow
                            foreach (Item Item in GetFloorItems())
                            {
                                if (Item.Definition.SpriteId == 5000)
                                {
                                    if (Actor.PositionToSet.X == Item.RoomPosition.X && Actor.PositionToSet.Y == Item.RoomPosition.Y)
                                    {
                                        Session Session = SessionManager.GetSessionByCharacterId(Actor.ReferenceId);
                                        ItemEventDispatcher.InvokeItemEventHandler(Session, Item, this, ItemEventType.Interact, Actor.MoveToAndInteractData);
                                    }
                                }
                            }
                            // Update the actual position
                            Actor.Position = new Vector3(Actor.PositionToSet.X, Actor.PositionToSet.Y, (Math.Round(GetUserStepHeight(Actor.PositionToSet), 1)));
                            Actor.PositionToSet = null;

                            // Handle any "MoveToAndInteract" events if this was the last step
                            if (!Actor.IsMoving && Actor.MoveToAndInteract > 0 && Actor.Type == RoomActorType.UserCharacter)
                            {
                                Item Item = GetItem(Actor.MoveToAndInteract);

                                if (Item != null)
                                {
                                    ItemEventDispatcher.InvokeItemEventHandler(SessionManager.GetSessionByCharacterId(
                                        Actor.ReferenceId), Item, this, ItemEventType.Interact, Actor.MoveToAndInteractData);
                                }

                                Actor.MoveToAndInteract = 0;
                            }
                        }

                        // If there are more steps to be made, handle it.
                        if (Actor.IsMoving)
                        {
                            // Check if moving to door
                            if (!Actor.IsLeavingRoom && Actor.Pathfinder.Target.X == mCachedModel.DoorPosition.X &&
                                Actor.Pathfinder.Target.Y == mCachedModel.DoorPosition.Y)
                            {
                                Actor.IsLeavingRoom = true;
                            }

                            // Get the next step from the pathfinder
                            Vector2 NextStep = Actor.GetNextStep();

                            // If the pathfinder reports no more steps to be made, this is the last step.
                            bool LastStep = !Actor.IsMoving;

                            // Check that the next step is valid and allowed
                            if (!Actor.Stunned)
                            {
                                if (NextStep != null && ((!Actor.ClippingEnabled && IsValidPosition(NextStep)) ||
                                    IsValidStep(Actor.Position.GetVector2(), NextStep, LastStep, NewUserGrid)))
                                {
                                    // Update "mv" status
                                    Actor.SetStatus("mv", NextStep.X + "," + NextStep.Y + "," + (Math.Round(GetUserStepHeight(NextStep), 1)).ToString().Replace(',', '.'));
                                    Actor.PositionToSet = NextStep;

                                    // Update new/temporary grid with our new move to position
                                    if (NewUserGrid[NextStep.X, NextStep.Y] == null)
                                    {
                                        NewUserGrid[NextStep.X, NextStep.Y] = new List<RoomActor>();
                                    }

                                    NewUserGrid[NextStep.X, NextStep.Y].Add(Actor);

                                    // Remove any "sit" statusses
                                    if (Actor.UserStatusses.ContainsKey("sit"))
                                    {
                                        Actor.RemoveStatus("sit");
                                    }

                                    // Remove any "lay" statusses
                                    if (Actor.UserStatusses.ContainsKey("lay"))
                                    {
                                        Actor.RemoveStatus("lay");
                                    }

                                    // Update rotation
                                    if (Actor.WalkingBackwards)
                                    {
                                        Actor.BodyRotation = Rotation.CalculateBackwards(Actor.Position.GetVector2(), NextStep);
                                    }
                                    else
                                    {
                                        Actor.BodyRotation = Rotation.Calculate(Actor.Position.GetVector2(), NextStep);
                                    }
                                    Actor.HeadRotation = Actor.BodyRotation;

                                    RoomTileEffect Effect = mTileEffects[NextStep.X, NextStep.Y];


                                    uint WiredId = mWiredManager.GetRegisteredWalkItem(mFurniMap[NextStep.X, NextStep.Y]);
                                    Item Wired = GetItem(WiredId);
                                    if (Wired != null && Wired.Definition.Behavior == ItemBehavior.WiredTrigger)
                                    {
                                        if (WiredTypesUtil.TriggerFromInt(Wired.Definition.BehaviorData) == WiredTriggerTypes.walks_on_furni && Actor.FurniOnId != mFurniMap[NextStep.X, NextStep.Y])
                                        {
                                            mWiredManager.ExecuteActions(Wired, Actor);
                                        }
                                    }

                                    WiredId = mWiredManager.GetRegisteredWalkItem(mFurniMap[Actor.Position.X, Actor.Position.Y]);
                                    Wired = GetItem(WiredId);
                                    if (Wired != null && Wired.Definition.Behavior == ItemBehavior.WiredTrigger)
                                    {
                                        if (WiredTypesUtil.TriggerFromInt(Wired.Definition.BehaviorData) == WiredTriggerTypes.walks_off_furni && Actor.FurniOnId != mFurniMap[NextStep.X, NextStep.Y])
                                        {
                                            mWiredManager.ExecuteActions(Wired, Actor);
                                        }
                                    }

                                    Actor.FurniOnId = mFurniMap[NextStep.X, NextStep.Y];

                                    // Request update for next @B cycle
                                    Actor.UpdateNeeded = true;

                                }
                                else
                                {
                                    // Invalid step: tell pathfinder to stop and mark current position on temporary grid
                                    Vector2 target = Actor.Pathfinder.Target;

                                    Actor.StopMoving();
                                    if (NewUserGrid[NextStep.X, NextStep.Y] == null)
                                    {
                                        NewUserGrid[NextStep.X, NextStep.Y] = new List<RoomActor>();
                                    }

                                    NewUserGrid[NextStep.X, NextStep.Y].Add(Actor);
                                    if (ConfigManager.GetValue("pathfinder.mode").ToString().ToLower() == "simple_redirect")
                                    {
                                        Actor.MoveTo(new Vector2(NextStep.X - 1, NextStep.Y)); // Redirect???
                                        while (Actor.IsMoving)
                                        {
                                        }
                                        Actor.MoveTo(target);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (NewUserGrid[Actor.Position.X, Actor.Position.Y] == null)
                            {
                                NewUserGrid[Actor.Position.X, Actor.Position.Y] = new List<RoomActor>();
                            }

                            NewUserGrid[Actor.Position.X, Actor.Position.Y].Add(Actor);
                        }

                        // Update status (apply any sit/lay/effect)
                        UpdateActorStatus(Actor);

                        // Add this actor to the update list if this has been requested
                        if (Actor.UpdateNeeded)
                        {
                            ActorsToUpdate.Add(Actor);
                            Actor.UpdateNeeded = false;
                        }
                    }
                }

                // Remove all actors that need to be removed
                foreach (RoomActor Actor in ActorsToRemove)
                {
                    lock (mActorSyncRoot)
                    {
                        if (ActorsToUpdate.Contains(Actor))
                        {
                            ActorsToUpdate.Remove(Actor);
                        }
                    }

                    switch (Actor.Type)
                    {
                        default:

                            RemoveActorFromRoom(Actor.Id);
                            break;

                        case RoomActorType.UserCharacter:

                            HardKickUser(Actor.ReferenceId);
                            break;
                    }
                }

                // Send update list (if there are any updates) to room
                if (ActorsToUpdate.Count > 0)
                {
                    BroadcastMessage(RoomUserStatusListComposer.Compose(ActorsToUpdate));
                }

                // Update tick on all items --
                List<Item> ItemCopy = null;

                lock (mItemSyncRoot)
                {
                    ItemCopy = mItems.Values.ToList();
                }

                foreach (Item Item in ItemCopy)
                {
                    Item.Update(this);
                }

                // Invalidate door position
                NewUserGrid[mCachedModel.DoorPosition.X, mCachedModel.DoorPosition.Y] = null;

                // Update user grid to our new version
                lock (mActorSyncRoot)
                {
                    mUserGrid = NewUserGrid;
                }

                if (mMusicController != null && mMusicController.IsPlaying)
                {
                    mMusicController.Update(this);
                }
            }
            catch (Exception e)
            {
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                Output.WriteLine("Error in Pathfinder: " + e.Message + "\n\n" + e.StackTrace);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Error in Pathfinder: " + e.Message + " " + e.StackTrace + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");
            }
        }

        public void UpdateActorStatus(RoomActor Actor)
        {
            try
            {
                Vector2 Redirection = mRedirectGrid[Actor.Position.X, Actor.Position.Y];

                if (Redirection != null)
                {
                    Actor.Position = new Vector3(Redirection.X, Redirection.Y, GetUserStepHeight(Redirection));
                }

                RoomTileEffect Effect = mTileEffects[Actor.Position.X, Actor.Position.Y];

                if (Effect == null)
                {
                    return;
                }

                Dictionary<string, string> CurrentStatusses = Actor.UserStatusses;

                if (Effect.Type == RoomTileEffectType.Sit && !CurrentStatusses.ContainsKey("mv"))
                {
                    string OldStatus = (CurrentStatusses.ContainsKey("sit") ? CurrentStatusses["sit"] : string.Empty);
                    string NewStatus = Math.Round(Effect.InteractionHeight, 1).ToString().Replace(',', '.');

                    if (Actor.BodyRotation != Effect.Rotation)
                    {
                        Actor.BodyRotation = Effect.Rotation;
                        Actor.HeadRotation = Effect.Rotation;
                        Actor.UpdateNeeded = true;
                    }

                    if (NewStatus != OldStatus)
                    {
                        Actor.SetStatus("sit", NewStatus);
                        Actor.UpdateNeeded = true;
                    }
                }
                else if (CurrentStatusses.ContainsKey("sit"))
                {
                    Actor.RemoveStatus("sit");
                    Actor.UpdateNeeded = true;
                }

                if (Effect.Type == RoomTileEffectType.Lay && !CurrentStatusses.ContainsKey("mv"))
                {
                    string OldStatus = (CurrentStatusses.ContainsKey("lay") ? CurrentStatusses["lay"] : string.Empty);
                    string NewStatus = Math.Round(Effect.InteractionHeight, 1).ToString().Replace(',', '.');

                    if (Actor.BodyRotation != Effect.Rotation)
                    {
                        Actor.BodyRotation = Effect.Rotation;
                        Actor.HeadRotation = Effect.Rotation;
                        Actor.UpdateNeeded = true;
                    }

                    if (OldStatus != NewStatus)
                    {
                        Actor.SetStatus("lay", NewStatus);
                        Actor.UpdateNeeded = true;
                    }
                }
                else if (CurrentStatusses.ContainsKey("lay"))
                {
                    Actor.RemoveStatus("lay");
                    Actor.UpdateNeeded = true;
                }

                if (Effect.Type == RoomTileEffectType.Effect)
                {
                    if ((Actor.AvatarEffectId != Effect.EffectId || !Actor.AvatarEffectByItem))
                    {
                        Actor.ApplyEffect(Effect.EffectId, true, true);
                    }
                }
                else if (Actor.AvatarEffectByItem)
                {
                    int ClearEffect = 0;

                    if (Actor.Type == RoomActorType.UserCharacter)
                    {
                        Session SessionObject = SessionManager.GetSessionByCharacterId(Actor.ReferenceId);

                        if (SessionObject != null)
                        {
                            ClearEffect = SessionObject.CurrentEffect;
                        }
                    }
                    else
                    {
                        Bot BotObject = (Bot)Actor.ReferenceObject;

                        if (BotObject != null)
                        {
                            ClearEffect = BotObject.Effect;
                        }
                    }

                    Actor.ApplyEffect(ClearEffect, true, true);
                }

                if (Actor.Type == RoomActorType.UserCharacter && Effect.QuestData > 0)
                {
                    Session SessionObject = SessionManager.GetSessionByCharacterId(Actor.ReferenceId);

                    if (SessionObject != null)
                    {
                        QuestManager.ProgressUserQuest(SessionObject, QuestType.EXPLORE_FIND_ITEM, Effect.QuestData);
                    }
                }
            }
            catch (StackOverflowException e)
            {
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                Output.WriteLine("Error in Pathfinder: " + e.Message);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Error in Pathfinder: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");
            }
        }
    }
}
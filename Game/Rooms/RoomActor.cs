﻿using System;
using System.Collections.Generic;

using Reality.Game.Characters;
using Reality.Specialized;
using Reality.Game.Pathfinding;
using Reality.Communication.Outgoing;
using Reality.Communication;
using Reality.Game.Sessions;
using Reality.Game.Misc;
using Reality.Game.Bots;
using Reality.Game.Moderation;
using Reality.Storage;
using Reality.Game.Items;

namespace Reality.Game.Rooms
{
    public enum RoomActorType
    {
        UserCharacter = 1,
        AiBot = 2
    }

    public class RoomActor
    {
        private uint mId;
        private uint mReferenceId;
        private object mReferenceObject;
        private RoomActorType mType;
        private Vector3 mPosition;
        private int mBodyRotation;
        private int mHeadRotation;
        private bool mUpdateNeeded;
        private Dictionary<string, string> mStatusses;
        private Pathfinder mPathfinder;
        private Vector2 mPositionToSet;
        private RoomInstance mInstance;
        private int mDanceId;
        private int mCarryItem;
        private int mAvatarEffect = 0;
        private int mCarryItemTimer;
        private bool mIsLeavingRoom;
        private bool mForcedLeave;
        private int mLeaveStepsTaken;
        private int mIdleTime;
        private bool mIsSleeping;
        private bool mEnableClipping;
        private bool mOverrideClipping;
        private object mMovementSyncRoot;
        private bool mWalkingBlocked;
        private bool mStunned;
        private uint mMoveToAndInteract;
        private int mMoveToAndInteractData;
        private uint mMoveToAndInteractOpcode;
        private bool mAvatarEffectByItem;
        private bool mWalkingBackwards;
        private int mAntiSpamMessagesSent;
        private int mAntiSpamTicks;
        private uint mFurniOnId;

        public uint Id
        {
            get
            {
                return mId;
            }
        }

        public uint FurniOnId
        {
            get
            {
                return mFurniOnId;
            }

            set
            {
                mFurniOnId = value;
            }
        }

        public uint ReferenceId
        {
            get
            {
                return mReferenceId;
            }
        }

        public object ReferenceObject
        {
            get
            {
                return mReferenceObject;
            }
        }

        public RoomActorType Type
        {
            get
            {
                return mType;
            }
        }

        public bool IsBot
        {
            get
            {
                return (mType != RoomActorType.UserCharacter);
            }
        }

        public string Name
        {
            get
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:

                        return ((CharacterInfo)mReferenceObject).Username;

                    case RoomActorType.AiBot:

                        return ((Bot)mReferenceObject).Name;

                    default:

                        return "Unknown User";
                }
            }
        }

        public string Motto
        {
            get
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:

                        return ((CharacterInfo)mReferenceObject).Motto;

                    case RoomActorType.AiBot:

                        return ((Bot)mReferenceObject).Motto;

                    default:

                        return string.Empty;
                }
            }
        }

        public string Figure
        {
            get
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:

                        return ((CharacterInfo)mReferenceObject).Figure;

                    case RoomActorType.AiBot:

                        return ((Bot)mReferenceObject).Look;

                    default:

                        return string.Empty;
                }
            }
        }

        public uint MoveToAndInteractOpcode
        {
            get
            {
                return mMoveToAndInteractOpcode;
            }

            set
            {
                mMoveToAndInteractOpcode = value;
            }
        }

        public bool WalkingBackwards
        {
            get
            {
                return mWalkingBackwards;
            }
            set
            {
                mWalkingBackwards = value;
            }
        }
        public CharacterGender Gender
        {
            get
            {
                 return ((CharacterInfo)mReferenceObject).Gender;
            }
        }

        public string NewFigure
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).NewFigure;
            }
            set
            {
                ((CharacterInfo)mReferenceObject).NewFigure = value;
            }
        }

        public Vector3 Position
        {
            get
            {
                return mPosition;
            }

            set
            {
                mPosition = value;
            }
        }

        public int BodyRotation
        {
            get
            {
                return mBodyRotation;
            }

            set
            {
                mBodyRotation = value;
            }
        }

        public int GroupId
        {
            get
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:

                        return ((CharacterInfo)mReferenceObject).GroupID;

                    case RoomActorType.AiBot:

                        return 1;

                    default:

                        return 0;
                }
            }
        }

        public int Health
        {
            get
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:

                        return ((CharacterInfo)mReferenceObject).Health;

                    case RoomActorType.AiBot:

                        return ((Bot)mReferenceObject).Health;

                    default:

                        return 0;
                }
            }
            set
            {
                switch (mType)
                {
                    case RoomActorType.UserCharacter:
                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ((CharacterInfo)mReferenceObject).Health = value;
                            ((CharacterInfo)mReferenceObject).UpdateHealth(MySqlClient, value);
                        }
                        break;

                    case RoomActorType.AiBot:

                        ((Bot)mReferenceObject).Health = value;
                        ((Bot)mReferenceObject).UpdateHealth(value);
                        break;
                }
            }
        }

        public int Jailed
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).Jailed;
            }
        }

        public int Dead
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).Dead;
            }
        }

        public uint CurrentRoomId
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).HomeRoom;
            }
        }

        public int Working
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).Working;
            }
        }

        public int Score
        {
            get
            {
                return ((CharacterInfo)mReferenceObject).Score;
            }
        }

        public void UpdateWorking(SqlDatabaseClient MySqlClient, int Value)
        {
            ((CharacterInfo)mReferenceObject).UpdateWorking(MySqlClient, Value);
        }

        public void UpdateJailed(SqlDatabaseClient MySqlClient, int Value)
        {
            ((CharacterInfo)mReferenceObject).UpdateJailed(MySqlClient, Value);
        }

        public int HeadRotation
        {
            get
            {
                return mHeadRotation;
            }

            set
            {
                mHeadRotation = value;
            }
        }

        public bool UpdateNeeded
        {
            get
            {
                return mUpdateNeeded;
            }

            set
            {
                mUpdateNeeded = value;
            }
        }

        public Dictionary<string, string> UserStatusses
        {
            get
            {
                lock (mStatusses)
                {
                    Dictionary<string, string> Copy = new Dictionary<string,string>();

                    foreach (KeyValuePair<string, string> Status in mStatusses)
                    {
                        Copy.Add(Status.Key, Status.Value);
                    }

                    return new Dictionary<string, string>(Copy);
                }
            }
        }

        public bool IsMoving
        {
            get
            {
                return !mPathfinder.IsCompleted;
            }
        }

        public Vector2 PositionToSet
        {
            get
            {
                return mPositionToSet;
            }

            set
            {
                mPositionToSet = value;
            }
        }

        public int DanceId
        {
            get
            {
                return mDanceId;
            }
        }

        public int CarryItemId
        {
            get
            {
                return mCarryItem;
            }
        }

        public int AvatarEffectId
        {
            get
            {
                if (mAvatarEffect > 0)
                {
                    return mAvatarEffect;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool IsLeavingRoom
        {
            get
            {
                return mIsLeavingRoom;
            }

            set
            {
                mIsLeavingRoom = true;
            }
        }

        public bool ForcedLeave
        {
            get
            {
                return mForcedLeave;
            }
        }

        public int IdleTime
        {
            get
            {
                return mIdleTime;
            }
        }

        public bool IsSleeping
        {
            get
            {
                return mIsSleeping;
            }
        }

        public int LeaveStepsTaken
        {
            get
            {
                return mLeaveStepsTaken;
            }
        }

        public bool ClippingEnabled
        {
            get
            {
                return mOverrideClipping ? false : mEnableClipping;
            }

            set
            {
                mEnableClipping = value;
            }
        }

        public bool OverrideClipping
        {
            get
            {
                return mOverrideClipping;
            }

            set
            {
                mOverrideClipping = value;
            }
        }

        public uint MoveToAndInteract
        {
            get
            {
                return mMoveToAndInteract;
            }

            set
            {
                mMoveToAndInteract = value;
            }
        }

        public int MoveToAndInteractData
        {
            get
            {
                return mMoveToAndInteractData;
            }

            set
            {
                mMoveToAndInteractData = value;
            }
        }

        public bool AvatarEffectByItem
        {
            get
            {
                return mAvatarEffectByItem;
            }
        }

        public Pathfinder Pathfinder
        {
            get
            {
                return mPathfinder;
            }
        }

        public RoomActor(uint Id, RoomActorType Type, uint ReferenceId, object ReferenceObject, Vector3 Position, int Rotation, RoomInstance Instance)
        {
            mId = Id;
            mType = Type;
            mReferenceId = ReferenceId;
            mReferenceObject = ReferenceObject;
            mPosition = Position;
            mBodyRotation = Rotation;
            mHeadRotation = Rotation;
            mUpdateNeeded = true;
            mStatusses = new Dictionary<string, string>();
            mInstance = Instance;
            mEnableClipping = true;
            mMovementSyncRoot = new object();

            mPathfinder = PathfinderManager.CreatePathfinderInstance();
            mPathfinder.SetRoomInstance(mInstance, Id);

        }

        public static RoomActor TryCreateActor(uint Id, RoomActorType Type, uint ReferenceId, object ReferenceObject, Vector3 Position, int Rotation, RoomInstance Instance)
        {
            if (ReferenceObject == null)
            {
                return null;
            }

            return new RoomActor(Id, Type, ReferenceId, ReferenceObject, Position, Rotation, Instance);
        }

        public void IncreaseIdleTime(bool IsOwner)
        {
            mIdleTime++;

            if (!IsSleeping && mIdleTime >= 600)
            {
                mIsSleeping = true;
                mInstance.BroadcastMessage(RoomUserSleepComposer.Compose(mId, true));
            }

            if (mAntiSpamTicks >= 0)
            {
                mAntiSpamTicks--;

                if (mAntiSpamTicks == -1)
                {
                    mAntiSpamMessagesSent = 0;
                }
            }
        }

        public void Unidle()
        {
            mIdleTime = 0;

            if (mIsSleeping)
            {
                mInstance.BroadcastMessage(RoomUserSleepComposer.Compose(mId, false));
                mIsSleeping = false;
            }
        }

        public bool Stunned
        {
            get
            {
                return mStunned;
            }
            set
            {
                mStunned = value;
            }
        }

        public void BlockWalking()
        {
            StopMoving();
            mWalkingBlocked = true;
        }

        public void UnblockWalking()
        {
            mWalkingBlocked = false;
        }

        public void MoveToItemAndInteract(Item Item, int RequestData, uint Opcode, Vector2 MoveToPosition = null)
        {
            MoveTo(MoveToPosition == null ? Item.SquareInFront : MoveToPosition);

            mMoveToAndInteract = Item.Id;
            mMoveToAndInteractData = RequestData;
            mMoveToAndInteractOpcode = Opcode;
        }

        public void MoveTo(Vector2 ToPosition, bool IgnoreCanInitiate = false, bool IgnoreRedirections = false, bool DisableClipping = false)
        {
            Unidle();

            if (!mInstance.IsValidPosition(ToPosition))
            {
                return;
            }

            mEnableClipping = !DisableClipping;

            if (!ClippingEnabled)
            {
                IgnoreCanInitiate = true;
            }

            if (!IgnoreRedirections)
            {
                ToPosition = mInstance.GetRedirectedTarget(ToPosition);
            }

            if ((ToPosition.X == Position.X && ToPosition.Y == Position.Y) || mForcedLeave ||
                (!IgnoreCanInitiate && !mInstance.CanInitiateMoveToPosition(ToPosition)) ||
                (mWalkingBlocked && !DisableClipping))
            {
                return;
            }

            lock (mMovementSyncRoot)
            {
                mMoveToAndInteract = 0;

                if (mPositionToSet != null)
                {
                    mPosition.X = mPositionToSet.X;
                    mPosition.Y = mPositionToSet.Y;
                    mPosition.Z = mInstance.GetUserStepHeight(new Vector2(mPosition.X, mPosition.Y));

                    mPositionToSet = null;
                }

                mLeaveStepsTaken = 0;
                mIsLeavingRoom = false;

                StopMoving();

                mPathfinder.MoveTo(ToPosition);
            }
        }

        public void MoveToPos(Vector2 ToPosition, bool teleport, bool IgnoreCanInitiate = false, bool IgnoreRedirections = false, bool DisableClipping = false)
        {
            Unidle();

            if ((ToPosition.X == Position.X && ToPosition.Y == Position.Y) || mForcedLeave ||
                (!IgnoreCanInitiate && !mInstance.CanInitiateMoveToPosition(ToPosition)) ||
                (mWalkingBlocked && !DisableClipping))
            {
                return;
            }

            lock (mMovementSyncRoot)
            {
                mMoveToAndInteract = 0;

                if (mPositionToSet != null)
                {
                    mPosition.X = mPositionToSet.X;
                    mPosition.Y = mPositionToSet.Y;
                    mPosition.Z = mInstance.GetUserStepHeight(new Vector2(mPosition.X, mPosition.Y));

                    mPositionToSet = null;
                }

                mLeaveStepsTaken = 0;
                mIsLeavingRoom = false;

                StopMoving();
                Position.X = ToPosition.X;
                Position.Y = ToPosition.Y;
                UpdateNeeded = true;
                if (teleport)
                {
                    foreach (Item Item in mInstance.GetFloorItems())
                    {
                        if (Item.Definition.SpriteId == 5000)
                        {
                            if (this.mPositionToSet.X == Item.RoomPosition.X && this.mPositionToSet.Y == Item.RoomPosition.Y)
                            {
                                Session Session = SessionManager.GetSessionByCharacterId(this.ReferenceId);
                                ItemEventDispatcher.InvokeItemEventHandler(Session, Item, mInstance, ItemEventType.Interact, this.MoveToAndInteractData);
                            }
                        }
                    }
                }

            }
        }

        public Vector2 GetNextStep()
        {
            lock (mMovementSyncRoot)
            {
                if (mType == RoomActorType.UserCharacter && mIsLeavingRoom)
                {
                    mLeaveStepsTaken++;
                }

                return mPathfinder.GetNextStep();
            }
        }

        public void StopMoving()
        {
            lock (mMovementSyncRoot)
            {
                mPathfinder.Clear();
            }
        }

        public void LeaveRoom(bool Forced = false)
        {
            lock (mMovementSyncRoot)
            {
                MoveTo(new Vector2(mInstance.Model.DoorPosition.X, mInstance.Model.DoorPosition.Y));
                mIsLeavingRoom = true;
                mForcedLeave = Forced;
            }
        }

        public void SetStatus(string Key, string Value = "")
        {
            lock (mStatusses)
            {
                if (!mStatusses.ContainsKey(Key))
                {
                    mStatusses.Add(Key, Value);
                    return;
                }

                mStatusses[Key] = Value;
            }
        }

        public void ClearStatusses()
        {
            lock (mStatusses)
            {
                mStatusses.Clear();
            }
        }

        public bool RemoveStatus(string Key)
        {
            lock (mStatusses)
            {
                return mStatusses.Remove(Key);
            }
        }

        public void Wave()
        {
            Unidle();
            mInstance.BroadcastMessage(RoomUserWaveComposer.Compose(Id));
        }

        public void Dance(int DanceId, bool Broadcast = true)
        {
            Unidle();

            if (mDanceId == DanceId)
            {
                return;
            }

            mDanceId = DanceId;

            if (Broadcast)
            {
                mInstance.BroadcastMessage(RoomUserDanceComposer.Compose(Id, DanceId));
            }
        }

        public void CarryItem(int ItemId, bool Broadcast = true)
        {
            Unidle();

            mCarryItem = ItemId;

            if (ItemId > 0)
            {
                mCarryItemTimer = 90;
            }

            if (Broadcast)
            {
                mInstance.BroadcastMessage(RoomUserCarryComposer.Compose(mId, ItemId));
            }
        }

        public void ApplyEffect(int EffectId, bool Broadcast = true, bool ByItem = false)
        {
            Unidle();

            if ((!ByItem && mAvatarEffectByItem) || mAvatarEffect == EffectId)
            {
                return;
            }

            mAvatarEffect = EffectId;
            mAvatarEffectByItem = (ByItem && EffectId > 0);

            if (Broadcast)
            {
                mInstance.BroadcastMessage(RoomUserEffectComposer.Compose(mId, EffectId));
            }
        }

        private void IncrecementAntiSpam(SqlDatabaseClient MySqlClient)
        {
            mAntiSpamMessagesSent++;

            if (mAntiSpamTicks == -1)
            {
                mAntiSpamTicks = 16; // 8 secs for 5 msgs
            }
            else if (mAntiSpamMessagesSent >= 5)
            {
                CharacterInfo CharacterInfo = ((CharacterInfo)mReferenceObject);
                CharacterInfo.Mute(MySqlClient, 30);

                Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterInfo.Id);

                if (TargetSession != null)
                {
                    TargetSession.SendData(RoomMutedComposer.Compose(30));
                }
            }
        }

        public void Chat(string MessageText, bool Shout = false, bool CanOverrideRoomMute = false)
        {
            if (mIsLeavingRoom && mForcedLeave)
            {
                return;
            }

            Unidle();

            if (mInstance.RoomMuted && !CanOverrideRoomMute)
            {
                return;
            }
            if (Shout)
            {
                mInstance.BroadcastChatMessage(this, "[ "+ MessageText + " ]", Shout, ChatEmotions.GetEmotionForText(MessageText));
            }
            else 
            {
                mInstance.BroadcastChatMessage(this, MessageText, Shout, ChatEmotions.GetEmotionForText(MessageText));
            }

            if (mType == RoomActorType.UserCharacter)
            {
                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    ModerationLogs.LogChatMessage(MySqlClient, mReferenceId, mInstance.RoomId, "(" + (Shout ? "OOC" : "IC") + ") " + MessageText);
                    IncrecementAntiSpam(MySqlClient);
                }
            }
        }

        public void Whisper(string MessageText, uint TargetUserId, bool CanOverrideRoomMute = false)
        {
            if (mIsLeavingRoom && mForcedLeave)
            {
                return;
            }

            Unidle();

            if (mInstance.RoomMuted && !CanOverrideRoomMute)
            {
                return;
            }

            ServerMessage Message = RoomChatComposer.Compose(Id, MessageText, 0, ChatType.Whisper);

            if (Type == RoomActorType.UserCharacter)
            {
                Session Session = SessionManager.GetSessionByCharacterId(mReferenceId);

                if (Session != null)
                {
                    Session.SendData(Message);
                }
            }

            string LogTargetName = "Unknown User";

            if (TargetUserId != mReferenceId)
            {
                RoomActor TargetActor = mInstance.GetActorByReferenceId(TargetUserId);

                if (TargetActor == null || TargetActor.Type != RoomActorType.UserCharacter)
                {
                    return;
                }

                Session TargetSession = SessionManager.GetSessionByCharacterId(TargetActor.ReferenceId);

                if (TargetSession != null)
                {
                    if (!TargetSession.IgnoreCache.UserIsIgnored(this.ReferenceId))
                    {
                        TargetSession.SendData(Message);
                    }

                    LogTargetName = TargetSession.CharacterInfo.Username;
                }
            }

            if (mType == RoomActorType.UserCharacter)
            {
                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    ModerationLogs.LogChatMessage(MySqlClient, mReferenceId, mInstance.RoomId, "(Whisper to " + LogTargetName + ") " + MessageText);
                    IncrecementAntiSpam(MySqlClient);
                }
            }
        }

        public void SetTypingState(bool IsNowTyping)
        {
            mInstance.BroadcastMessage(RoomUserTypingComposer.Compoze(Id, IsNowTyping));
        }
    }
}

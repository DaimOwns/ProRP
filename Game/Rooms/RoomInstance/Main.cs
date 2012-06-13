using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using System.Data;

using System.Threading;

using Reality.Specialized;
using Reality.Game.Sessions;
using Reality.Communication;
using Reality.Game.Characters;
using Reality.Communication.Outgoing;
using Reality.Storage;
using Reality.Game.Pathfinding;
using Reality.Game.Bots;
using Reality.Game.Rooms.Events;
using Reality.Game.Items;

using Reality.Game.Pets;
using Reality.Game.Music;
using Reality.Game.Rooms.Trading;
using Reality.Game.Items.Wired;

namespace Reality.Game.Rooms
{

    public partial class RoomInstance : IDisposable
    {
        private uint mInstanceId;
        private RoomInfo mInfo;
        private bool mUnloaded;
        private double mUnloadedTimestamp;
        private int mMarkedEmptyRoom;

        public uint InstanceId
        {
            get
            {
                return mInstanceId;
            }
        }

        public RoomInfo Info
        {
            get
            {
                return mInfo;
            }
        }

        public uint RoomId
        {
            get
            {
                return mInfo.Id;
            }
        }

        public bool Unloaded
        {
            get
            {
                return mUnloaded;
            }
        }

        public double TimeUnloaded
        {
            get
            {
                return (Unloaded ? UnixTimestamp.GetCurrent() - mUnloadedTimestamp : 0);
            }
        }

        public int MarkedAsEmpty
        {
            get
            {
                return mMarkedEmptyRoom;
            }

            set
            {
                mMarkedEmptyRoom = value;
            }
        }

        public List<string> SearchableTags
        {
            get
            {
                List<string> Tags = new List<string>();
                Tags.AddRange(Info.Tags);

                if (HasOngoingEvent)
                {
                    Tags.AddRange(Event.Tags);
                }

                return Tags;
            }
        }

        public RoomInstance(uint InstanceId, RoomInfo Info, RoomModel Model)
        {
            mActorSyncRoot = new object();
            mItemSyncRoot = new object();

            mInstanceId = InstanceId;
            mInfo = Info;
            mActors = new Dictionary<uint, RoomActor>();
            mCachedModel = Model;
            mRelativeHeightmap = string.Empty;
            mActorIdGenerator = 1;
            mActorIdGeneratorSyncLock = new object();
            mTileStates = new TileState[mCachedModel.Heightmap.SizeX, mCachedModel.Heightmap.SizeY];
            mUsersWithRights = new List<uint>();
            mBannedUsers = new Dictionary<uint, double>();
            mItems = new Dictionary<uint, Item>();
            mStaticObjects = new List<StaticObject>();
            mItemLimitCache = new Dictionary<ItemBehavior, int>();
            mUserGrid = new List<RoomActor>[mCachedModel.Heightmap.SizeX, mCachedModel.Heightmap.SizeY];
            mMusicController = new RoomMusicController();
            mTemporaryStickieRights = new Dictionary<uint, uint>();
            mTradeManager = new TradeManager();
            mRollerItems = new List<Item>[mCachedModel.Heightmap.SizeX, mCachedModel.Heightmap.SizeY];
            mWiredManager = new WiredManager(this);

            foreach (Bot Bot in BotManager.GenerateBotInstancesForRoom(RoomId))
            {
                AddBotToRoom(Bot);
            }

            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                // Items
                MySqlClient.SetParameter("id", RoomId);
                DataTable ItemTable = MySqlClient.ExecuteQueryTable("SELECT * FROM items WHERE room_id = @id");

                foreach (DataRow Row in ItemTable.Rows)
                {
                    Item Item = ItemFactory.CreateFromDatabaseRow(Row, mWiredManager);

                    if (Item.PendingExpiration && Item.ExpireTimeLeft <= 0)
                    {
                        Item.RemovePermanently(MySqlClient);
                        continue;
                    }

                    if (Item.InSoundManager)
                    {
                        continue;
                    }

                    mItems.Add(Item.Id, Item);
                    IncrecementFurniLimitCache(Item.Definition.Behavior);

                    ItemEventDispatcher.InvokeItemEventHandler(null, Item, this, ItemEventType.InstanceLoaded);
                }

                // Static objects
                MySqlClient.SetParameter("id", RoomId);
                DataTable StaticObjectTable = MySqlClient.ExecuteQueryTable("SELECT id,name,position,height,rotation,is_seat FROM static_objects WHERE room_id = @id");

                foreach (DataRow Row in StaticObjectTable.Rows)
                {
                    mStaticObjects.Add(new StaticObject((uint)Row["id"], (string)Row["name"], Vector2.FromString((string)Row["position"]),
                        (int)Row["height"], (int)Row["rotation"], (Row["is_seat"].ToString() == "1")));
                }

                // Rights
                MySqlClient.SetParameter("id", RoomId);
                DataTable RightsTable = MySqlClient.ExecuteQueryTable("SELECT user_id FROM room_rights WHERE room_id = @id");

                foreach (DataRow Row in RightsTable.Rows)
                {
                    mUsersWithRights.Add((uint)Row["user_id"]);
                }

                // Pets
                MySqlClient.SetParameter("id", RoomId);
                DataTable PetsTable = MySqlClient.ExecuteQueryTable("SELECT * FROM pets WHERE room_id = @id");

                foreach (DataRow Row in PetsTable.Rows)
                {
                    Pet PetData = PetFactory.GetPetFromDatabaseRow(Row);

                    AddBotToRoom(BotManager.CreateNewInstance(BotManager.GetHandlerDefinitionForPetType(PetData.Type),
                        RoomId, Vector3.FromString(Row["room_pos"].ToString()), PetData));
                }
            }

            RegenerateRelativeHeightmap();

            mUpdater = new Timer(new TimerCallback(PerformUpdate), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
        }

        public static RoomInstance TryCreateRoomInstance(uint InstanceId, uint RoomId)
        {
            RoomInfo Info = RoomInfoLoader.GetRoomInfo(RoomId);

            if (Info == null)
            {
                return null;
            }

            RoomModel Model = Info.TryGetModel();

            if (Model == null)
            {
                return null;
            }

            return new RoomInstance(InstanceId, Info, Model);
        }

        public void Unload()
        {
            if (mUnloaded)
            {
                return;
            }

            mUnloaded = true;

            if (mActorCountSyncNeeded)
            {
                DoActorCountSync();
            }

            BroadcastMessage(RoomKickedComposer.Compose());

            lock (mActorSyncRoot)
            {
                mActors.Clear();
            }

            mUnloadedTimestamp = UnixTimestamp.GetCurrent();
        }

        public void Dispose()
        {
            if (!mUnloaded)
            {
                Unload();
            }

            mUpdater.Dispose();
            mUpdater = null;
            mInfo = null;
        }
    }
}
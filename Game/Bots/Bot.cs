using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Reality.Specialized;
using Reality.Game.Bots.Behavior;
using Reality.Game.Pets;
using Reality.Storage;
using Reality.Game.Rooms;

namespace Reality.Game.Bots
{
    public enum BotWalkMode
    {
        STAND = 0,
        FREEROAM = 1,
        SPECIFIED_RANGE = 2
    }

    public class Bot
    {
        private uint mId;
        private uint mDefinitionId;
        private string mBehaviorType;
        private string mName;
        private string mLook;
        private string mMotto;
        private uint mRoomId;
        private Vector3 mInitialPosition;
        private Vector2 mServePosition;
        private List<Vector2> mDefinedPositions;
        private BotWalkMode mWalkMode;
        private bool mKickable;
        private int mRotation;
        private List<BotResponse> mResponses;
        private int mEffect;
        private int mHealth;
        private IBotBehavior mBrain;
        private int mResponseDistance;
        private Pet mPetData;
        private string mPositionNow;

        public uint Id
        {
            get
            {
                return mId;
            }
        }

        public uint DefinitionId
        {
            get
            {
                return mDefinitionId;
            }
        }

        public string BehaviorType
        {
            get
            {
                return mBehaviorType;
            }
        }

        public string Name
        {
            get
            {
                return (IsPet ? mPetData.Name : mName);
            }
        }

        public string Look
        {
            get
            {
                return (IsPet ? mPetData.Look : mLook);
            }
        }

        public string Motto
        {
            get
            {
                return (IsPet ? string.Empty : mMotto); 
            }
        }

        public uint RoomId
        {
            get
            {
                return mRoomId;
            }
        }

        public int GroupId
        {
            get
            {
                return 0;
            }
        }

        public int Health
        {
            get
            {
                return mHealth;
            }
            set
            {
                mHealth = value;
            }
        }

        public Vector3 InitialPosition
        {
            get
            {
                return mInitialPosition;
            }
        }

        public Vector2 ServePosition
        {
            get
            {
                return mServePosition;
            }
        }

        public string CurrentPos
        {
            get
            {
                return mPositionNow;
            }
            set
            {
                mPositionNow = value;
            }
        }

        public void UpdateHealth(int Amount)
        {
            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                mHealth = Amount;
                MySqlClient.SetParameter("userid", mId);
                MySqlClient.SetParameter("amount", mHealth);
                MySqlClient.ExecuteNonQuery("UPDATE bots SET health = @amount WHERE id = @userid LIMIT 1");
                BotManager.Initialize(MySqlClient);
            }
        }

        public void UpdatePosition(string Amount)
        {
            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                mPositionNow = Amount;
                MySqlClient.SetParameter("userid", Id);
                MySqlClient.SetParameter("amount", Amount);
                MySqlClient.ExecuteNonQuery("UPDATE bots SET pos_now = @amount WHERE id = @userid LIMIT 1");
                BotManager.Initialize(MySqlClient);
            }
        }

        public ReadOnlyCollection<Vector2> PredefinedPositions
        {
            get
            {
                List<Vector2> Copy = new List<Vector2>();
                Copy.AddRange(mDefinedPositions);
                return Copy.AsReadOnly();
            }
        }

        public BotWalkMode WalkMode
        {
            get
            {
                return mWalkMode;
            }
        }

        public bool Kickable
        {
            get
            {
                return mKickable;
            }
        }

        public int Rotation
        {
            get
            {
                return mRotation;
            }
        }

        public int Effect
        {
            get
            {
                return mEffect;
            }
        }

        public IBotBehavior Brain
        {
            get
            {
                return mBrain;
            }
        }

        public int ResponseDistance
        {
            get
            {
                return mResponseDistance;
            }
        }

        public bool IsPet
        {
            get
            {
                return mPetData != null;
            }
        }

        public Pet PetData
        {
            get
            {
                return mPetData;
            }
        }

        public List<BotResponse> Responses
        {
            get
            {
                return mResponses;
            }
        }

        public Bot(uint Id, uint DefId, string BehaviorType, string Name, string Look, string Motto, uint RoomId, Vector3 Position,
            Vector2 ServePosition, List<Vector2> DefinedPositions, BotWalkMode WalkMode, bool Kickable, int Rotation,
            List<BotResponse> Responses, int Effect, int ResponseDistance, int Health, string PositionNow, Pet PetData = null)
        {
            mId = Id;
            mDefinitionId = DefId;
            mBehaviorType = BehaviorType;
            mName = Name;
            mLook = Look;
            mMotto = Motto;
            mRoomId = RoomId;
            mInitialPosition = Position;
            mPositionNow = PositionNow;
            mServePosition = ServePosition;
            mDefinedPositions = DefinedPositions;
            mWalkMode = WalkMode;
            mKickable = Kickable;
            mRotation = Rotation;
            mEffect = Effect;
            mResponses = Responses;
            mResponseDistance = ResponseDistance;
            mPetData = PetData;
            mHealth = Health;

            switch (mBehaviorType.ToLower())
            {
                case "pet":

                    mBrain = new PetBot();
                    break;

                default:

                    mBrain = new GenericBot();
                    break;
            }
        }

        public BotResponse GetResponseForMessage(string Text)
        {
            foreach (BotResponse Response in mResponses)
            {
                if (Response.MatchesTrigger(Text))
                {
                    return Response;
                }
            }

            return null;
        }
    }
}

using System;

using Reality.Specialized;

namespace Reality.Game.Items
{
    public class StaticObject
    {
        private uint mId;
        private string mName;
        private Vector2 mPosition;
        private int mHeight;
        private int mRotation;
        private bool mIsSeat;

        public uint Id
        {
            get
            {
                return mId;
            }
        }

        public string Name
        {
            get
            {
                return mName;
            }
        }

        public Vector2 Position
        {
            get
            {
                return mPosition;
            }
        }

        public int Height
        {
            get
            {
                return mHeight;
            }
        }

        public int Rotation
        {
            get
            {
                return mRotation;
            }
        }

        public bool IsSeat
        {
            get
            {
                return mIsSeat;
            }
        }

        public StaticObject(uint Id, string Name, Vector2 Position, int Height, int Rotation, bool IsSeat)
        {
            mId = Id;
            mName = Name;
            mPosition = Position;
            mHeight = Height;
            mRotation = Rotation;
            mIsSeat = IsSeat;
        }
    }
}
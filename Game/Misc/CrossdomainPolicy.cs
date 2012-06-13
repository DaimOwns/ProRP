using System;
using System.IO;
using System.Text;
using Reality.Config;

namespace Reality.Game.Misc
{
    public static class CrossdomainPolicy
    {
        private static string mPolicyText;

        public static string PolicyText
        {
            get
            {
                return mPolicyText;
            }
        }

        public static void Initialize(string Path)
        {
            if (!File.Exists(Path))
            {
                throw new ArgumentException("Crossdomain policy file not found at: " + Path + ".");
            }

            mPolicyText = File.ReadAllText(Path);
        }

        public static byte[] GetBytes()
        {
            return GetBytes(Constants.DefaultEncoding);
        }

        public static byte[] GetBytes(Encoding Encoding)
        {
            return Encoding.GetBytes(mPolicyText);
        }
    }
}

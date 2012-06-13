﻿using System;
using System.Collections.Generic;
using System.Data;

using Reality.Storage;
using System.Threading;

namespace Reality.Game.Moderation
{
    public static class ModerationBanManager
    {
        private static List<uint> mCharacterBlacklist;
        private static List<string> mRemoteAddressBlacklist;
        private static Thread mWorkerThread;
        private static object mSyncRoot;

        public static void Initialize(SqlDatabaseClient MySqlClient)
        {
            mCharacterBlacklist = new List<uint>();
            mRemoteAddressBlacklist = new List<string>();
            mSyncRoot = new object();

            mWorkerThread = new Thread(new ThreadStart(ProcessThread));
            mWorkerThread.Name = "ModerationBanManager";
            mWorkerThread.Priority = ThreadPriority.Lowest;
            mWorkerThread.Start();

            ReloadCache(MySqlClient);
        }

        public static void ProcessThread()
        {
            try
            {
                while (Program.Alive)
                {
                    Thread.Sleep(600000);

                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        ReloadCache(MySqlClient);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
        }

        public static void ReloadCache(SqlDatabaseClient MySqlClient)
        {
            lock (mSyncRoot)
            {
                mCharacterBlacklist.Clear();
                mRemoteAddressBlacklist.Clear();

                MySqlClient.SetParameter("timestamp", UnixTimestamp.GetCurrent());
                DataTable Table = MySqlClient.ExecuteQueryTable("SELECT * FROM bans WHERE timestamp_expire > @timestamp");

                foreach (DataRow Row in Table.Rows)
                {
                    uint UserId = (uint)Row["user_id"];
                    string RemoteAddr = (string)Row["remote_address"];

                    if (UserId > 0 && !mCharacterBlacklist.Contains(UserId))
                    {
                        mCharacterBlacklist.Add(UserId);
                    }

                    if (RemoteAddr.Length > 0 && !mRemoteAddressBlacklist.Contains(RemoteAddr))
                    {
                        mRemoteAddressBlacklist.Add(RemoteAddr);
                    }
                }
            }
        }

        public static bool IsRemoteAddressBlacklisted(string RemoteAddressString)
        {
            lock (mSyncRoot)
            {
                return mRemoteAddressBlacklist.Contains(RemoteAddressString);
            }
        }

        public static bool IsUserIdBlacklisted(uint UserId)
        {
            lock (mSyncRoot)
            {
                return mCharacterBlacklist.Contains(UserId);
            }
        }

        public static void BanUser(SqlDatabaseClient MySqlClient, uint UserId, string MessageText, uint ModeratorId, double Length)
        {
            MySqlClient.SetParameter("userid", UserId);
            MySqlClient.SetParameter("reason", MessageText);
            MySqlClient.SetParameter("timestamp", UnixTimestamp.GetCurrent());
            MySqlClient.SetParameter("timestampex", UnixTimestamp.GetCurrent() + Length);
            MySqlClient.SetParameter("moderator", ModeratorId);
            MySqlClient.ExecuteNonQuery("INSERT INTO bans (user_id,reason_text,timestamp_created,timestamp_expire,moderator_id) VALUES (@userid,@reason,@timestamp,@timestampex,@moderator)");

            lock (mSyncRoot)
            {
                mCharacterBlacklist.Add(UserId);
            }
        }
    }
}

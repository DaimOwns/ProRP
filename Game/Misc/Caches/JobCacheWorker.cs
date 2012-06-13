using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Reality.Storage;
using Reality.Game.Sessions;
using Reality.Specialized;
using Reality.Game.Rooms;
using Reality.Communication.Outgoing;
using System.Data;

namespace Reality.Game.Misc
{
    public static class JobCacheWorker
    {
        private static Thread jWorkerThread;

        public static void Initialize()
        {
            jWorkerThread = new Thread(new ThreadStart(ProcessThread));
            jWorkerThread.Priority = ThreadPriority.Highest;
            jWorkerThread.Name = "JobCacheWorker";
            jWorkerThread.Start();
        }

        public static void CheckEffectExpiry(Session Session)
        {
            try
            {
                if (Session.CharacterInfo.Timer > 0)
                {
                    int time = Session.CharacterInfo.Timer - 1;
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        Session.CharacterInfo.UpdateTimer(MySqlClient, -1);
                        RoomInstance Instance = RoomManager.GetInstanceByRoomId(Session.CurrentRoomId);
                        RoomActor Actor = (Instance == null ? null : Instance.GetActorByReferenceId(Session.CharacterId));

                        if (Session.CharacterInfo.Timer == 20 || Session.CharacterInfo.Timer == 40 || Session.CharacterInfo.Timer == 60 || Session.CharacterInfo.Timer == 80 || Session.CharacterInfo.Timer == 100 || Session.CharacterInfo.Timer == 120 || Session.CharacterInfo.Timer == 140 || Session.CharacterInfo.Timer == 160 || Session.CharacterInfo.Timer == 180 || Session.CharacterInfo.Timer == 200 || Session.CharacterInfo.Timer == 220 || Session.CharacterInfo.Timer == 240 || Session.CharacterInfo.Timer == 260 || Session.CharacterInfo.Timer == 280 || Session.CharacterInfo.Timer == 320 || Session.CharacterInfo.Timer == 340 || Session.CharacterInfo.Timer == 360 || Session.CharacterInfo.Timer == 380 || Session.CharacterInfo.Timer == 400 || Session.CharacterInfo.Timer == 420 || Session.CharacterInfo.Timer == 440 || Session.CharacterInfo.Timer == 460 || Session.CharacterInfo.Timer == 480 || Session.CharacterInfo.Timer == 500 || Session.CharacterInfo.Timer == 520 || Session.CharacterInfo.Timer == 540 || Session.CharacterInfo.Timer == 560 || Session.CharacterInfo.Timer == 580 || Session.CharacterInfo.Timer == 600 || Session.CharacterInfo.Timer == 620 || Session.CharacterInfo.Timer == 640 || Session.CharacterInfo.Timer == 660 || Session.CharacterInfo.Timer == 680 || Session.CharacterInfo.Timer == 700 || Session.CharacterInfo.Timer == 720 || Session.CharacterInfo.Timer == 740 || Session.CharacterInfo.Timer == 760 || Session.CharacterInfo.Timer == 780 || Session.CharacterInfo.Timer == 800 || Session.CharacterInfo.Timer == 820 || Session.CharacterInfo.Timer == 840 || Session.CharacterInfo.Timer == 860 || Session.CharacterInfo.Timer == 880 || Session.CharacterInfo.Timer == 900 || Session.CharacterInfo.Timer == 920 || Session.CharacterInfo.Timer == 940 || Session.CharacterInfo.Timer == 960 || Session.CharacterInfo.Timer == 980)
                        {
                            if (Session.CharacterInfo.Jailed == 1)
                            {
                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left in jail!", 0, ChatType.Whisper));
                            }
                            else if (Session.CharacterInfo.Dead == 1)
                            {
                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left in hospital!", 0, ChatType.Whisper));
                            }
                            else if (Session.CharacterInfo.Working == 1)
                            {
                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left untill your pay!", 0, ChatType.Whisper));
                            }
                        }
                        if (Session.CharacterInfo.Timer <= 1)
                        {
                            if (Session.CharacterInfo.Jailed == 1)
                            {
                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Finishes their time in jail*", 0, ChatType.Shout));
                                Session.CharacterInfo.UpdateJailed(MySqlClient, 0);
                                Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, Session.CharacterInfo.Figure, Session.CharacterInfo.Gender, Session.CharacterInfo.Motto, Session.CharacterInfo.Score));
                            }
                            if (Session.CharacterInfo.Dead == 1)
                            {
                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Regains their consciousness*", 0, ChatType.Shout));
                                Session.CharacterInfo.UpdateDead(MySqlClient, 0);
                                Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, Session.CharacterInfo.Figure, Session.CharacterInfo.Gender, Session.CharacterInfo.Motto, Session.CharacterInfo.Score));
                            }
                            if (Session.CharacterInfo.Working == 1)
                            {
                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                int pay = (int)Row3["pay"];
                                int paytime = (int)Row3["paytime"];
                                int newbal = Session.CharacterInfo.CreditsBalance + pay;
                                Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, +pay);
                                Session.SendData(CreditsBalanceComposer.Compose(newbal));
                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Gets their payslip*", 0, ChatType.Shout));
                                Session.CharacterInfo.UpdateTimer(MySqlClient, paytime);
                            }
                        }
                        if (Session.CharacterInfo.Dead == 1 || Session.CharacterInfo.Jailed == 1)
                        {
                            Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                Output.WriteLine("Error in JobCacheWorker thread: " + e.Message);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Error in JobCacheWorker thread: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");

                file.Close();
            }
        }

        private static void ProcessThread()
        {
            try
            {
                while (Program.Alive)
                {
                    Thread.Sleep(1000);
                    Dictionary<uint, Session> Sessions = SessionManager.Sessions;

                    foreach (Session Session in Sessions.Values)
                    {
                        if (Session.Stopped || !Session.Authenticated || Session == null)
                        {
                            continue;
                        }
                        
                        CheckEffectExpiry(Session);
                    }
                    
                    ProcessThread();
                }
            }
            catch (ThreadAbortException e) 
            {
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                Output.WriteLine("Error in JobCacheWorker thread: " + e.Message);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Error in JobCacheWorker thread: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");

                file.Close();
            }
            catch (ThreadInterruptedException e) 
            {
                Output.WriteLine("Error in JobCacheWorker thread: " + e.Message);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine("Error in JobCacheWorker thread: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");

                file.Close();
            }
        }

        public static void HandleExpiration(Session Session)
        {
            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
            }
        }
    }
}

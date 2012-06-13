using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using Reality.Specialized;
using Reality.Game.Sessions;
using Reality.Communication.Outgoing;
using Reality.Game.Infobus;
using Reality.Game.Rooms;
using Reality.Game.Bots;
using Reality.Game.Items;
using Reality.Storage;
using Reality.Game.Achievements;
using Reality.Game.Catalog;
using Reality.Game.Characters;
using Reality.Util;
using System.Text;
using Reality.Game.Moderation;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Reality.Game.Misc
{
    public static class ChatCommands
    {
                public static string FigToUniFig(string _Figure)
                {
                    string _Uni;
                    string FigurePartHair = _Figure;
                    string GetHairPart;

                    GetHairPart = Regex.Split(_Figure, "hr")[1];
                    FigurePartHair = GetHairPart.Split('.')[0];
                    string FigurePartBody = _Figure;
                    string GetBodyPart;

                    GetBodyPart = Regex.Split(_Figure, "hd")[1];
                    FigurePartBody = GetBodyPart.Split('.')[0];

                    _Uni = Convert.ToString("hr" + FigurePartHair + "." + "hd" + FigurePartBody + ".");

                    return _Uni;
                }

                static ulong GetTotalMemoryInBytes()
                {
                    return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
                }
                public static bool HandleCommand(Session Session, string Input)
                {
                        Input = Input.Substring(1, Input.Length - 1);
                        string[] Bits = Input.Split(' ');

                        RoomInstance Instance = RoomManager.GetInstanceByRoomId(Session.CurrentRoomId);
                        RoomActor Actor = (Instance == null ? null : Instance.GetActorByReferenceId(Session.CharacterId));
                        try
                        {
                        switch (Bits[0].ToLower())
                        {
                            #region Staff Only Commands (16)
                            #region :update_catalog
                            case "update_catalog":
                            case "update_catalogue":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        CatalogManager.Initialize(MySqlClient);
                                    }
                                    return true;
                                }
                            #endregion
                            #region :update_bots
                            case "update_bots":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        BotManager.Initialize(MySqlClient);
                                    }
                                    return true;
                                }
                            #endregion
                            #region :update_items
                            case "update_items":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ItemDefinitionManager.Initialize(MySqlClient);
                                    }
                                    return true;
                                }
                            #endregion
                            #region :update_rooms
                            case "update_rooms":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        RoomManager.Initialize(MySqlClient);
                                    }
                                    return true;
                                }
                            #endregion
                            #region :summon x
                            case "summon":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }

                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :summon <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));

                                    if (TargetSession == null || TargetSession.CurrentRoomId == Session.CurrentRoomId)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username, 0, ChatType.Whisper));
                                        return true;
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        RoomHandler.PrepareRoom(TargetSession, Session.CurrentRoomId, null, true);
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Summoned user from server (chat command)",
                                           "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                                    }
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Summons " + Username + " to this room*", 0, ChatType.Shout));
                                    return true;
                                }
                            #endregion
                            #region :kick x
                            case "kick":
                                {
                                    if (!Session.HasRight("moderation_tool"))
                                    {
                                        return false;
                                    }

                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :kick <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));

                                    if (TargetSession == null || TargetSession.HasRight("moderation_tool") || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline, not in a room, or cannot be kicked.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    RoomManager.RemoveUserFromRoom(TargetSession, true);
                                    TargetSession.SendData(NotificationMessageComposer.Compose("You have been kicked from the room by a community staff member."));

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Kicked user from room (chat command)",
                                            "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                                    }

                                    return true;
                                }
                            #endregion
                            #region :superkick x
                            case "superkick":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }

                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :kick <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));


                                    if (TargetSession == null || TargetSession.HasRight("moderation_tool") || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline or cannot be kicked.", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    RoomInstance Instance1 = RoomManager.GetInstanceByRoomId(TargetSession.CurrentRoomId);
                                    Instance1.BroadcastMessage(RoomChatComposer.Compose(TargetSession.CharacterInfo.Id, "-- Logging out in 10 seconds! --", 0, ChatType.Shout));
                                    System.Threading.Thread.Sleep(10000);
                                    SessionManager.StopSession(TargetActor.Id);

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Superkicked user from server (chat command)",
                                           "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                                    }

                                    return true;
                                }
                            #endregion
                            #region :roommute
                            case "roommute":
                                {
                                    if (!Session.HasRight("mute"))
                                    {
                                        return false;
                                    }

                                    if (!Instance.RoomMuted)
                                    {
                                        Instance.RoomMuted = true;
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "The current room has been muted successfully.", 0, ChatType.Whisper));
                                    }
                                    else
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "This room is already muted.", 0, ChatType.Whisper));
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Muted room", "Room '"
                                            + Instance.Info.Name + "' (ID " + Instance.RoomId + ")");
                                    }

                                    return true;
                                }
                            #endregion
                            #region :roomunmute
                            case "roomunmute":
                                {
                                    if (!Session.HasRight("mute"))
                                    {
                                        return false;
                                    }

                                    if (Instance.RoomMuted)
                                    {
                                        Instance.RoomMuted = false;
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "The current room has been unmuted successfully.", 0, ChatType.Whisper));
                                    }
                                    else
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "This room is not muted.", 0, ChatType.Whisper));
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Unmuted room", "Room '"
                                            + Instance.Info.Name + "' (ID " + Instance.RoomId + ")");
                                    }

                                    return true;
                                }
                            #endregion
                            #region :mute x
                            case "mute":
                                {
                                    if (!Session.HasRight("mute"))
                                    {
                                        return false;
                                    }

                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :mute <username> [length in seconds]", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    int TimeToMute = 0;

                                    if (Bits.Length >= 3)
                                    {
                                        int.TryParse(Bits[2], out TimeToMute);
                                    }

                                    if (TimeToMute <= 0)
                                    {
                                        TimeToMute = 300;
                                    }

                                    if (TimeToMute > 3600)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "The maximum mute time is one hour.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));

                                    if (TargetSession == null || TargetSession.HasRight("mute"))
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' does not exist, is not online, or cannot be muted.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        TargetSession.CharacterInfo.Mute(MySqlClient, TimeToMute);
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Muted user",
                                            "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ") for " + TimeToMute + " seconds.");
                                    }

                                    TargetSession.SendData(RoomMutedComposer.Compose(TimeToMute));
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "User '" + Username + "' has been muted successfully for " + TimeToMute + " seconds.", 0, ChatType.Whisper));
                                    return true;
                                }
                            #endregion
                            #region :unmute x
                            case "unmute":
                                {
                                    if (!Session.HasRight("mute"))
                                    {
                                        return false;
                                    }

                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :unmute <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());

                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    Username = TargetSession.CharacterInfo.Username;
                                    if (TargetSession == null)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' does not exist or is not online.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    if (!TargetSession.CharacterInfo.IsMuted)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is not muted.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        TargetSession.CharacterInfo.Unmute(MySqlClient);
                                    }

                                    TargetSession.SendData(NotificationMessageComposer.Compose("You have been unmuted. Please reload the room."));
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' was successfully unmuted.", 0, ChatType.Whisper));

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Unmuted user",
                                            "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                                    }

                                    return true;
                                }
                            #endregion
                            #region :clipping/:override
                            case "clipping":
                            case "override":

                                if (!Session.HasRight("hotel_admin"))
                                {
                                    return false;
                                }

                                Actor.OverrideClipping = !Actor.OverrideClipping;
                                Actor.ApplyEffect(Actor.ClippingEnabled ? 0 : 23);
                                Session.CurrentEffect = 0;
                                return true;
                            #endregion
                            #region :coords
                            case "coords":

                                if (!Session.HasRight("hotel_admin"))
                                {
                                    return false;
                                }

                                Session.SendData(NotificationMessageComposer.Compose("Position: " + Actor.Position.ToString() + ", Rotation: " + Actor.BodyRotation));
                                return true;
                            #endregion
                            #region :restart
                            case "restart":

                                if (!Session.HasRight("hotel_admin"))
                                {
                                    return false;
                                }
                                string reason = UserInputFilter.FilterString(Bits[1]);
                                SessionManager.BroadcastPacket(NotificationMessageComposer.Compose("The server is restarting for an update! \nPlease log back in when you logout.\n\nUpdates don't take long at RealityRP.\n\nReason for Update:\n" + reason));
                                System.Threading.Thread.Sleep(30000);
                                Process.Start(Environment.CurrentDirectory + "\\RealityEMU.exe", "\"delay 4000\"");
                                Program.Stop();
                                return true;
                            #endregion
                            #region :emptyinv
                            case "emptyinv":

                                if (!Session.HasRight("hotel_admin"))
                                {
                                    return false;
                                }

                                Session.InventoryCache.ClearAndDeleteAll();
                                Session.SendData(InventoryRefreshComposer.Compose());
                                Session.SendData(NotificationMessageComposer.Compose("Your inventory has been emptied."));
                                return true;
                            #endregion
                            #region :kill x
                            case "kill":
                                {
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    #region Error - Syntax Error
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :hit <username>", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    #endregion
                                    #region Sessions
                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                    Username = TargetSession.CharacterInfo.Username;
                                    #endregion
                                    #region Error - Cant Find User
                                    if (TargetSession == null || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    #endregion
                                    #region Hitself
                                    if (Username == Actor.Name)
                                    {
                                        Random _r = new Random();
                                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                        {
                                            int HitPoint = _r.Next(100, 101);
                                            TargetSession.CharacterInfo.UpdateHealth(MySqlClient, -HitPoint);
                                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Hit user from server (chat command)",
                                               "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");

                                            if (TargetSession.CharacterInfo.Health < 1)
                                            {
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Sends a bolt of lightning to " + Username + ", causing them to set fire and burn*", 0, ChatType.Shout));
                                                bool wired_done = Instance.WiredManager.HandleDeath(Target);
                                                if (!wired_done)
                                                {
                                                    RoomHandler.PrepareRoom(TargetSession, 2, null, true);
                                                    TargetSession.CharacterInfo.SetHomeRoom(MySqlClient, 2);
                                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Searches " + Username + "'s body and finds 0 coins.*", 0, ChatType.Shout));
                                                    TargetSession.CharacterInfo.UpdateDead(MySqlClient, 1);
                                                    TargetSession.CharacterInfo.Timer = 200;
                                                }
                                                TargetSession.CharacterInfo.Heal(MySqlClient);
                                            }
                                            else
                                            {
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hits " + Username + ", causing " + HitPoint + " damage!*", 0, ChatType.Shout));
                                            }
                                        }
                                        return true;
                                    }
                                    #endregion
                                    #region Hit
                                    Random _s = new Random();
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        int HitPoint = _s.Next(100, 101);
                                        TargetSession.CharacterInfo.UpdateHealth(MySqlClient, -HitPoint);
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Hit user from server (chat command)",
                                           "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");

                                        if (TargetSession.CharacterInfo.Health < 1)
                                        {
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Sends a bolt of lightning to " + Username + ", causing them to set fire and burn*", 0, ChatType.Shout));
                                            bool wired_done = Instance.WiredManager.HandleDeath(Target);
                                            if (!wired_done)
                                            {
                                                RoomHandler.PrepareRoom(TargetSession, 2, null, true);
                                                TargetSession.CharacterInfo.SetHomeRoom(MySqlClient, 2);
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Searches " + Username + "'s body and finds 0 coins.*", 0, ChatType.Shout));
                                                TargetSession.CharacterInfo.UpdateDead(MySqlClient, 1);
                                                TargetSession.CharacterInfo.Timer = 200;
                                            }
                                            TargetSession.CharacterInfo.Heal(MySqlClient);
                                        }
                                        else
                                        {
                                            RoomHandler.PrepareRoom(TargetSession, 2, null, true);
                                            TargetSession.CharacterInfo.SetHomeRoom(MySqlClient, 2);
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Sends a bolt of lightning to " + Username + ", causing them to set fire and burn*", 0, ChatType.Shout));
                                            TargetSession.CharacterInfo.Heal(MySqlClient);
                                            TargetSession.CharacterInfo.UpdateDead(MySqlClient, 1);
                                        }
                                    }
                                    #endregion
                                    return true;
                                }
                            #endregion
                            #region :ha <msg>
                            case ":ha":
                                if (!Session.HasRight("hotel_admin"))
                                {
                                    return false;
                                }
                                string Msg = Input.Replace(Bits[0].ToLower(), "");
                                        SessionManager.BroadcastPacket(UserAlertModernComposer.Compose("Important Message", Msg));
                                    break;
                            #endregion
                            #region :superhire x <jobid> <rankid>
                            case "superhire":
                                    if (!Session.HasRight("hotel_admin"))
                                    {
                                        return false;
                                    }
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                            if (Bits.Length < 4)
                                            {
                                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :superhire <username> <jobid> <rankid>", 0, ChatType.Whisper));
                                                return true;
                                            }

                                            string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                            string JobID = UserInputFilter.FilterString(Bits[2].Trim());
                                            string RankID = UserInputFilter.FilterString(Bits[3].Trim());
                                            Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                            if (TargetSession == null || !TargetSession.InRoom)
                                            {
                                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                                return true;
                                            }
                                                Username = TargetSession.CharacterInfo.Username;
                                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + JobID + "'");
                                                DataRow Row4 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + JobID + "' AND rankid = '" + RankID + "'");
                                                string jobname = (string)Row1["name"] + " " + (string)Row4["name"];
                                                MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                                MySqlClient.SetParameter("rankid", RankID);
                                                MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = @rankid WHERE id = @userid LIMIT 1");
                                                MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                                MySqlClient.SetParameter("groupid", JobID);
                                                MySqlClient.ExecuteNonQuery("UPDATE characters SET groupid = @groupid WHERE id = @userid LIMIT 1");
                                                TargetSession.CharacterInfo.UpdateGroupID(MySqlClient, int.Parse(JobID));
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Uses their God-Like powers to hire " + Username + " as " + jobname + "*", 0, ChatType.Shout));
                                                TargetSession.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                                RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                                Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                                return true;
                                        }
                                    break;
                            #endregion
                            #endregion

                            #region Everyone's Commands (15)
                            #region :commands
                            case "commands":
                                {
                                    Session.SendData(MessageOfTheDayComposer.Compose(":update_catalog - Updates the catalogue in-game from the database. Staff Command\n:update_bots - Updates the bots in-game from the database. Staff Command\n:update_items - Updates the items in-game from the database. Staff Command\n:update_rooms - Updates the rooms in-game from the database. Staff Command\n:summon <user> - Summons a user to the room you're in. Staff Command\n:kick <user> - Kicks a user out the room. Staff Command\n:superkick <user> - Kicks a user from the room and bans them from the room. Staff Command\n:roommute - Mutes the room, Allowing only admins in the room to speak. Staff Command\n:roomunmute - Un-Mutes the room, Allowing everyone in the room to speak. Staff Command\n:mute <user> [<length/seconds>] - Mutes a specific user (You can add a length of the mute in). Staff Command\n:unmute <user> - Un-Mutes a specific user, Allowing them to speak. Staff Command\n:override/:clipping - Allows you to walk anywhere and over any object. Staff Command\n:coords - Shows your current coordinates. Staff Command\n:restart - Restarts the server (30 second delay to let people finish off). Staff Command\n:emptyinv - Emptys YOUR inventory. Staff Command\n:kill <user> - Kills a user. Staff Command\n:commands - Shows the available commands in the server.\n:online - Shows all the users online in 1 list.\n:timeleft - Shows how long you have left in jail or hospital.\n:hit <user> - Hits a user. You can knock out a user when their HP goes below 1.\n:roomid - Shows you the Room ID.\n:stats - Shows your HP, Timeleft, Job and Job Rank.\n:give <user> <amount> - Gives a user money out of your pocket.\n:startwork - Makes you start working.\n:stopwork - Makes you stop working.\n:deposit <amount> - Deposits money into your bank account.\n:balance - Shows your bank balance to you.\n:about - Shows info about the server.\n:pickall - Picks up all the furniture in the room. Room Owner Only\n:drive - Makes you get into a car until you say :drive again. VIP Command\n:moonwalk - Makes you walk backwards untill you say :moonwalk again. VIP Command\n:heal <user> - Heals a user, Setting their Health Points to 100%. Hospital Command\n:arrest <user> <amount> - Arrests a user for the set time (amount). The user must be stunned first. Police Command\n:stun <user> - Stuns a user, Stopping them from moving. Police Command\n:release <user> - Releases a user from jail or hospital. Police Command\n:withdraw <user> <amount> - Withdraws money from a users account and gives it to them. Bank Command\n:fire <user> - Fires a user from their job. Manager only command.\n:hire <user> - Hires a user to your corporation if they dont have a job. Manager only command.\n:promote <user> - Promotes a user 1 rank higher. Manager only command.\n:demote <user> - Demotes a user 1 rank lower. Manager only command.\n:sendhome <user> [time] - Disallows the selected user from working for the set time. Manager and Supervisor only command. (Time is optional, Default is 900 (15 minutes))\n:taxi <roomid> - Calls a taxi to take you to the room id specified.\n:quitjob - Quits your job.\n"));
                                    return true;
                                }
                            #endregion
                            #region :online
                            case "online":
                                {
                                    List<string> OnlineUsers = SessionManager.ConnectedUserData.Values.ToList();
                                    StringBuilder MessageText = new StringBuilder("There are currently " + OnlineUsers.Count + " user(s) online:\n");

                                    foreach (string OnlineUser in OnlineUsers)
                                    {
                                        MessageText.Append('\n');
                                        MessageText.Append("- " + OnlineUser);
                                    }

                                    Session.SendData(NotificationMessageComposer.Compose(MessageText.ToString()));
                                    return true;
                                }
                            #endregion
                            #region :timeleft
                            case "timeleft":
                                {
                                    if (Session.CharacterInfo.Dead == 1)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left in hospital!", 0, ChatType.Whisper));
                                    }
                                    if (Session.CharacterInfo.Jailed == 1)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left in jail!", 0, ChatType.Whisper));
                                    }
                                    if (Session.CharacterInfo.Jailed == 0 && Session.CharacterInfo.Dead == 0 && Session.CharacterInfo.Working == 0)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "You currently have no timers running", 0, ChatType.Whisper));
                                    }
                                    if (Session.CharacterInfo.Working == 1)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Timer + " seconds left untill your pay!", 0, ChatType.Whisper));
                                    }
                                    return true;
                                }
                            #endregion
                            #region :hit x
                            case "hit":
                                {
                                    #region Error - Syntax Error
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :hit <username>", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    #endregion
                                    #region Sessions

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = null;
                                    RoomActor Target = null;
                                    Bot TheBot = null;
                                    bool isBot = false;
                                    Vector2 PositionNow = new Vector2(0, 0);

                                    foreach (RoomActor RoomActor1 in Instance.Actors)
                                    {
                                        if (RoomActor1.Type != RoomActorType.AiBot)
                                        {
                                            if (RoomActor1.Name.ToLower() == Username.ToLower())
                                            {
                                                Username = RoomActor1.Name;
                                                PositionNow = RoomActor1.Position.GetVector2();
                                                TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                                Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                                isBot = false;
                                            }
                                        }
                                        else if (RoomActor1.Type == RoomActorType.AiBot)
                                        {
                                            if (RoomActor1.Name.ToLower() == Username.ToLower())
                                            {
                                                TheBot = BotManager.GetBotDefinition(RoomActor1.Id);
                                                Username = RoomActor1.Name;
                                                PositionNow = RoomActor1.Position.GetVector2();
                                                Target = RoomActor1;
                                                isBot = true;
                                            }
                                        }
                                    }
                                    #endregion
                                    #region Error - Cant Find User
                                    if (isBot == false)
                                    {
                                        if (TargetSession == null || !TargetSession.InRoom)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                            return true;
                                        }
                                    }
                                    #endregion
                                    #region Missed
                                    if (Distance.Calculate(Actor.Position.GetVector2(), PositionNow) > 1)
                                    {
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Swings for " + Username + ", but misses!*", 0, ChatType.Shout));
                                        return true;
                                    }
                                    #endregion
                                    #region Hit Bot
                                    if (isBot == true)
                                    {
                                        Random _ss = new Random();
                                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                        {
                                            int HitPoint = _ss.Next(1, 10);
                                            TheBot.UpdateHealth(TheBot.Health - HitPoint);

                                            if (TheBot.Health < 1)
                                            {
                                                // Kill the bot
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hits " + Username + ", causing " + HitPoint + " damage!*", 0, ChatType.Shout));
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*" + Username + " gets knocked out due to the hit!", 0, ChatType.Shout));
                                                if (TheBot.Motto.ToLower().Contains("[boss]"))
                                                {
                                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Searches " + Username + "'s body and finds 120 coins.*", 0, ChatType.Shout));
                                                    Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, +120);
                                                    Session.SendData(CreditsBalanceComposer.Compose(Session.CharacterInfo.CreditsBalance));
                                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Wins the match of mafia wars.* [+120 points]", 0, ChatType.Shout));
                                                    Session.CharacterInfo.UpdateScore(MySqlClient, +120);
                                                    Session.SendData(AchievementScoreUpdateComposer.Compose(Session.CharacterInfo.Score));
                                                    if (TheBot.Name.ToLower().Contains("green") || TheBot.Name.ToLower().Contains("blue") || TheBot.Name.ToLower().Contains("yellow") || TheBot.Name.ToLower().Contains("red"))
                                                    {
                                                        foreach (RoomActor GameActor in Instance.Actors)
                                                        {
                                                            if (!GameActor.IsBot && GameActor.Figure.ToLower().Contains("sh-725-70.ch-635-70.lg-700-70") || GameActor.Figure.ToLower().Contains("sh-290-70.ch-215-70.lg-275-70") || GameActor.Figure.ToLower().Contains("sh-290-85.ch-215-85.lg-275-85") || GameActor.Figure.ToLower().Contains("sh-725-85.ch-635-85.lg-700-85") || GameActor.Figure.ToLower().Contains("sh-290-82.ch-215-82.lg-275-82") || GameActor.Figure.ToLower().Contains("sh-725-82.ch-635-82.lg-700-82") || GameActor.Figure.ToLower().Contains("sh-290-1321.ch-215-1321.lg-275-1321") || GameActor.Figure.ToLower().Contains("sh-725-1321.ch-635-1321.lg-700-1321"))
                                                            {
                                                                Session GameSession = SessionManager.GetSessionByCharacterId(GameActor.ReferenceId);
                                                                Instance.WiredManager.HandleDeath(GameActor);
                                                                GameActor.MoveToPos(new Vector2(24, 9), false);
                                                                GameSession.CharacterInfo.Heal(MySqlClient);
                                                            }
                                                            if (GameActor.IsBot)
                                                            {
                                                                Bot TheBot1 = BotManager.GetBotDefinition(GameActor.Id);
                                                                TheBot1.UpdateHealth(100);
                                                            }
                                                        }
                                                    }
                                                    TheBot.UpdateHealth(100);
                                                    return true;
                                                }
                                            }
                                            else
                                            {
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hits " + Username + ", causing " + HitPoint + " damage!* [+10 points]", 0, ChatType.Shout));
                                                Session.CharacterInfo.UpdateScore(MySqlClient, +10);
                                                Session.SendData(AchievementScoreUpdateComposer.Compose(Session.CharacterInfo.Score));
                                                return true;
                                            }
                                        }
                                    }
                                    #endregion
                                    #region Hit User
                                    Random _s = new Random();
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        int HitPoint = _s.Next(1, 10);
                                        Target.Health = (Target.Health - HitPoint);
                                        ModerationLogs.LogModerationAction(MySqlClient, Session, "Hit user from server (chat command)",
                                           "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");

                                        if (TargetSession.CharacterInfo.Health < 1)
                                        {
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hits " + Username + ", causing " + HitPoint + " damage!*", 0, ChatType.Shout));
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*" + Username + " gets knocked out due to the hit! [+10 points]*", 0, ChatType.Shout));
                                            Session.CharacterInfo.UpdateScore(MySqlClient, +10);
                                            Session.SendData(AchievementScoreUpdateComposer.Compose(Session.CharacterInfo.Score));
                                            bool wired_done = Instance.WiredManager.HandleDeath(Target);
                                            if (!wired_done)
                                            {
                                                RoomHandler.PrepareRoom(TargetSession, 2, null, true);
                                                TargetSession.CharacterInfo.SetHomeRoom(MySqlClient, 2);
                                                int reward = (TargetSession.CharacterInfo.CreditsBalance % (_s.Next(1, 10)));
                                                decimal rewards = decimal.Parse(reward.ToString());
                                                rewards = Math.Round(rewards, 0);
                                                Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Searches " + Username + "'s body and finds " + rewards + " coins.*", 0, ChatType.Shout));
                                                reward = int.Parse(rewards.ToString());
                                                Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, +reward);
                                                Session.SendData(CreditsBalanceComposer.Compose(Session.CharacterInfo.CreditsBalance));
                                                TargetSession.CharacterInfo.UpdateDead(MySqlClient, 1);
                                                TargetSession.CharacterInfo.Timer = 200;
                                            }
                                            TargetSession.CharacterInfo.Heal(MySqlClient);
                                        }
                                        else
                                        {
                                            int reward = (TargetSession.CharacterInfo.Score % (_s.Next(1, 10)));
                                            decimal rewards = decimal.Parse(reward.ToString());
                                            rewards = Math.Round(rewards, 0);
                                            reward = int.Parse(rewards.ToString());
                                            string more = "";
                                            if (reward > 1)
                                            {
                                                more = "s";
                                            }
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hits " + Username + ", causing " + HitPoint + " damage! [+" + reward + " point" + more + "]*", 0, ChatType.Shout));
                                            Session.CharacterInfo.UpdateScore(MySqlClient, +reward);
                                            Session.SendData(AchievementScoreUpdateComposer.Compose(Session.CharacterInfo.Score));
                                        }
                                    }
                                    #endregion
                                    return true;
                                }
                            #endregion
                            #region :roomid
                            case "roomid":
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "The current room ID is " + Instance.RoomId, 0, ChatType.Whisper));
                                    return true;
                                }
                            #endregion
                            #region :stats
                            case "stats":
                                {
                                    Session.SendData(NotificationMessageComposer.Compose(Session.CharacterInfo.Username + "\r---------------\nHealth: " + Session.CharacterInfo.Health));
                                    return true;
                                }
                            #endregion
                            #region :give x <amount>
                            case "give":
                                {
                                    if (Bits.Length < 3)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :give <username> <amount>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    int amount = Int32.Parse(UserInputFilter.FilterString(Bits[2].Trim()));
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                    RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                    Username = TargetSession.CharacterInfo.Username;

                                    if (TargetSession == null || !TargetSession.InRoom || Session.AbsoluteRoomId != TargetSession.AbsoluteRoomId)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room.", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    if (amount < 1 || amount > Session.CharacterInfo.CreditsBalance)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Error - You dont have enough coins to give away!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    Username = TargetSession.CharacterInfo.Username;
                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        int user1 = Session.CharacterInfo.CreditsBalance - amount;
                                        Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, -amount);
                                        Session.SendData(CreditsBalanceComposer.Compose(user1));

                                        int user2 = TargetSession.CharacterInfo.CreditsBalance + amount;
                                        TargetSession.CharacterInfo.UpdateCreditsBalance(MySqlClient, user2);
                                        TargetSession.SendData(CreditsBalanceComposer.Compose(user2));
                                    }
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hands " + amount + " coins to " + Username + "*", 0, ChatType.Shout));
                                }
                                return true;
                            #endregion
                            #region :startwork
                            case "startwork":
                                if (Session.CharacterInfo.SentHome == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You cant work, Your manager/supervisor has sent you home!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Working == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You're already working!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Dead == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You cant work, You're dead!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Jailed == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You cant work, You're jailed!", 0, ChatType.Whisper));
                                    return true;
                                }
                                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                {
                                    MySqlClient.SetParameter("groupid", Session.CharacterInfo.GroupID);
                                    DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = @groupid");
                                    MySqlClient.SetParameter("userid", Session.CharacterInfo.Id);
                                    DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = @userid");
                                    MySqlClient.SetParameter("groupid", Session.CharacterInfo.GroupID);
                                    MySqlClient.SetParameter("rankid", Row2["jobrank"]);
                                    DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = @groupid AND rankid = @rankid");
                                    int room_id = (int)Row1["roomid"];
                                    string jobname = (string)Row1["name"] + " " + (string)Row3["name"];
                                    string jobfig = (string)Row3["figure_data_" + Session.CharacterInfo.Gender.ToString()];
                                    int paytime = (int)Row3["paytime"];

                                    if (room_id == Instance.RoomId || room_id == 0)
                                    {
                                        string figure_update = FigToUniFig(Session.CharacterInfo.Figure) + jobfig;
                                        if (jobfig == "")
                                        {
                                            figure_update = Session.CharacterInfo.Figure;
                                        }
                                        if (Session.CharacterInfo.Timer < 19 || Session.CharacterInfo.Timer > paytime)
                                        {
                                            Session.CharacterInfo.UpdateTimer(MySqlClient, paytime);
                                        }
                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, figure_update, Session.CharacterInfo.Gender, "[WORKING] " + jobname, Session.CharacterInfo.Score));
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Starts to work*", 0, ChatType.Shout));
                                        Session.CharacterInfo.UpdateWorking(MySqlClient, 1);
                                    }
                                }
                                return true;
                            #endregion
                            #region :stopwork
                            case "stopwork":
                                if (Session.CharacterInfo.Working != 1)
                                {
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "You cant stop work, You're are not working!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Dead == 1)
                                {
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "You cant stop work, You're are dead!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Jailed == 1)
                                {
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "You cant stop work, You're are in jail!", 0, ChatType.Whisper));
                                    return true;
                                }
                                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                {
                                    Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, Actor.Figure, Session.CharacterInfo.Gender, Session.CharacterInfo.Motto, Session.CharacterInfo.Score));
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Stops working*", 0, ChatType.Shout));
                                    Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                }
                                return true;
                            #endregion
                            #region :deposit <amount>
                            case "deposit":
                                {
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :deposit <amount>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    int amount = Int32.Parse(UserInputFilter.FilterString(Bits[1].Trim()));

                                    if (amount <= 0)
                                    {
                                        return false;
                                    }

                                    if (amount > Session.CharacterInfo.CreditsBalance)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Error - You dont have " + amount + " coins!", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                    {
                                        int newamount = Session.CharacterInfo.CreditsBalance - amount;
                                        int newbank = Session.CharacterInfo.Bank + amount;
                                        Session.CharacterInfo.UpdateBank(MySqlClient, newbank);
                                        Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, newamount);
                                        Session.SendData(CreditsBalanceComposer.Compose(newamount));
                                    }

                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Deposits " + amount + " coins into their bank account*", 0, ChatType.Shout));
                                    return true;
                                }
                            #endregion
                            #region :balance
                            case "balance":
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have " + Session.CharacterInfo.Bank + " credits in your bank!", 0, ChatType.Whisper));
                                    return true;
                                }
                            #endregion
                            #region :about
                            case "about":
                                Session.SendData(NotificationMessageComposer.Compose("This hotel is proudly powered by Reality, the premium RP Habbo Hotel emulator.\n\nOur system is currently running on " + GetTotalMemoryInBytes() + "bytes of RAM", "http://archie-zone.co.uk/reality"));
                                return true;
                            #endregion
                            #region :pickall
                            case "pickall":

                                if (!Instance.CheckUserRights(Session, true))
                                {
                                    Session.SendData(NotificationMessageComposer.Compose("You do not have rights to pickall in this room."));
                                    return true;
                                }

                                Instance.PickAllToUserInventory(Session);
                                return true;
                            #endregion
                            #region :taxi <roomid>
                            case "taxi":
                                if (Bits.Length < 2)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :taxi <room id>", 0, ChatType.Whisper));
                                    return true;
                                }
                                uint roomid = uint.Parse(UserInputFilter.FilterString(Bits[1].Trim().ToString()));

                                if (roomid == Session.CurrentRoomId)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You're already in that room!", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Jailed == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You cannot taxi whilst jailed.", 0, ChatType.Whisper));
                                    return true;
                                }
                                if (Session.CharacterInfo.Dead == 1)
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "You cannot taxi whilst dead.", 0, ChatType.Whisper));
                                    return true;
                                }

                                bool loaded = false;
                                if (!RoomManager.InstanceIsLoadedForRoom(roomid))
                                {
                                    loaded = RoomManager.TryLoadRoomInstance(roomid);
                                }
                                else
                                {
                                    loaded = true;
                                }
                                if (loaded)
                                {
                                    RoomInstance GoingTo = RoomManager.GetInstanceByRoomId(roomid);
                                    if (Session.CharacterInfo.Working == 1 && Session.CharacterInfo.GroupID == 3)
                                    {
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hops in squad car and drives to " + GoingTo.Info.Name + " [" + roomid + "]*", 0, ChatType.Shout));
                                        HandleTaxi(Session, Actor, roomid, true);
                                        return true;
                                    }
                                    else
                                    {
                                        if (Session.CharacterInfo.CreditsBalance < 10)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Sorry, You're gonna have to walk. You dont have enough coins!", 0, ChatType.Whisper));
                                            return true;
                                        }
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Calls a taxi to " + GoingTo.Info.Name + " [" + roomid + "]*", 0, ChatType.Shout));
                                        HandleTaxi(Session, Actor, roomid);
                                        return true;
                                    }
                                }
                                else
                                {
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "Couldn't find room, ID: " + roomid, 0, ChatType.Whisper));
                                    return true;
                                }
                            break;
                            #endregion
                            #region :quitjob
                            case "quitjob":
                            if (Session.CharacterInfo.GroupID == 2)
                            {
                                Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have no job, Therefore you cannot quit your job.", 0, ChatType.Whisper));
                                return true;
                            }
                            else
                            {
                                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                {
                                    MySqlClient.SetParameter("userid", Session.CharacterId);
                                    MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = '1' WHERE id = @userid LIMIT 1");
                                    MySqlClient.SetParameter("userid", Session.CharacterId);
                                    MySqlClient.ExecuteNonQuery("UPDATE characters SET groupid = '2' WHERE id = @userid LIMIT 1");
                                    Session.CharacterInfo.UpdateGroupID(MySqlClient, 2);
                                    Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                    Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, Session.CharacterInfo.Figure, Session.CharacterInfo.Gender, Session.CharacterInfo.Motto, Session.CharacterInfo.Score));
                                    Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Quits their job*", 0, ChatType.Shout));
                                }
                                return true;
                            }
                            break;
                            #endregion
                            #endregion

                            #region Job Commands (10)
                            #region Hospital Commands (1)
                            #region :heal x
                            case "heal":
                                {
                                    if (Session.CharacterInfo.GroupID == 4 && Session.CharacterInfo.Working == 1)
                                    {

                                        if (Bits.Length < 2)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :heal <username>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        Username = TargetSession.CharacterInfo.Username;

                                        if (TargetSession == null || !TargetSession.InRoom)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                        {
                                            TargetSession.CharacterInfo.Heal(MySqlClient);
                                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Healed user from server (chat command)",
                                               "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                                            TargetSession.CharacterInfo.UpdateDead(MySqlClient, 0);
                                        }
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Applies bandages to " + Username + "*", 0, ChatType.Shout));
                                    }
                                    return true;
                                }
                            #endregion // HOSPITAL ONLY
                            #endregion

                            #region Police Commands (3)
                            #region :arrest x <time>
                            case "arrest":
                                {
                                    if (Session.CharacterInfo.GroupID == 3 && Session.CharacterInfo.Working == 1)
                                    {

                                        if (Bits.Length < 3)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :arrest <username> <time /seconds>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        int time = Int32.Parse(UserInputFilter.FilterString(Bits[2].Trim()));
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        Username = TargetSession.CharacterInfo.Username;

                                        if (!TargetActor.Stunned)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have to stun someone before arresting them!", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        if (time < 1)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "You have to arrest someone for 1 or more seconds!", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        if (TargetSession == null || !TargetSession.InRoom || Session.AbsoluteRoomId != TargetSession.AbsoluteRoomId)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline, not in a room, or cannot be arrested.", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        if (Distance.Calculate(Actor.Position.GetVector2(), Target.Position.GetVector2()) > 3)
                                        {
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Atempts to cuff " + Username + ", but misses their hands!*", 0, ChatType.Shout));
                                            return true;
                                        }
                                        string figure_update = FigToUniFig(TargetSession.CharacterInfo.Figure) + "ch-220-94.lg-280-94.sh-290-62";
                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(TargetActor.Id, figure_update, TargetSession.CharacterInfo.Gender, "[JAILED] " + TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                        RoomHandler.PrepareRoom(TargetSession, 9, null, true);
                                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                        {
                                            TargetSession.CharacterInfo.SetHomeRoom(MySqlClient, 9);
                                            TargetSession.CharacterInfo.UpdateJailed(MySqlClient, 1);
                                            TargetSession.CharacterInfo.Timer = time;
                                        }
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Arrests " + Username + " for " + time + " seconds*", 0, ChatType.Shout));
                                    }
                                }
                                return true;
                            #endregion
                            #region :release x
                            case "release":
                                {
                                    if (Session.CharacterInfo.GroupID == 3 && Session.CharacterInfo.Working == 1)
                                    {

                                        if (Bits.Length < 2)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :release <username>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));

                                        if (TargetSession == null || !TargetSession.InRoom || Session.AbsoluteRoomId != TargetSession.AbsoluteRoomId)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline, not in a room, or cannot be arrested.", 0, ChatType.Whisper));
                                            return true;
                                        }
                                        Username = TargetSession.CharacterInfo.Username;

                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(TargetActor.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                        if (TargetSession.CharacterInfo.Jailed == 1)
                                        {
                                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                            {
                                                TargetSession.CharacterInfo.UpdateJailed(MySqlClient, 0);
                                            }
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Releases " + Username + " from jail*", 0, ChatType.Shout));
                                        }
                                        if (TargetSession.CharacterInfo.Dead == 1)
                                        {
                                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                            {
                                                TargetSession.CharacterInfo.UpdateDead(MySqlClient, 0);
                                            }
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Releases " + Username + " from hospital*", 0, ChatType.Shout));
                                        }
                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(TargetActor.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                    }
                                }
                                return true;
                            #endregion
                            #region :stun x
                            case "stun":
                                {
                                    if (Session.CharacterInfo.GroupID == 3 && Session.CharacterInfo.Working == 1)
                                    {

                                        if (Bits.Length < 2)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :stun <username>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));

                                        if (TargetSession == null || !TargetSession.InRoom || Session.AbsoluteRoomId != TargetSession.AbsoluteRoomId)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline, not in a room, or cannot be stunned.", 0, ChatType.Whisper));
                                            return true;
                                        }
                                        Username = TargetSession.CharacterInfo.Username;

                                        if (TargetActor.Stunned)
                                        {
                                            TargetActor.Stunned = false;
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Unstuns " + Username + "*", 0, ChatType.Shout));
                                            TargetActor.ApplyEffect(0);
                                        }
                                        else
                                        {
                                            TargetActor.Stunned = true;
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Stuns " + Username + "*", 0, ChatType.Shout));
                                            TargetActor.ApplyEffect(53);
                                        }
                                    }
                                }
                                return true;
                            #endregion
                            #endregion

                            #region Bank Commands (1)
                            #region :withdraw x <amount>
                            case "withdraw":
                                {
                                    if (Session.CharacterInfo.GroupID == 1 && Session.CharacterInfo.Working == 1)
                                    {

                                        if (Bits.Length < 3)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :withdraw <username> <amount>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        int amount = Int32.Parse(UserInputFilter.FilterString(Bits[2].Trim()));
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        RoomActor Target = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        RoomActor TargetActor = (Instance == null ? null : Instance.GetActorByReferenceId(TargetSession.CharacterId));
                                        Username = TargetSession.CharacterInfo.Username;

                                        if (TargetSession == null || !TargetSession.InRoom || Session.AbsoluteRoomId != TargetSession.AbsoluteRoomId)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Target user '" + Username + "' is offline or not in a room.", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        if (amount < 1 || amount > TargetSession.CharacterInfo.Bank)
                                        {
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Attempts to withdraw money from " + Username + "'s account, but they have only have "+ TargetSession.CharacterInfo.Bank +"!*", 0, ChatType.Shout));
                                            return true;
                                        }
                                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                        {
                                            int newamount = TargetSession.CharacterInfo.Bank - amount;
                                            TargetSession.CharacterInfo.UpdateBank(MySqlClient, newamount);
                                            int newcredits = TargetSession.CharacterInfo.CreditsBalance + amount;
                                            TargetSession.CharacterInfo.UpdateCreditsBalance(MySqlClient, newcredits);
                                            TargetSession.SendData(CreditsBalanceComposer.Compose(TargetSession.CharacterInfo.CreditsBalance));
                                        }
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Withdraws " + amount + " coins from " + Username + "'s bank account*", 0, ChatType.Shout));
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hands " + amount + " coins to " + Username + "*", 0, ChatType.Shout));
                                    }
                                }
                                return true;
                            #endregion
                            #endregion

                            #region Manager Commands (5)
                            #region :hire x
                            case "hire":
                                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                {
                                    DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                    DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                    DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                    if (Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == (string)"manager")
                                    {
                                        if (Bits.Length < 2)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :hire <username>", 0, ChatType.Whisper));
                                            return true;
                                        }

                                        string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                        Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                        if (TargetSession == null || !TargetSession.InRoom)
                                        {
                                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                            return true;
                                        }
                                        Username = TargetSession.CharacterInfo.Username;
                                        if (TargetSession.CharacterInfo.GroupID == 2)
                                        {
                                            DataRow Row4 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '1'");
                                            string jobname = (string)Row1["name"] + " " + (string)Row4["name"];
                                            MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                            MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = '1' WHERE id = @userid LIMIT 1");
                                            MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                            MySqlClient.SetParameter("groupid", Session.CharacterInfo.GroupID);
                                            MySqlClient.ExecuteNonQuery("UPDATE characters SET groupid = @groupid WHERE id = @userid LIMIT 1");
                                            TargetSession.CharacterInfo.UpdateGroupID(MySqlClient, Session.CharacterInfo.GroupID);
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Hires " + Username + " as " + jobname + "*", 0, ChatType.Shout));
                                            TargetSession.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                            RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                            Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                            return true;
                                        }
                                        else
                                        {
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Attempts to hire " + Username + ", but notices they already have a job*", 0, ChatType.Shout));
                                            return true;
                                        }
                                    }
                                }
                            break;
                            #endregion
                            #region :fire x
                            case "fire":
                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                            {
                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                if (Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == "manager" || Session.HasRight("hotel_admin"))
                                {
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :fire <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    if (TargetSession == null || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                    Username = TargetSession.CharacterInfo.Username;
                                    if (TargetSession.CharacterInfo.GroupID == Session.CharacterInfo.GroupID || Session.HasRight("hotel_admin"))
                                    {
                                        MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                        MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = '1' WHERE id = @userid LIMIT 1");
                                        MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                        MySqlClient.ExecuteNonQuery("UPDATE characters SET groupid = '2' WHERE id = @userid LIMIT 1");
                                        TargetSession.CharacterInfo.UpdateGroupID(MySqlClient, 2);
                                        TargetSession.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Fires " + Username + " from their job*", 0, ChatType.Shout));
                                        return true;
                                    }
                                }
                            }
                            break;
                            #endregion
                            #region :promote x
                            case "promote":
                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                            {
                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                if (Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == (string)"manager" || Session.HasRight("hotel_admin"))
                                {
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :promote <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    if (TargetSession == null || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                    Username = TargetSession.CharacterInfo.Username;
                                    if (TargetSession.CharacterInfo.GroupID == Session.CharacterInfo.GroupID || Session.HasRight("hotel_admin"))
                                    {
                                        DataRow Row4 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + TargetSession.CharacterInfo.Id + "'");

                                        int newrank = (int)Row4["jobrank"] + 1;
                                        DataRow Row5 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + TargetSession.CharacterInfo.GroupID + "' AND rankid = '" + newrank + "'");
                                        DataRow Row6 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Row5["jobid"] + "'");
                                        int rowsInRow5 = int.Parse(MySqlClient.ExecuteScalar("SELECT COUNT(*) FROM jobranks WHERE jobid = '" + Row5["jobid"] + "' AND rankid = '" + newrank + "'").ToString());
                                        if (rowsInRow5 >= 1 && (string)Row5["rank"] != (string)"manager" || Session.HasRight("hotel_admin"))
                                        {
                                            string JobName = (string)Row6["name"] + " " + (string)Row5["name"];
                                            MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                            MySqlClient.SetParameter("rank", newrank);
                                            MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = @rank WHERE id = @userid LIMIT 1");
                                            Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Promotes " + Username + " to " + JobName + "*", 0, ChatType.Shout));
                                        }
                                        return true;
                                    }
                                }
                            }
                            break;
                            #endregion
                            #region :demote x
                            case "demote":
                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                            {
                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                if (Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == (string)"manager" || Session.HasRight("hotel_admin"))
                                {
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :promote <username>", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    if (TargetSession == null || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                    Username = TargetSession.CharacterInfo.Username;
                                    if (TargetSession.CharacterInfo.GroupID == Session.CharacterInfo.GroupID || Session.HasRight("hotel_admin"))
                                    {
                                        DataRow Row4 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + TargetSession.CharacterInfo.Id + "'");

                                        int newrank = (int)Row4["jobrank"] - 1;
                                        DataRow Row5 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + TargetSession.CharacterInfo.GroupID + "' AND rankid = '" + newrank + "'");
                                        DataRow Row6 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Row5["jobid"] + "'");
                                        int rowsInRow5 = int.Parse(MySqlClient.ExecuteScalar("SELECT COUNT(*) FROM jobranks WHERE jobid = '" + Row5["jobid"] + "' AND rankid = '" + newrank + "'").ToString());
                                        if (rowsInRow5 >= 1 && (string)Row5["rank"] != (string)"manager" || Session.HasRight("hotel_admin"))
                                        {
                                            string JobName = (string)Row6["name"] + " " + (string)Row5["name"];
                                            MySqlClient.SetParameter("userid", TargetSession.CharacterId);
                                            MySqlClient.SetParameter("rank", newrank);
                                            MySqlClient.ExecuteNonQuery("UPDATE characters SET jobrank = @rank WHERE id = @userid LIMIT 1");
                                            Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                            Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Demotes " + Username + " to " + JobName + "*", 0, ChatType.Shout));
                                        }
                                        return true;
                                    }
                                }
                            }
                            break;
                            #endregion

                            #region :sendhome x [time]
                            case "sendhome":
                            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                            {
                                DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                                DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                                DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                                if (Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == "manager" || Session.HasRight("hotel_admin") || Session.CharacterInfo.Working == 1 && (string)Row3["rank"] == "supervisor")
                                {
                                    if (Bits.Length < 2)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :sendhome <username> [<time>]", 0, ChatType.Whisper));
                                        return true;
                                    }

                                    string Username = UserInputFilter.FilterString(Bits[1].Trim());
                                    int Time = 900;
                                    if (Bits.Length > 2)
                                    {
                                        Time = int.Parse(Bits[2].Trim());
                                    }
                                    Session TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(Username));
                                    if (TargetSession == null || !TargetSession.InRoom)
                                    {
                                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Could not find " + Username + " in this room!", 0, ChatType.Whisper));
                                        return true;
                                    }
                                    RoomActor Target = Instance.GetActorByReferenceId(TargetSession.CharacterId);
                                    Username = TargetSession.CharacterInfo.Username;
                                    if (TargetSession.CharacterInfo.GroupID == Session.CharacterInfo.GroupID || Session.HasRight("hotel_admin"))
                                    {
                                        TargetSession.CharacterInfo.UpdateWorking(MySqlClient, 0);
                                        Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Target.Id, TargetSession.CharacterInfo.Figure, TargetSession.CharacterInfo.Gender, TargetSession.CharacterInfo.Motto, TargetSession.CharacterInfo.Score));
                                        Instance.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "*Sends " + Username + " home for " + Time + " seconds*", 0, ChatType.Shout));
                                        SendHome(TargetSession, Time);
                                        return true;
                                    }
                                }
                            }
                            break;
                            #endregion
                            #endregion
                            #endregion

                            #region VIP Commands (2)
                            #region :drive
                            case "drive":

                                if (Session.CurrentEffect == 0)
                                {
                                    Actor.ApplyEffect(21);
                                }
                                else
                                {
                                    Actor.ApplyEffect(0);
                                }
                                return true;
                            #endregion
                            #region :moonwalk
                            case "moonwalk":
                                if (Actor.WalkingBackwards)
                                {
                                    Actor.WalkingBackwards = false;
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "Moonwalk disabled!", 0, ChatType.Whisper));
                                }
                                else
                                {
                                    Actor.WalkingBackwards = true;
                                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "Moonwalk enabled!", 0, ChatType.Whisper));
                                }
                                return true;
                            #endregion
                            #endregion
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        Session.SendData(MessageOfTheDayComposer.Compose("Error in Command " + Bits[0].ToLower() + ": " + e.Message + "\n\n" + e.StackTrace));
                        string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                        Output.WriteLine("Error in Command " + Bits[0].ToLower() + ": " + e.Message);
                        System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                        file.WriteLine(text + "Error in Command " + Bits[0].ToLower() + ": " + e.Message + "\n\n" + e.StackTrace,
                            OutputLevel.Notification + "\n\n");
                        file.Close();
                        return false;
                    }
                }


                public static bool SendHome(Session Session, int Time)
                {
                    Session.CharacterInfo.UpdateSentHome(1);
                    Time = Time * 1000;
                    System.Timers.Timer dispatcherTimer = new System.Timers.Timer(Time);
                    dispatcherTimer.Interval = Time;
                    dispatcherTimer.Elapsed += delegate { FinishSendHome(Session, dispatcherTimer); };
                    dispatcherTimer.Start();
                    return true;
                }

                public static bool FinishSendHome(Session Session, System.Timers.Timer Timer)
                {
                    Timer.Dispose();
                    Timer.Stop();
                    Session.CharacterInfo.UpdateSentHome(0);
                    return true;
                }

                public static bool HandleTaxi(Session Session, RoomActor Actor, uint RoomID, bool instant = false)
                {
                    int time = RandomGenerator.GetNext(6, 40);
                    time = time * 1000;
                    if (instant)
                    {
                        time = 1;
                    }
                    else
                    {
                        SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient();
                        int newcoins = Session.CharacterInfo.CreditsBalance - 10;
                        Session.SendData(CreditsBalanceComposer.Compose(newcoins));
                        Session.CharacterInfo.UpdateCreditsBalance(MySqlClient, -10);
                    }
                    System.Timers.Timer dispatcherTimer = new System.Timers.Timer(time);
                    dispatcherTimer.Interval = time;
                    dispatcherTimer.Elapsed += delegate { ExecuteTaxi(Session, Actor, RoomID, dispatcherTimer); };
                    dispatcherTimer.Start();
                    return true;
                }

                public static bool ExecuteTaxi(Session Session, RoomActor Actor, uint RoomID, System.Timers.Timer Timer)
                {
                    Timer.Dispose();
                    Timer.Stop();
                    if (Session.CharacterInfo.Dead != 1 && Session.CharacterInfo.Jailed != 1)
                    {
                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            Session.CharacterInfo.SetHomeRoom(MySqlClient, RoomID);
                            RoomHandler.PrepareRoom(Session, RoomID, null, true);
                        }
                    }
                    return true;
                }
    }
}

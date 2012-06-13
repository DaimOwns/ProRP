using System;
using System.Collections.Generic;
using System.Data;

using Reality.Game.Rooms;
using Reality.Game.Characters;
using Reality.Game.Bots;
using Reality.Storage;
using Reality.Game.Sessions;
using System.Text.RegularExpressions;

namespace Reality.Communication.Outgoing
{
    public static class RoomUserObjectListComposer
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

        public static ServerMessage Compose(List<RoomActor> Actors)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.ROOM_USER_LIST);
            Message.AppendInt32(Actors.Count);

            foreach (RoomActor Actor in Actors)
            {
                bool IsBot = (Actor.Type == RoomActorType.AiBot);
                Bot BotData = (IsBot ? (Bot)Actor.ReferenceObject : null);
                bool IsPet = (BotData != null && BotData.IsPet);

                if (IsBot && !IsPet)
                {
                    Message.AppendInt32(-1);
                }
                else
                {
                    Message.AppendUInt32(Actor.ReferenceId);
                }
                
                Message.AppendStringWithBreak(Actor.Name);

                if (Actor != null)
                {
                    if (!IsBot && !IsPet)
                    {
                        string figure_update = "";
                        string motto = "";
                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            Session Session = SessionManager.GetSessionByCharacterId(Actor.ReferenceId);
                            DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Actor.GroupId + "'");
                            DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Actor.ReferenceId + "'");
                            DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Actor.GroupId + "' AND rankid = '" + Row2["jobrank"] + "'");
                            int room_id = (int)Row1["roomid"];
                            string jobname = (string)Row1["name"] + " " + (string)Row3["name"];
                            string jobfig = (string)Row3["figure_data_" + Actor.Gender.ToString()];
                            if (Actor.Jailed == 1)
                            {
                                Actor.UpdateWorking(MySqlClient, 0);
                                figure_update = FigToUniFig(Actor.Figure) + "ch-220-94.lg-280-94.sh-290-62";
                                motto = "[JAILED] " + Actor.Motto;
                            }
                            else if (Actor.Dead == 1)
                            {
                                Actor.UpdateWorking(MySqlClient, 0);
                                figure_update = Actor.Figure;
                                motto = "[DEAD] " + Actor.Motto;
                            }
                            else if (Actor.Working == 1)
                            {
                                if (room_id == Actor.CurrentRoomId || room_id == 0)
                                {
                                    if (jobfig == "")
                                    {
                                        figure_update = Actor.Figure;
                                    }
                                    else
                                    {
                                        figure_update = FigToUniFig(Actor.Figure) + jobfig;
                                    }
                                    motto = "[WORKING] " + jobname;
                                }
                                else
                                {
                                    Actor.UpdateWorking(MySqlClient, 0);
                                }
                            }
                            else
                            {
                                figure_update = Actor.Figure;
                                motto = Actor.Motto;
                                Actor.UpdateWorking(MySqlClient, 0);
                            }
                            Message.AppendStringWithBreak(motto);
                            Message.AppendStringWithBreak(figure_update);
                        }
                    }
                    else
                    {
                        Message.AppendStringWithBreak(Actor.Motto);
                        Message.AppendStringWithBreak(Actor.Figure);
                    }
                }
                Message.AppendUInt32(Actor.Id);
                Message.AppendInt32(Actor.Position.X);
                Message.AppendInt32(Actor.Position.Y);
                Message.AppendRawDouble(Actor.Position.Z);

                Message.AppendInt32(Actor.Type == RoomActorType.UserCharacter ? 2 : 4); // 2 for user, 4 for bot
                Message.AppendInt32(Actor.Type == RoomActorType.UserCharacter ? 1 : (((Bot)Actor.ReferenceObject).BehaviorType == "pet" ? 2 : 3)); // 1 for user, 2 for pet, 3 for other bot

                if (!IsBot)
                {
                    int groupid;
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        DataRow GroupExtraData = MySqlClient.ExecuteQueryRow("SELECT groupid FROM characters WHERE id = ' " + Actor.ReferenceId + "'");
                        groupid = ((int)GroupExtraData["groupid"]);
                    }
                    
                    Message.AppendStringWithBreak(((CharacterInfo)Actor.ReferenceObject).Gender == CharacterGender.Male ? "m" : "f");
                    Message.AppendInt32(-1); // Unknown
                    Message.AppendInt32(groupid); // Group ID
                    Message.AppendInt32(-1); // Unknown (sometimes -1, sometimes 1)
                    Message.AppendStringWithBreak(string.Empty);
                    Message.AppendInt32(((CharacterInfo)Actor.ReferenceObject).Score);
                }
                else if (IsPet)
                {
                    Message.AppendInt32(500);
                }
            }

            return Message;
        }
    }
}

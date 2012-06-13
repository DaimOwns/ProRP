using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using Reality.Game.Sessions;
using Reality.Game.Characters;
using Reality.Storage;
using Reality.Game.Rooms;


namespace Reality.Communication.Outgoing
{
    public static class UserObjectComposer
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

        public static ServerMessage Compose(Session Session)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.USER_OBJECT);
            Message.AppendStringWithBreak(Session.CharacterId.ToString());
            Message.AppendStringWithBreak(Session.CharacterInfo.Username);
                string figure_update = "";
                string motto = "";
                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    DataRow Row1 = MySqlClient.ExecuteQueryRow("SELECT * FROM groups_details WHERE id = '" + Session.CharacterInfo.GroupID + "'");
                    DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Session.CharacterInfo.Id + "'");
                    DataRow Row3 = MySqlClient.ExecuteQueryRow("SELECT * FROM jobranks WHERE jobid = '" + Session.CharacterInfo.GroupID + "' AND rankid = '" + Row2["jobrank"] + "'");
                    int room_id = (int)Row1["roomid"];
                    string jobname = (string)Row1["name"] + " " + (string)Row3["name"];
                    string jobfig = (string)Row3["figure_data_" + Session.CharacterInfo.Gender.ToString()];

                    RoomInstance Instance = RoomManager.GetInstanceByRoomId(Session.CharacterInfo.HomeRoom);

                    if (Session.CharacterInfo.Jailed == 1)
                    {
                        Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                        figure_update = FigToUniFig(Session.CharacterInfo.Figure) + "ch-220-94.lg-280-94.sh-290-62";
                        motto = "[JAILED] " + Session.CharacterInfo.Motto;
                    }
                    else if (Session.CharacterInfo.Dead == 1)
                    {
                        Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                        figure_update = Session.CharacterInfo.Figure;
                        motto = "[DEAD] " + Session.CharacterInfo.Motto;
                    }
                    else if (Session.CharacterInfo.Working == 1)
                    {
                        if (room_id == Session.CurrentRoomId || room_id == 0)
                        {
                            if (jobfig == "")
                            {
                                figure_update = Session.CharacterInfo.Figure;
                            }
                            else
                            {
                                figure_update = FigToUniFig(Session.CharacterInfo.Figure) + jobfig;
                            }
                            motto = "[WORKING] " + jobname;
                        }
                        else
                        {
                            Session.CharacterInfo.UpdateWorking(MySqlClient, 0);
                        }
                    }
                }

                    Message.AppendStringWithBreak(figure_update);
                    Message.AppendStringWithBreak(Session.CharacterInfo.Gender == CharacterGender.Male ? "M" : "F");
                    Message.AppendStringWithBreak(motto);
            Message.AppendStringWithBreak(Session.CharacterInfo.RealName);
            Message.AppendInt32(0);
            Message.AppendStringWithBreak("");
            Message.AppendInt32(0);
            Message.AppendInt32(0);
            Message.AppendInt32(Session.CharacterInfo.RespectPoints);
            Message.AppendInt32(Session.CharacterInfo.RespectCreditHuman);
            Message.AppendInt32(Session.CharacterInfo.RespectCreditPets);
            Message.AppendUInt32(24708);
            return Message;
        }
    }
}

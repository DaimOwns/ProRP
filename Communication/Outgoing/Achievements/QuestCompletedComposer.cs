using System;

using Reality.Game.Achievements;
using Reality.Game.Sessions;

namespace Reality.Communication.Outgoing
{
    public static class QuestCompletedComposer
    {
        public static ServerMessage Compose(Session Session, Quest Quest)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.QUEST_COMPLETED);
            QuestListComposer.SerializeQuest(Message, Session, Quest, Quest.Category);
            return Message;
        }
    }
}

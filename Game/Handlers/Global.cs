using System;

using Reality.Game.Sessions;
using Reality.Communication;
using Reality.Communication.Outgoing;
using Reality.Communication.Incoming;
using Reality.Game.Rooms;

namespace Reality.Game.Handlers
{
    public static class GlobalHandler
    {
        public static void Initialize()
        {
            DataRouter.RegisterHandler(OpcodesIn.SESSION_PONG, new ProcessRequestCallback(OnSessionPong), true);
            DataRouter.RegisterHandler(OpcodesIn.SESSION_USER_AGENT_INFORMATION, new ProcessRequestCallback(OnUserAgent), true);
            DataRouter.RegisterHandler(OpcodesIn.SESSION_LATENCY_TEST, new ProcessRequestCallback(OnSessionLatencyTest), true);
            DataRouter.RegisterHandler(OpcodesIn.SESSION_DEBUG_EVENT, new ProcessRequestCallback(OnDebugEvent), true);
            DataRouter.RegisterHandler(OpcodesIn.SESSION_CLIENT_CONFIGURATION, new ProcessRequestCallback(OnClientConfig));
            DataRouter.RegisterHandler(OpcodesIn.SESSION_DISCONNECT_EVENT, new ProcessRequestCallback(OnClientDisconnectNotification), true);
        }

        private static void OnSessionPong(Session Session, ClientMessage Message)
        {
            Session.LatencyTestOk = true;
        }

        private static void OnUserAgent(Session Session, ClientMessage Message)
        {
            string UserAgent = Message.PopString();

            if (UserAgent.Length > 2000)
            {
                UserAgent = UserAgent.Substring(0, 2000);
            }

            Session.UserAgent = UserAgent;
        }

        private static void OnSessionLatencyTest(Session Session, ClientMessage Message)
        {
            // Sesion timer sends a number to the server and expects it right back.
            // Maybe something to do with latency testing... or a keepalive?
            // Seems like a waste of bandwith since we're using pinging

            Session.SendData(LatencyTestResponseComposer.Compose(Message.PopWiredInt32()));
        }

        private static void OnDebugEvent(Session Session, ClientMessage Message)
        {
            // Debug events are sometimes sent when a user clicks an element
            // This information is completely useless to us (+bandwith waste), so we're ignoring it for now
        }

        private static void OnClientConfig(Session Session, ClientMessage Message)
        {
            int Volume = Message.PopWiredInt32();
            bool Something = Message.PopWiredBoolean();

            if (Volume < 0)
            {
                Volume = 0;
            }

            if (Volume > 100)
            {
                Volume = 100;
            }

            Session.CharacterInfo.ConfigVolume = Volume;
        }

        private static void OnClientDisconnectNotification(Session Session, ClientMessage Message)
        {
            RoomInstance Instance1 = RoomManager.GetInstanceByRoomId(Session.CurrentRoomId);
            RoomActor Actor = Instance1.GetActorByReferenceId(Session.CharacterInfo.Id);
            Instance1.BroadcastMessage(RoomChatComposer.Compose(Actor.Id, "-- Logging out in 10 seconds! -- [DISCONNECED]", 0, ChatType.Shout));
            System.Threading.Thread.Sleep(10000);
            SessionManager.StopSession(Session.Id);
        }
    }
}

using System;

using Reality.Game.Advertisements;

namespace Reality.Communication.Outgoing
{
    public static class RoomInterstitialComposer
    {
        public static ServerMessage Compose(Interstitial Interstitial)
        {
            ServerMessage Message = new ServerMessage(258);
            Message.AppendStringWithBreak(Interstitial == null ? string.Empty : Interstitial.Image);
            Message.AppendStringWithBreak(Interstitial == null ? string.Empty : Interstitial.Url);
            return Message;
        }
    }
}

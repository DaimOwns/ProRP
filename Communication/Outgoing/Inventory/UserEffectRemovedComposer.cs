using System;

using Reality.Game.AvatarEffects;

namespace Reality.Communication.Outgoing
{
    public static class UserEffectRemovedComposer
    {
        public static ServerMessage Compose(AvatarEffect Effect)
        {
            ServerMessage Message = new ServerMessage(OpcodesOut.USER_EFFECT_EXPIRED);
            Message.AppendInt32(Effect.SpriteId);
            return Message;
        }
    }
}

using System;

namespace Reality.Communication.Outgoing
{
    public static class ModerationPickingErrorComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.MODERATION_PICKING_ERROR);
        }
    }
}

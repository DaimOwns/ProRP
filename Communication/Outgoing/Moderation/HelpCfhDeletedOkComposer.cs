using System;

namespace Reality.Communication.Outgoing
{
    public static class HelpCfhDeletedOkComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.HELP_CFH_DELETED_OK);
        }
    }
}

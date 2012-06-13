﻿using System;

namespace Reality.Communication.Outgoing
{
    public static class CatalogUpdatedNotificationComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.CATALOG_UPDATED_NOTIFICATION);
        }
    }
}

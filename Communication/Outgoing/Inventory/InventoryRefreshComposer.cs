﻿using System;

namespace Reality.Communication.Outgoing
{
    public static class InventoryRefreshComposer
    {
        public static ServerMessage Compose()
        {
            return new ServerMessage(OpcodesOut.INVENTORY_REFRESH);
        }
    }
}

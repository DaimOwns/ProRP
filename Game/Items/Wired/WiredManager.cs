using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reality.Storage;
using Reality.Game.Sessions;
using Reality.Communication;
using Reality.Communication.Incoming;
using Reality.Game.Rooms;
using Reality.Specialized;
using System.Collections.ObjectModel;
using Reality.Util;
using Reality.Communication.Outgoing;
using System.Text.RegularExpressions;
using System.Timers;
using System.Data;

namespace Reality.Game.Items.Wired
{
    public enum WiredTriggerTypes
    {
        says_something = 0,
        walks_on_furni = 1,
        walks_off_furni = 2,
        at_given_time = 3,
        state_changed = 4,
        periodically = 6,
        enter_room = 7,
        on_death = 8
    }

    public enum WiredEffectTypes
    {
        toggle_state = 0,
        match_to_sshot = 3,
        move_rotate = 4,
        show_message = 7,
        teleport_to = 8,
        give_clothes = 9,
        reset_timer = 10,
        give_clothes_back = 12
    }

    public static class WiredTypesUtil
    {
        public static WiredTriggerTypes TriggerFromInt(int Type)
        {
            switch (Type)
            {
                default:
                case 0:
                    return WiredTriggerTypes.says_something;
                case 1:
                    return WiredTriggerTypes.walks_on_furni;
                case 2:
                    return WiredTriggerTypes.walks_off_furni;
                case 3:
                    return WiredTriggerTypes.at_given_time;
                case 4:
                    return WiredTriggerTypes.state_changed;
                case 6:
                    return WiredTriggerTypes.periodically;
                case 7:
                    return WiredTriggerTypes.enter_room;
                case 8:
                    return WiredTriggerTypes.on_death;
            }
        }

        public static WiredEffectTypes EffectFromInt(int Type)
        {
            switch (Type)
            {
                default:
                case 0:
                    return WiredEffectTypes.toggle_state;
                case 3:
                    return WiredEffectTypes.match_to_sshot;
                case 4:
                    return WiredEffectTypes.move_rotate;
                case 7:
                    return WiredEffectTypes.show_message;
                case 8:
                    return WiredEffectTypes.teleport_to;
                case 9:
                    return WiredEffectTypes.give_clothes;
                case 10:
                    return WiredEffectTypes.reset_timer;
                case 12:
                    return WiredEffectTypes.give_clothes_back;
            }
        }
    }

    public class WiredManager
    {
        private Dictionary<uint, WiredData> mWired;
        private RoomInstance mInstance;
        private Dictionary<uint, uint> mRegisteredWalkItems;

        public WiredManager(RoomInstance Instance)
        {
            mInstance = Instance;
            mWired = new Dictionary<uint, WiredData>();
            mRegisteredWalkItems = new Dictionary<uint, uint>();
        }

        public WiredData LoadWired(uint ItemId, int Type)
        {

            if (!mWired.ContainsKey(ItemId))
            {
                mWired.Add(ItemId, new WiredData(ItemId, Type));
            }


            return mWired[ItemId];
        }

        public void RemoveWired(uint ItemId, SqlDatabaseClient MySqlClient)
        {
            if (mWired.ContainsKey(ItemId))
            {
                mWired.Remove(ItemId);
                DeRegisterWalkItems(ItemId);
                MySqlClient.SetParameter("id", ItemId);
                MySqlClient.ExecuteNonQuery("DELETE FROM wired_items WHERE item_id = @id");
            }
        }

        public void SynchronizeDatabase(SqlDatabaseClient MySqlClient)
        {
            foreach (WiredData data in mWired.Values)
            {
                data.SynchronizeDatabase(MySqlClient);
            }
        }

        public void HandleSave(Session Session, ClientMessage Message)
        {
            uint ItemId = Message.PopWiredUInt32();



            if (!mInstance.CheckUserRights(Session) || !mWired.ContainsKey(ItemId))
            {
                return;
            }

            Item item = mInstance.GetItem(ItemId);

            if (item == null)
            {
                return;
            }

            WiredData data = mWired[ItemId];

            String Data1 = "";
            int Data2 = 0;
            int Data3 = 0;
            int Data4 = 0;
            int Time = 0;
            String Data5 = "";

            Message.PopWiredInt32();
            Data2 = Message.PopWiredInt32();

            Boolean Simple = true;

            if (item.Definition.Behavior == ItemBehavior.WiredEffect)
            {
                switch (WiredTypesUtil.EffectFromInt(item.Definition.BehaviorData))
                {
                    case WiredEffectTypes.match_to_sshot:
                    case WiredEffectTypes.move_rotate:
                    case WiredEffectTypes.teleport_to:
                    case WiredEffectTypes.toggle_state:
                        Simple = false;
                        break;
                }
            }

            if (item.Definition.Behavior == ItemBehavior.WiredTrigger)
            {
                switch (WiredTypesUtil.TriggerFromInt(item.Definition.BehaviorData))
                {
                    case WiredTriggerTypes.state_changed:
                    case WiredTriggerTypes.walks_off_furni:
                    case WiredTriggerTypes.walks_on_furni:
                        Simple = false;
                        break;
                    case WiredTriggerTypes.periodically:
                        item.RequestUpdate(Data2);
                        break;
                }
            }

            if (!Simple)
            {
                Data3 = Message.PopWiredInt32();

                if (item.Definition.Behavior == ItemBehavior.WiredEffect && WiredTypesUtil.EffectFromInt(item.Definition.BehaviorData) == WiredEffectTypes.match_to_sshot)
                {
                    Data4 = Message.PopWiredInt32();
                }

                Message.PopString();
                int c = Message.PopWiredInt32();
                for (int i = 0; i < c; i++)
                {
                    uint tmp = Message.PopWiredUInt32();
                    if (mInstance.GetItem(tmp) == null)
                    {
                        continue;
                    }
                    if (tmp != 0)
                    {
                        Data1 += "" + tmp.ToString() + "|";
                    }
                }

                Time = Message.PopWiredInt32();
            }
            else
            {
                Data1 = Message.PopString();
                Data3 = Message.PopWiredInt32();
            }


            if (item.Definition.Behavior == ItemBehavior.WiredEffect)
            {
                switch (WiredTypesUtil.EffectFromInt(item.Definition.BehaviorData))
                {
                    case WiredEffectTypes.match_to_sshot:
                        String[] Selected = Data1.Split('|');

                        foreach (String ItemIdS in Selected)
                        {
                            uint SelectedItemId;
                            uint.TryParse(ItemIdS, out SelectedItemId);
                            Item Item = mInstance.GetItem(SelectedItemId);
                            if (Item == null)
                            {
                                continue;
                            }

                            Data5 += Item.Id + "#" + Item.RoomPosition.ToString() + "#" + Item.RoomRotation + "#" + Item.Flags + "+";
                        }
                        break;
                }
            }

            if (data.Data1 == Data1 && data.Data2 == Data2 && data.Data3 == Data3 && data.Data4 == Data4 && data.Time == Time && data.Data5 == Data5)
            {
                return;
            }

            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {

                data.Data1 = Data1;
                data.Data2 = Data2;
                data.Data3 = Data3;
                data.Data4 = Data4;
                data.Data5 = Data5;
                data.Time = Time;
                data.SynchronizeDatabase(MySqlClient);
            }

            if (item.Definition.Behavior == ItemBehavior.WiredTrigger)
            {
                switch (WiredTypesUtil.TriggerFromInt(item.Definition.BehaviorData))
                {
                    case WiredTriggerTypes.at_given_time:
                        item.RequestUpdate(Data2);
                        break;
                    case WiredTriggerTypes.walks_on_furni:
                    case WiredTriggerTypes.walks_off_furni:
                        DeRegisterWalkItems(item.Id);
                        RegisterWalkItems(item.Id);
                        break;
                }
            }

        }

        public uint GetRegisteredWalkItem(uint Id)
        {
            if (mRegisteredWalkItems.ContainsKey(Id))
            {
                return mRegisteredWalkItems[Id];
            }
            return 0;
        }

        public void RegisterWalkItems(uint ItemId)
        {

            String[] Selected = mWired[ItemId].Data1.Split('|');

            foreach (String ItemIdS in Selected)
            {
                uint Id;
                uint.TryParse(ItemIdS, out Id);
                Item check = mInstance.GetItem(Id);
                if (check == null)
                {
                    continue;
                }

                if (!mRegisteredWalkItems.ContainsKey(Id))
                {
                    mRegisteredWalkItems.Add(Id, ItemId);
                }
            }
        }

        private void DeRegisterWalkItems(uint ItemId)  // DeRegister by WiredItem
        {
            if (!mRegisteredWalkItems.ContainsValue(ItemId))
            {
                return;
            }

            List<uint> ToRemove = new List<uint>();
            foreach (uint Id in mRegisteredWalkItems.Keys)
            {
                if (mRegisteredWalkItems[Id] == ItemId)
                {
                    ToRemove.Add(Id);
                }
            }

            foreach (uint Id in ToRemove)
            {
                if (mRegisteredWalkItems.ContainsKey(Id))
                {
                    mRegisteredWalkItems.Remove(Id);
                }
            }
        }

        public void DeRegisterWalkItem(uint Id)  // Deregister by Walkable Item
        {
            if (mRegisteredWalkItems.ContainsKey(Id))
            {
                mRegisteredWalkItems.Remove(Id);
            }
        }

        public void HandleEnterRoom(RoomActor Actor)
        {
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.enter_room)
                {
                    if (data.Data1 != "" && data.Data1 != Actor.Name)
                    {
                        continue;
                    }

                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(mInstance);
                    Item.DisplayFlags = "";
                    Item.RequestUpdate(4);

                    ExecuteActions(Item, Actor);
                }
            }
        }

        public bool HandleDeath(RoomActor Actor)
        {
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.on_death)
                {
                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(mInstance);
                    Item.DisplayFlags = "";
                    Item.RequestUpdate(4);

                    ExecuteActions(Item, Actor);
                    return true;
                }
            }
            return false;
        }

        public void HandleToggleState(RoomActor Actor, Item TheItemChanged)
        {
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.state_changed)
                {
                    String[] Selected3 = Item.WiredData.Data1.Split('|');

                    foreach (String ItemIdS2 in Selected3)
                    {
                        uint ItemId3;
                        uint.TryParse(ItemIdS2, out ItemId3);
                        Item AffectedItem3 = mInstance.GetItem(ItemId3);
                        if (AffectedItem3 == null)
                        {
                            continue;
                        }
                        if (AffectedItem3 == TheItemChanged)
                        {
                            Item.DisplayFlags = "1";
                            Item.BroadcastStateUpdate(mInstance);
                            Item.DisplayFlags = "";
                            Item.RequestUpdate(4);

                            ExecuteActions(Item, Actor);
                        }
                    }
                }
            }
        }

        public List<Item> TriggerRequiresActor(int BehaviorData, Vector2 Position)
        {
            List<Item> Items = new List<Item>();

            if (WiredTypesUtil.TriggerFromInt(BehaviorData) != WiredTriggerTypes.periodically)
            {
                return Items;
            }


            foreach (Item Item in mInstance.GetItemsOnPosition(Position))
            {
                if (Item.Definition.Behavior != ItemBehavior.WiredEffect)
                {
                    continue;
                }
                if (WiredTypesUtil.EffectFromInt(Item.Definition.BehaviorData) == WiredEffectTypes.show_message || WiredTypesUtil.EffectFromInt(Item.Definition.BehaviorData) == WiredEffectTypes.give_clothes)
                {
                    Items.Add(Item);
                }
            }

            return Items;
        }

        public List<Item> ActionRequiresActor(int BehaviorData, Vector2 Position)
        {
            List<Item> Items = new List<Item>();

            if (WiredTypesUtil.EffectFromInt(BehaviorData) != WiredEffectTypes.show_message || WiredTypesUtil.EffectFromInt(BehaviorData) != WiredEffectTypes.give_clothes)
            {
                return Items;
            }

            foreach (Item Item in mInstance.GetItemsOnPosition(Position))
            {
                if (Item.Definition.Behavior != ItemBehavior.WiredTrigger)
                {
                    continue;
                }
                if (WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.periodically)
                {
                    Items.Add(Item);
                }
            }


            return Items;
        }

        public bool HandleChat(String Message, RoomActor Actor)
        {
            Boolean doneAction = false;
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger &&
                    WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.says_something &&
                    Message.ToLower().Contains(data.Data1.ToLower()) && (data.Data2 == 0 || data.Data2 == Actor.Id) && data.Data1 != ""
                    )
                {

                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(mInstance);
                    Item.DisplayFlags = "2";
                    Item.RequestUpdate(4);

                    ExecuteActions(Item, Actor);
                    doneAction = true;
                }
            }
            return doneAction;
        }

        public bool HandlePeriodicly(Item Item, RoomActor Actor)
        {
            uint ItemID = Item.Id;
            int time = mWired[ItemID].Data2 * 100;
            time = time % 2;
            time = time * 10;
            if (time == 0)
            {
                time = 500;
            }
            System.Timers.Timer dispatcherTimer = new System.Timers.Timer(time);
            dispatcherTimer.Interval = time;
            dispatcherTimer.Elapsed += delegate { ExecuteActions(Item, Actor); };
            dispatcherTimer.Start();
            return true;
        }

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

        public void ExecuteActions(Item Item, RoomActor Actor)
        {
            try
            {
                Random rnd = new Random();
                foreach (Item ActionItem in mInstance.GetItemsOnPosition(Item.RoomPosition.GetVector2()))
                {
                    if (ActionItem.Definition.Behavior == ItemBehavior.WiredEffect)
                    {
                        ActionItem.DisplayFlags = "1";
                        ActionItem.BroadcastStateUpdate(mInstance);
                        ActionItem.DisplayFlags = "2";
                        ActionItem.RequestUpdate(4);
                        Session Session = null;
                        if (Actor != null)
                        {
                            Session = SessionManager.GetSessionByCharacterId(Actor.ReferenceId);
                        }
                        RoomInstance Instance = mInstance;
                        if (Actor != null)
                        {
                            Instance = RoomManager.GetInstanceByRoomId(Actor.CurrentRoomId);
                        }

                        int time = 0;
                        if (mWired[ActionItem.Id].Time != 0)
                        {
                            time = mWired[ActionItem.Id].Time * 100;
                            time = time % 2;
                            time = time * 10;
                        }

                        switch (WiredTypesUtil.EffectFromInt(ActionItem.Definition.BehaviorData))
                        {
                            #region reset_timer
                            case WiredEffectTypes.reset_timer:
                                // for every item in the room...
                                foreach (Item nItem in Instance.GetFloorItems())
                                {
                                    // if the item is a wired trigger
                                    if (nItem.Definition.Behavior == ItemBehavior.WiredTrigger)
                                    {
                                        //if the item is Trigger: At Set Time
                                        if (nItem.Definition.BehaviorData == 3)
                                        {
                                            //reset the timer
                                            nItem.RequestUpdate(nItem.WiredData.Data2);
                                        }
                                    }
                                }
                                break;
                            #endregion
                            #region give_clothes
                            case WiredEffectTypes.give_clothes:
                                if (Actor != null)
                                {
                                    string figure = Actor.Figure.ToString();
                                    int colorcode = 70;
                                    int Color = mWired[ActionItem.Id].Data2;
                                    if (Color == 1)
                                    {
                                        colorcode = 70;
                                    }
                                    else if (Color == 2)
                                    {
                                        colorcode = 85;
                                    }
                                    else if (Color == 3)
                                    {
                                        colorcode = 82;
                                    }
                                    else if (Color == 4)
                                    {
                                        colorcode = 1321;
                                    }
                                    if (Actor.Gender == Characters.CharacterGender.Male)
                                    {
                                        figure = "sh-290-" + colorcode + ".ch-215-" + colorcode + ".lg-275-" + colorcode + "";
                                    }
                                    else if (Actor.Gender == Characters.CharacterGender.Female)
                                    {
                                        figure = "sh-725-" + colorcode + ".ch-635-" + colorcode + ".lg-700-" + colorcode + "";
                                    }
                                    Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, FigToUniFig(Actor.Figure) + figure, Session.CharacterInfo.Gender, Actor.Motto, Session.CharacterInfo.Score));
                                    Session.CharacterInfo.NewFigure = FigToUniFig(Actor.Figure) + figure;
                                    Actor.NewFigure = FigToUniFig(Actor.Figure) + figure;
                                }
                                break;
                            #endregion
                            #region give_clothes_back
                            case WiredEffectTypes.give_clothes_back:
                                if (Actor == null)
                                {
                                    continue;
                                }
                                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                                {
                                    DataRow Row2 = MySqlClient.ExecuteQueryRow("SELECT * FROM characters WHERE id = '" + Actor.ReferenceId + "'");
                                    string figure = (string)Row2["figure"];
                                    Instance.BroadcastMessage(UserInfoUpdateComposer.Compose(Actor.Id, figure, Session.CharacterInfo.Gender, Actor.Motto, Session.CharacterInfo.Score));
                                    Session.CharacterInfo.NewFigure = figure;
                                    Actor.NewFigure = figure;
                                }
                                break;
                            #endregion
                            #region show_message
                            case WiredEffectTypes.show_message:
                                if (Actor == null)
                                {
                                    continue;
                                }
                                System.Threading.Thread.Sleep(time);
                                Actor.Whisper(mWired[ActionItem.Id].Data1, 0, true);
                                break;
                            #endregion
                            #region move_rotate
                            case WiredEffectTypes.move_rotate:
                                if (ActionItem.WiredData.Data2 == 0 && ActionItem.WiredData.Data3 == 0)
                                {
                                    continue;
                                }
                                System.Threading.Thread.Sleep(time);
                                String[] ItemsToMove = ActionItem.WiredData.Data1.Split('|');
                                foreach (String toMove in ItemsToMove)
                                {
                                    uint ItemId;
                                    uint.TryParse(toMove, out ItemId);
                                    Item Move = mInstance.GetItem(ItemId);
                                    if (Move == null)
                                    {
                                        continue;
                                    }
                                    Vector2 NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y);
                                    Vector2 OldPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y);

                                    switch (ActionItem.WiredData.Data2)
                                    {
                                        case 1:
                                            switch (rnd.Next(1, 5))
                                            {
                                                case 1:
                                                    NewPosition = new Vector2(Move.RoomPosition.X - 1, Move.RoomPosition.Y);
                                                    break;
                                                case 2:
                                                    NewPosition = new Vector2(Move.RoomPosition.X + 1, Move.RoomPosition.Y);
                                                    break;


                                                case 3:
                                                    NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y + 1);
                                                    break;


                                                case 4:
                                                    NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y - 1);
                                                    break;
                                            }
                                            break;
                                        case 2:
                                            if (rnd.Next(0, 2) == 1)
                                            {
                                                NewPosition = new Vector2(Move.RoomPosition.X - 1, Move.RoomPosition.Y);
                                            }
                                            else
                                            {
                                                NewPosition = new Vector2(Move.RoomPosition.X + 1, Move.RoomPosition.Y);
                                            }

                                            break;
                                        case 3:
                                            if (rnd.Next(0, 2) == 1)
                                            {
                                                NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y - 1);
                                            }
                                            else
                                            {
                                                NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y + 1);
                                            }

                                            break;
                                        case 4:
                                            NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y - 1);
                                            break;
                                        case 5:
                                            NewPosition = new Vector2(Move.RoomPosition.X + 1, Move.RoomPosition.Y);
                                            break;
                                        case 6:
                                            NewPosition = new Vector2(Move.RoomPosition.X, Move.RoomPosition.Y + 1);
                                            break;
                                        case 7:
                                            NewPosition = new Vector2(Move.RoomPosition.X - 1, Move.RoomPosition.Y);
                                            break;
                                    }

                                    int NewRotation = Move.RoomRotation;

                                    switch (ActionItem.WiredData.Data3)
                                    {
                                        case 1:
                                            NewRotation = NewRotation + 2;
                                            if (NewRotation == 8)
                                            {
                                                NewRotation = 0;
                                            }
                                            break;

                                        case 2:
                                            NewRotation = (NewRotation - 2);
                                            if (NewRotation == -2)
                                            {
                                                NewRotation = 6;
                                            }
                                            break;
                                        case 3:
                                            if (rnd.Next(0, 2) == 1)
                                            {
                                                goto case 1;
                                            }
                                            else
                                            {
                                                goto case 2;
                                            }
                                    }



                                    bool IsRotationOnly = (ActionItem.WiredData.Data2 == 0);
                                    Vector3 FinalizedPosition = mInstance.SetFloorItem(null, Move, NewPosition, NewRotation);
                                    Vector3 FinalizedOld = mInstance.SetFloorItem(null, Move, OldPosition, NewRotation);

                                    if (FinalizedPosition != null)
                                    {
                                        Move.MoveToRoom(null, mInstance.RoomId, FinalizedPosition, NewRotation, "");
                                        RoomManager.MarkWriteback(Move, false);

                                        mInstance.RegenerateRelativeHeightmap();
                                        mInstance.BroadcastMessage(RoomItemUpdatedComposer.Compose(Move));
                                        mInstance.BroadcastMessage(RollerEventComposer.Compose(FinalizedOld, FinalizedPosition, Item.Id, 0, Move.Id));
                                        ItemEventDispatcher.InvokeItemEventHandler(null, Move, mInstance, ItemEventType.Moved, IsRotationOnly ? 1 : 0);

                                    }
                                }
                                break;
                            #endregion
                            #region match_to_sshot
                            case WiredEffectTypes.match_to_sshot:
                                String[] Selected = ActionItem.WiredData.Data5.Split('+');
                                foreach (String FullData in Selected)
                                {
                                    System.Threading.Thread.Sleep(time);
                                    if (!FullData.Contains('#'))
                                    {
                                        continue;
                                    }

                                    String[] Data = FullData.Split('#');
                                    if (Data.Length != 4)
                                    {
                                        continue;
                                    }

                                    uint Id = uint.Parse(Data[0]);
                                    String[] Position = Data[1].Split('|');
                                    int Rotation = int.Parse(Data[2]);
                                    String Flags = Data[3];

                                    int X = int.Parse(Position[0]);
                                    int Y = int.Parse(Position[1]);
                                    uint Z = uint.Parse(Position[2]);

                                    Item AffectedItem = mInstance.GetItem(Id);

                                    if (AffectedItem == null)
                                    {
                                        continue;
                                    }

                                    Boolean IsRotationOnly = (X == AffectedItem.RoomPosition.X && Y == AffectedItem.RoomPosition.Y && Z == AffectedItem.RoomPosition.Z);

                                    Vector2 NewPosition = new Vector2(X, Y);
                                    Vector2 OldPosition = new Vector2(AffectedItem.RoomPosition.X, AffectedItem.RoomPosition.Y);

                                    if (ActionItem.WiredData.Data2 == 1)
                                    {
                                        AffectedItem.Flags = Flags;
                                        AffectedItem.DisplayFlags = Item.Flags;
                                        AffectedItem.BroadcastStateUpdate(mInstance);
                                    }

                                    if (ActionItem.WiredData.Data3 == 0)
                                    {
                                        Rotation = AffectedItem.RoomRotation;
                                    }

                                    if (ActionItem.WiredData.Data4 == 0)
                                    {
                                        NewPosition = AffectedItem.RoomPosition.GetVector2();
                                    }

                                    if (ActionItem.WiredData.Data4 == 1 || ActionItem.WiredData.Data3 == 1)
                                    {
                                        Vector3 FinalizedPosition = mInstance.SetFloorItem(null, AffectedItem, NewPosition, Rotation);
                                        AffectedItem.MoveToRoom(null, mInstance.RoomId, FinalizedPosition, Rotation, "");

                                        RoomManager.MarkWriteback(AffectedItem, false);

                                        mInstance.RegenerateRelativeHeightmap();
                                        mInstance.BroadcastMessage(RoomItemUpdatedComposer.Compose(AffectedItem));

                                        ItemEventDispatcher.InvokeItemEventHandler(null, AffectedItem, mInstance, ItemEventType.Moved, IsRotationOnly ? 1 : 0);
                                    }
                                    else if (ActionItem.WiredData.Data2 == 1)
                                    {
                                        RoomManager.MarkWriteback(AffectedItem, true);
                                    }
                                }
                                break;
                            #endregion
                            #region teleport_to_furni
                            case WiredEffectTypes.teleport_to:
                                if (Actor == null)
                                {
                                    continue;
                                }
                                System.Threading.Thread.Sleep(time);
                                String[] Selected2 = ActionItem.WiredData.Data1.Split('|');
                                String ItemIdS = Actor.FurniOnId.ToString();

                                while (Actor.FurniOnId.ToString() == ItemIdS)
                                {
                                    ItemIdS = Selected2[rnd.Next(0, Selected2.Length)];
                                }

                                uint ItemId2;
                                uint.TryParse(ItemIdS, out ItemId2);
                                Item AffectedItem2 = mInstance.GetItem(ItemId2);
                                if (AffectedItem2 == null)
                                {
                                    continue;
                                }
                                Actor.BlockWalking();
                                int OldEffect = Actor.AvatarEffectId;
                                Actor.ApplyEffect(4);
                                Actor.PositionToSet = AffectedItem2.RoomPosition.GetVector2();
                                Actor.MoveToPos(AffectedItem2.RoomPosition.GetVector2(), true, false, true);
                                Actor.UpdateNeeded = true;
                                Actor.UnblockWalking();
                                System.Threading.Thread.Sleep(1000);
                                Actor.ApplyEffect(OldEffect);
                                break;
                            #endregion
                            #region toggle_furni_state
                            case WiredEffectTypes.toggle_state:
                                String[] Selected3 = ActionItem.WiredData.Data1.Split('|');

                                System.Threading.Thread.Sleep(time);
                                foreach (String ItemIdS2 in Selected3)
                                {
                                    uint ItemId3;
                                    uint.TryParse(ItemIdS2, out ItemId3);
                                    Item AffectedItem3 = mInstance.GetItem(ItemId3);
                                    if (AffectedItem3 == null)
                                    {
                                        continue;
                                    }

                                    int CurrentState = 0;
                                    int.TryParse(AffectedItem3.Flags, out CurrentState);

                                    int NewState = CurrentState + 1;

                                    if (CurrentState < 0 || CurrentState >= (AffectedItem3.Definition.BehaviorData - 1))
                                    {
                                        NewState = 0;
                                    }
                                    if (AffectedItem3.Definition.Behavior == ItemBehavior.Fireworks)
                                    {
                                        int CurrentCharges = 0;
                                        int.TryParse(AffectedItem3.Flags, out CurrentCharges);
                                        if (AffectedItem3.DisplayFlags == "2")
                                        {
                                        }
                                        else if (CurrentCharges > 0)
                                        {
                                            AffectedItem3.DisplayFlags = "2";
                                            AffectedItem3.BroadcastStateUpdate(mInstance);

                                            AffectedItem3.Flags = (--CurrentCharges).ToString();
                                            RoomManager.MarkWriteback(AffectedItem3, true);

                                            AffectedItem3.RequestUpdate(AffectedItem3.Definition.BehaviorData);
                                        }
                                    }
                                    else if (AffectedItem3.Definition.Behavior == ItemBehavior.HoloDice || AffectedItem3.Definition.Behavior == ItemBehavior.Dice)
                                    {
                                        AffectedItem3.Flags = "-1";
                                        AffectedItem3.DisplayFlags = "-1";

                                        AffectedItem3.BroadcastStateUpdate(mInstance);
                                        AffectedItem3.RequestUpdate(3);
                                    }
                                    else if (AffectedItem3.Definition.Behavior == ItemBehavior.TraxPlayer)
                                    {
                                        if (mInstance.MusicController.IsPlaying)
                                        {
                                            mInstance.MusicController.Stop();
                                            mInstance.MusicController.BroadcastCurrentSongData(mInstance);
                                            AffectedItem3.DisplayFlags = "0";
                                            AffectedItem3.BroadcastStateUpdate(mInstance);
                                        }
                                        else
                                        {
                                            if (mInstance.MusicController.PlaylistSize > 0)
                                            {
                                                mInstance.MusicController.Start();
                                                AffectedItem3.DisplayFlags = "1";
                                                AffectedItem3.BroadcastStateUpdate(mInstance);
                                            }
                                            else
                                            {
                                                AffectedItem3.DisplayFlags = "0";
                                                AffectedItem3.BroadcastStateUpdate(mInstance);
                                            }
                                        }
                                    }
                                    else if (CurrentState != NewState)
                                    {
                                        AffectedItem3.Flags = NewState.ToString();
                                        AffectedItem3.DisplayFlags = AffectedItem3.Flags;

                                        RoomManager.MarkWriteback(AffectedItem3, true);

                                        AffectedItem3.BroadcastStateUpdate(mInstance);
                                    }
                                    Instance.WiredManager.HandleToggleState(Actor, AffectedItem3);
                                }
                                break;
                            #endregion
                        }
                    }
                }
            }
            catch (StackOverflowException e)
            {
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                Output.WriteLine("Error in Wired Action: " + e.Message);
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Error in Wired Action: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.Notification + "\n\n");
            }
        }

    }
}

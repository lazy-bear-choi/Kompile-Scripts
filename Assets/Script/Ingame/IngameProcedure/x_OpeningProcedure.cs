//namespace Script.Content
//{
//    using Script.Manager;
//    using Script.Index;
//    using Script.Interface;
//    using Script.IngameMessage;
//    using System.Threading.Tasks;

//    public partial class x_OpeningProcedure : IngameProcedureBase, IMessageReceiver
//    {
//        public x_OpeningProcedure() : base()
//        {
//            procedureType = IngameProcedureType.OPENING;
//        }
//        public override async Task<bool> Start()
//        {
//            return await ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
//        }

//        protected override async Task<bool> ExecuteIngameEventAsync(IngameEventType messageType)
//        {
//            switch (messageType)
//            {
//                case IngameEventType.OPENING_INSTANTIATE_TITLE:
//                    UITitleObject titleObject = await GetIngameObjectAsync<UITitleObject>(AssetCode.OP_TitleObject, IngameManager.UIOverayRootTransform);
//                    ingameObjects.Add(new(AssetCode.OP_TitleObject, titleObject.gameObject));
//                    break;
//                case IngameEventType.OPENING_LOAD_TITLE_MENU:
//                    var uiTitleMenuObject = await GetIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject, IngameManager.UIOverayRootTransform);
//                    ingameObjects.Add(new(AssetCode.UI_TitleMenuObject, uiTitleMenuObject.gameObject));
//                    break;
//                case IngameEventType.OPENING_SELECT_NEW_GAME:
//                    IngameManager.AddIngameProcedure(IngameProcedureType.NEW_GAME);
//                    break;
//                default:
//                    return false;
//            }

//            return true;
//        }

//        public async Task<bool> ReceiveIngameMessage<T>(T data) where T : struct
//        {
//            if (data is OnSelect_UITitleMenu onSelectMenu)
//            {
//                var menuType = onSelectMenu.ValueInt;
//                IngameEventType next_event_type;

//                switch (menuType)
//                {
//                    case 0: next_event_type = IngameEventType.OPENING_SELECT_NEW_GAME;  break;
//                    case 1: next_event_type = IngameEventType.OPENING_SELECT_LOAD_GAME; break;
//                    case 2: next_event_type = IngameEventType.OPENING_SELECT_OPTION;    break;
//                    case 3: next_event_type = IngameEventType.OPENING_SELECT_EXIT;      break;
//                    default:
//                        return false;
//                }

//                return await ExecuteIngameEventAsync(next_event_type);
//            }
//            if (data is OnEndEvent onEndEvent)
//            {
//                if (IngameEventType.OPENING_INSTANTIATE_TITLE == onEndEvent.EventType)
//                {
//                    return await ExecuteIngameEventAsync(IngameEventType.OPENING_LOAD_TITLE_MENU);
//                }
//            }

//            return false;
//        }
//    }
//}
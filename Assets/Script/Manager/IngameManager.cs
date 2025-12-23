namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Data;

    public partial class IngameManager : MonoBehaviour
    {
        private static IngameManager    instance;
        private static PlayData         playData;

        private static InputHandler     inputHandler;
        private static IngameCamera     ingameCam;

        private static Transform[]  canvasTransforms;
        private static Transform    maptRootTransform;
        private static Transform    unitRootTransform;

        private static ContentStateMachine contentStateMachine;

        public static Transform UnitRootTransform       => unitRootTransform;
        public static Transform MapRootTransform        => maptRootTransform;
        public static Transform UICameraRootTransform   => canvasTransforms[(int)CanvasType.CAMERA];
        public static Transform UIOverayRootTransform   => canvasTransforms[(int)CanvasType.OVERLAY];


        // camera
        public static void InitFollowingCamera(IngameUnitBase player_character)
        {
            ingameCam.InitFollowingCamera(player_character);
        }


        private void Awake()
        {
            // init instance
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            contentStateMachine = new ContentStateMachine();

            // get asset
            AssetManagerV2.Initialize();

            canvasTransforms = new Transform[3];
            Transform uiParent = transform.Find("UI");
            for (int i = 0; i < canvasTransforms.Length; ++i)
            {
                canvasTransforms[i] = uiParent.GetChild(i);
            }

            //maptRootTransform = transform.Find("Map").transform;
            //unitRootTransform = transform.Find("Unit").transform;
        }

        private void Start()
        {
            contentStateMachine.ChangeState(new OpeningContent());
            inputHandler = new InputHandler();
        }
    }
}
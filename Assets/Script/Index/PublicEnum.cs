using System;

public enum EAnimeCodeToString
{ 
    NONE,

    IDLE_FRONT,
    IDLE_BACK,
    IDLE_LEFT,
    IDLE_RIGHT,

    MOVE_FRONT,
    MOVE_BACK,
    MOVE_LEFT,
    MOVE_RIGHT,
}
public enum UpdaterType
{ 
    UPDATE          = 0,
    FIXED_UPDATE,
    LATE_UPDATE,

    INPUT
}
public enum IngameEventType
{
    NONE,

    GET_ASSET,
    END_OBJECT_PROCESS,
    SELECT_ITEM,

    // LOADING
    LOADING_PROCEED,
    LOADING_CURTAIN_ON,
    LOADING_CURTAIN_OFF,

    // OPENING
    OPENING_INSTANTIATE_TITLE,
    OPENING_LOAD_TITLE_MENU,
    OPENING_SELECT_NEW_GAME,
    OPENING_SELECT_LOAD_GAME,
    OPENING_SELECT_OPTION,
    OPENING_SELECT_EXIT,
    OPENING_DISPOSE,

    // NEW_GAME
    NEWGAME_INIT_PLAYER,
    NEWGAME_INITIALIZE,
    NEWGAME_DISPOSE,

    // FIELD
    FIELD_INIT,

}

#if UNITY_EDITOR
[Flags]
public enum DirFlag
{
    NONE  = 0,
    UP    = 1 << 0,
    DOWN  = 1 << 1,
    LEFT  = 1 << 2,
    RIGHT = 1 << 3
}
#endif
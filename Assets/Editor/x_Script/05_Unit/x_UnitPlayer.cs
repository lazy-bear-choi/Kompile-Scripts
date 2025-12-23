//using System.Collections.Generic;
//using UnityEngine;
//using Script.Util;

//public class UnitPlayer : UnitBase
//{
//    //자동 방향 전환 시 탐색 각도. 시계방향 기준
//    private static  readonly float[] INTERVAL_ROTATION  = new float[] { 0, 45f, -45f, 90f, -90f }; 
//    private static readonly float   SPEED_MOVE = 3f;

//    private EAnimeCodeToString mAnimeCurrentCode = EAnimeCodeToString.IDLE_FRONT;
//    private Vector3 mDirectionBefore = new Vector3(-1f, 0, -1f);
//    private float   mScale = 1f;
//    private int     mLayer = 0;

//    public void Move(Dictionary<int, STile> map, Vector3 dirInput)
//    {
//        Vector3 position = transform.position;
//        //직전 이동 방향과 같은 방향이면 시계 방향으로, 그렇지 않다면 반시계 방향으로 탐색한다.
//        float sign = Mathf.Sign(Vector3.Cross(dirInput, mDirectionBefore).y) >= 0 ? 1f : -1f;
//        for (int i = 0; i < INTERVAL_ROTATION.Length; ++i)
//        {
//            //입력 방향을 회전시킨다.
//            Vector3 dirRotated = Quaternion.Euler(0f, sign * INTERVAL_ROTATION[i], 0f) * dirInput;
//            dirRotated.Normalize();

//            dirRotated *= Time.fixedDeltaTime * SPEED_MOVE * mScale;
//            Vector3 goal = (position + dirRotated).Truncate();

//            int keyGoal = TileUtility.GetKeyByPoint(mLayer, /*Vector3*/ goal, mScale);
//            keyGoal = TileUtility.GetKeyByRelativeCoord(map, keyGoal, x: 0, z: 0);
//            if (-1 == keyGoal)
//            {
//                continue;
//            }

//            //목표 지점에 타일이 존재하는가?
//            if (false == map.TryGetValue(keyGoal, out STile tileGoal))
//            {
//                return;
//            }

//            //감지 대상 삼각형을 배열에 저장한다.
//            Vector3 pivot = TileUtility.GetPivotByPoint(goal, mScale);
//            int triangePoint = TileUtility.GetTriangleIndex(goal - pivot, mScale * 0.5f);
//            TileUtility.SetTriangleArray(map, triangePoint, keyGoal, pivot, mScale);

//            //UnitPlayer.Move() : 이동 가능할 때의 처리
//            if (true == TileUtility.IsMovable(map, goal, mScale))
//            {
//                //위치 변경
//                float y = tileGoal.GetYValue(keyGoal, goal);
//                goal = new Vector3(goal.x, y, goal.z);
//                transform.position = goal;

//                //트리거 호출
//                if (true == tileGoal.HasTrigger(ETileTriggerType.Scale, out int flagScale))
//                {
//                    mScale = (flagScale == 1) ? 0.5f : 1f;
//                    Main.Cam.SetFOV(mScale);
//                    transform.localScale = Vector3.one * mScale;
//                }
//                if (true == tileGoal.HasTrigger(ETileTriggerType.Layer, out int layer))
//                {
//                    this.mLayer = layer;
//                    Main.Instance.SetFieldLayer(layer);
//                }
//                //필드 이벤트는 아직 미구현
//                //if (true == tileMy.HasTrigger(TileTrigger.Event, out int code))
//                //{
//                //  //call event
//                //}

//                int flag = 0;
//                if      (0 < dirRotated.x) { flag += 10; }
//                else if (0 > dirRotated.x) { flag += 20; }
//                if      (0 < dirRotated.z) { flag +=  1; }
//                else if (0 > dirRotated.z) { flag +=  2; }

//                EAnimeCodeToString anime;
//                switch (flag)
//                {
//                    case 01: anime = EAnimeCodeToString.MOVE_BACK; break;
//                    case 20: anime = EAnimeCodeToString.MOVE_LEFT; break;
//                    case 10: anime = EAnimeCodeToString.MOVE_RIGHT; break;
//                    case 02: anime = EAnimeCodeToString.MOVE_FRONT; break;
//                    //TODO: 8방향을 희망하나 아직 4방향 스프라이트 시트밖에 없다...
//                    default: anime = EAnimeCodeToString.NONE; break;
//                }

//                if (anime != mAnimeCurrentCode)
//                {
//                    PlayAnime(anime);
//                    mAnimeCurrentCode = anime;
//                }

//                //직전 입력 방향 갱신
//                float x = (0 != dirRotated.x) ? dirRotated.x : mDirectionBefore.x;
//                float z = (0 != dirRotated.z) ? dirRotated.z : mDirectionBefore.z;
//                mDirectionBefore = new Vector3(x, y, z);
//                return;
//            }
//        }

//        //만약 이동불가한 경우가 있다면? 부동 소수점 기준을 1자리 올린다. (소수점 3자리 -> 소수점 2자리)
//        transform.position = TileUtility.SnappingPoint(position, Time.fixedDeltaTime * 10f, 2); ;
//    }
//    public void StopMove()
//    {
//        EAnimeCodeToString anime;
//        switch (mAnimeCurrentCode)
//        {
//            case EAnimeCodeToString.MOVE_FRONT: anime = EAnimeCodeToString.IDLE_FRONT; break;
//            case EAnimeCodeToString.MOVE_BACK:  anime = EAnimeCodeToString.IDLE_BACK;  break;
//            case EAnimeCodeToString.MOVE_LEFT:  anime = EAnimeCodeToString.IDLE_LEFT;  break;
//            case EAnimeCodeToString.MOVE_RIGHT: anime = EAnimeCodeToString.IDLE_RIGHT; break;
//            default: anime = EAnimeCodeToString.NONE; break;
//        }

//        PlayAnime(anime);
//        mAnimeCurrentCode = anime;
//    }
//}


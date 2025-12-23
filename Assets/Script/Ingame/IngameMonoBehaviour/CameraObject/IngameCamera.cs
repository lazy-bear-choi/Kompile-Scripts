using UnityEngine;
using Script.Interface;
using Script.Index;

public class IngameCamera : IngameMonoBehaviourBase, IIngameLateUpdater
{
    private readonly Vector3 OFFSET = new Vector3(0f, 3f, -2f);
    private readonly Quaternion ROTATION = Quaternion.Euler(50f, 0f, 0f);

    private Camera mainCam;
    private Transform target;

    private void Awake()
    {
        mainCam = transform.GetComponent<Camera>();
    }
    public void InitFollowingCamera(IngameUnitBase player_character)
    {
        enabled = true;

        target = player_character.transform;

        transform.position = player_character.Position + OFFSET;
        transform.SetPositionAndRotation(OFFSET, ROTATION);

        mainCam.fieldOfView = 60f;
    }

    public IngameUpdateState LateUpdateState()
    {
        if (null == target)
        {
            return IngameUpdateState.FAILURE;
        }

        transform.position = target.transform.position + OFFSET;
        return IngameUpdateState.RUNNING;
    }

    //public override void Release()
    //{
    //    //throw new System.NotImplementedException();
    //}
}

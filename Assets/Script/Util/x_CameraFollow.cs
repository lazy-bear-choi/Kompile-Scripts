using UnityEngine;

public class x_CameraFollow : MonoBehaviour
{
    private readonly Vector3    OFFSET   = new Vector3(0f, 3f, -2f);
    private readonly Quaternion ROTATION = Quaternion.Euler(50f, 0f, 0f);

    private Camera     mMainCam;
    private Transform  mTarget;

    private void Awake() 
    {
        mMainCam = transform.GetComponent<Camera>();
        enabled = false;
    }
    //public void SetFollow(Transform target)
    //{
    //    mTarget = target;
    //    transform.SetPositionAndRotation(OFFSET, ROTATION);
    //    enabled = true;
    //}
    //public void SetFOV(float scale)
    //{
    //    mMainCam.fieldOfView = 60f * scale;
    //}
    //public void StopFollow()
    //{
    //    enabled = false;
    //}

    //private void LateUpdate() 
    //{
    //    transform.position = mTarget.position + OFFSET;
    //}
}

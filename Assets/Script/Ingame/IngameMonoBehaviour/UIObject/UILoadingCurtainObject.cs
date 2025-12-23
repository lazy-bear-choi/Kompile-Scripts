using Script.Index;
using Script.Interface;
using Script.Manager;
using Script.IngameMessage;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingCurtainObject : IngameMonoBehaviourBase, IIngameUpdater
{
    private Image image;
    
    private float delta;
    private float alpha;

    private void Awake()
    {
        image = transform.GetComponent<Image>();
        enabled = false;
    }
    public void On(bool on)
    {
        if (true == on)
        {
            alpha = float.Epsilon;
            delta = 1f;
        }
        else
        {
            alpha = 1f;
            delta = -1.5f;
        }

        enabled = true;
    }
    public IngameUpdateState UpdateState()
    {
        // idea: 이걸 sin 함수로 구현할 순 없나?
        alpha = System.Math.Clamp(alpha + delta * Time.deltaTime, float.Epsilon, 1f);
        image.color = new Color(0f, 0f, 0f, alpha);

        if (alpha <= float.Epsilon)
        {
            MessageManager.Publish(new OnEndEvent(IngameEventType.LOADING_CURTAIN_OFF));
            enabled = false;
            return IngameUpdateState.SUCCESS;
        }
        else if (alpha >= 1)
        {
            MessageManager.Publish(new OnEndEvent(IngameEventType.LOADING_CURTAIN_ON));
            enabled = false;
        }

        return IngameUpdateState.RUNNING;
    }

    //public override void Release()
    //{
    //    //throw new System.NotImplementedException();
    //}
}

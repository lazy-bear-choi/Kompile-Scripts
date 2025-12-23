using Script.Manager;
using System;
using UnityEngine;
using UnityEngine.UI;
using Script.Index;
using Script.IngameMessage;
using static Script.Index.IDxInput;
using Script.Interface;

public class UITitleMenuObject : MonoBehaviour, IContentUpdater
{   
    [SerializeField] private Transform menuParent;
    [SerializeField] private Image selectSlotImage;

    private readonly float minAlpha   = 0.3f;
    private readonly float maxAlpha   = 0.7f;
    private readonly float alphaDelta = 0.5f;
    private readonly float waitTime   = 0.125f;

    private Vector2[] anchoredPositions;
    private float alpha;
    private float sign;

    private float lastInputTime;
    private int   index;

    private void Awake()
    {
        anchoredPositions = new Vector2[menuParent.childCount];
        for (int i = 0; i < anchoredPositions.Length; ++i)
        {
            anchoredPositions[i] = menuParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
        }
        menuParent = null; // 사용을 마침

        alpha = minAlpha;
        sign  = 1f;

        index = 0;
        lastInputTime = 0;
    }

    public void OnUpdate()
    {
        alpha += sign * Time.deltaTime * alphaDelta;

        if (alpha >= maxAlpha)
        {
            alpha = maxAlpha;
            sign = -1f;
        }
        else if (alpha <= minAlpha)
        {
            alpha = minAlpha;
            sign = 1f;
        }

        selectSlotImage.color = new Color(0.2232704f, 0.5052339f, 1f, alpha);
    }

    public bool ReceiveInput(InputFlag inputFlag)
    {
        if (true == inputFlag.Contains(InputFlag.ENTER | InputFlag.ACTION))
        {
            selectSlotImage.color = new Color(0.2232704f, 0.5052339f, 1f, 1f);
            MessageManager.Publish(new OnSelect_UITitleMenu(index)); //이벤트 방식으로 처리했음
            return true;
        }

        if (Time.time < lastInputTime + waitTime)
        {
            return true;
        }

        lastInputTime = Time.time;

        if (true == inputFlag.Contains(InputFlag.UP))
        {
            index = ((index - 1) + 4) % 4;
            selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
        }
        if (true == inputFlag.Contains(InputFlag.DOWN))
        {
            index = ((index + 1) + 4) % 4;
            selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
        }

        return true;
    }

}

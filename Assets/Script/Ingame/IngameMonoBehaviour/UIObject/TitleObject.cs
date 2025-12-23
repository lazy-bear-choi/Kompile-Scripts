using Script.Index;
using Script.Interface;
using Script.Manager;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using static Script.Index.IDxInput;

public class TitleObject : IngameMonoBehaviourBase, IInputReceiver
{
    private float DeltaTime => Time.deltaTime;

    [SerializeField] private UITitleMenuObject titleMenu;
    [SerializeField] private Image[] images;

    // play company logo
    private readonly float logoAlphaDelta = 0.625f;
    private float alpha;
    private float waitTime;

    // play title
    private readonly float movingSpeed = 4000f;
    private readonly float movingTime = 0.75f;
    private readonly float flashDelta = 3f;

    private RectTransform[] rects;
    private Vector2[] titleInitPositions;
    private float movingDist;
    private float passedTime;

    private CancellationTokenSource skipToken;

    private enum ImageType
    {
        COMPANY_LOGO,
        // DEMO_PLAY
        TITLE_LOGO_UPPER,
        TITLE_LOGO_LOWER,
        TITLE_FLASH
    }

    private void Awake()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);

        titleMenu.gameObject.SetActive(false);

        Color initColor = new Color(1f, 1f, 1f, 0f);
        images[(int)ImageType.COMPANY_LOGO].color = initColor;
        images[(int)ImageType.TITLE_LOGO_LOWER].color = initColor;
        images[(int)ImageType.TITLE_LOGO_UPPER].color = initColor;
        images[(int)ImageType.TITLE_FLASH].color = initColor;
    }

    private void Start()
    {
        InputHandler.AddInputReceiver(this);
    }

    public bool ReceiveInput(IDxInput.InputFlag flag)
    {
        if (false == flag.Contains(IDxInput.InputFlag.ACTION | IDxInput.InputFlag.ENTER))
        {
            return false;
        }
        if (null == skipToken)
        {
            return false;
        }

        skipToken.Cancel();
        return true;
    }


    public async Awaitable PlayLogoSequence()
    {
        skipToken = new CancellationTokenSource();
        //skipToken.Token.ThrowIfCancellationRequested();

        transform.GetChild(0).gameObject.SetActive(true);

        alpha = 0f;
        while (alpha < 1f)
        {
            alpha += DeltaTime * logoAlphaDelta;
            images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);
            await Awaitable.NextFrameAsync(skipToken.Token);
        }


        waitTime = 0f;
        while (waitTime < 1f)
        {
            waitTime += DeltaTime;
            await Awaitable.NextFrameAsync(skipToken.Token);

        }

        alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= DeltaTime * logoAlphaDelta;
            images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);
            await Awaitable.NextFrameAsync(skipToken.Token);
        }
        alpha = 0f;

        skipToken.Dispose();
        skipToken = null;
    }
    public async Awaitable ExitLogoSequence()
    {
        skipToken.Dispose();
        skipToken = null;

        alpha = 1f;
        while (0 > alpha)
        {
            alpha -= DeltaTime * logoAlphaDelta;
            images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);
            await Awaitable.NextFrameAsync();
        }
    }

    public async Awaitable PlayTitleSequence()
    {
        transform.GetChild(2).gameObject.SetActive(true);
        passedTime = 0f;
        alpha = 0;
        waitTime = 0f;

        // get RectTransform
        rects = new RectTransform[2];
        rects[0] = images[(int)ImageType.TITLE_LOGO_UPPER].GetComponent<RectTransform>();
        rects[1] = images[(int)ImageType.TITLE_LOGO_LOWER].GetComponent<RectTransform>();

        // set anchored position
        movingDist = movingSpeed * movingTime;
        titleInitPositions = new Vector2[2];

        Vector2 anchoredPosition = rects[0].anchoredPosition;
        titleInitPositions[0] = new Vector2(anchoredPosition.x, anchoredPosition.y + movingDist);
        rects[0].anchoredPosition = titleInitPositions[0];

        anchoredPosition = rects[1].anchoredPosition;
        titleInitPositions[1] = new Vector2(anchoredPosition.x, anchoredPosition.y - movingDist);
        rects[1].anchoredPosition = titleInitPositions[1];

        // set color
        images[(int)ImageType.TITLE_LOGO_UPPER].color = new Color(1f, 1f, 1f, 1f);
        images[(int)ImageType.TITLE_LOGO_LOWER].color = new Color(1f, 1f, 1f, 1f);
        images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, 0f);

        // move title logo
        float ratio;
        do
        {
            passedTime += DeltaTime;
            ratio = Math.Clamp(passedTime / movingTime, 0f, 1f);
            rects[0].anchoredPosition = titleInitPositions[0] - new Vector2(0, movingDist * ratio);
            rects[1].anchoredPosition = titleInitPositions[1] + new Vector2(0, movingDist * ratio);
            await Awaitable.NextFrameAsync();
        }
        while (ratio < 1f);


        // flash
        while (alpha < 1f)
        {
            alpha += DeltaTime * flashDelta;
            images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);
            await Awaitable.NextFrameAsync();
        };
        alpha = 1f;

        while (waitTime < 0.25f)
        {
            waitTime += DeltaTime;
            await Awaitable.NextFrameAsync();
        };

        while (alpha > 0f)
        {
            alpha -= DeltaTime * flashDelta * 1.125f;
            images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);
            await Awaitable.NextFrameAsync();
        };
    }

    public UITitleMenuObject SetActiveTitleMenu()
    {
        InputHandler.RemoveInputReceiver(this);

        titleMenu.gameObject.SetActive(true);
        return titleMenu;
    }
}
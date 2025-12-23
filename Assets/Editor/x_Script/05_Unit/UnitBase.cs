using UnityEngine;
using static EAnimeCodeToString;

public abstract class UnitBase /* : MonoBehaviour */
{
    protected Transform transform;
    public  Transform Transform { get => transform; }

    protected AnimationClip[] mAnimationClips;
    protected Animator        mAnimator;
    protected int             mIndexUnit;

    public void Awake(int indexUnit, Transform transform)
    {
        this.transform = transform;
        this.mIndexUnit = indexUnit;
        mAnimator = transform.GetComponent<Animator>();

    }
    public void SetAnimeController(RuntimeAnimatorController controller)
    {
        mAnimator.runtimeAnimatorController = controller;
        PlayAnime(IDLE_FRONT);
    }
    protected void PlayAnime(EAnimeCodeToString code)
    {
        string anime = null;
        switch (code)
        {
            default:
                anime = code.ToString();
                break;
            case NONE:
                break;
        }

        mAnimator.Play(anime, 0);
    }
    //public bool Release()
    //{
    //    //TODO: 오브젝트 풀링을 해야 할까?
    //    GameObject.Destroy(transform.gameObject);

    //    string address = AssetManager.GetAssetAddress(EAssetType.AnimCtrl, mIndexUnit);
    //    return AssetManager.ReleaseAsset(address);
    //}
}


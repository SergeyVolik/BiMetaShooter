using UnityEngine;
using System.Collections;

public class bl_WeaponAnimation : MonoBehaviour
{
    public AnimationType m_AnimationType = AnimationType.Animation;

    public AnimationClip DrawName;
    public AnimationClip TakeOut;
    public AnimationClip SoloFireClip;
    public AnimationClip[] FireAnimations;
    public AnimationClip FireAimAnimation;
    public AnimationClip ReloadName;
    public AnimationClip IdleClip;
    [Range(0.1f,5)]public float FireSpeed = 1.0f;
    [Range(0.1f, 5)] public float DrawSpeed = 1.0f;
    [Range(0.1f, 5)] public float HideSpeed = 1.0f;
    [Header("ShotGun/Sniper")]
    public AnimationClip StartReloadAnim;
    public AnimationClip InsertAnim;
    public AnimationClip AfterReloadAnim;
    [Range(0.1f, 5)] public float InsertSpeed = 1.0f;
    [Header("Others")]
    public AnimationClip QuickFireAnim;
    public ParticleSystem[] Particles;
    [Range(1, 10)] public float ParticleRate = 5;
    public bool HasParticles = false;

    public bool AnimatedMovements = false;
    public bool DrawAfterFire = false;

    [Header("Audio")]
    private AudioSource m_source;
    public AudioClip Reload_1;
    public AudioClip Reload_2;
    public AudioClip Reload_3;
    public AudioClip m_Fire;

    //private
    private int m_repeatReload;
    public bl_Gun ParentGun { get; set; }
    private bl_GunManager GunManager;
    private bool cancelReload = false;
    private bl_WeaponBob GunBob;
    private PlayerState TempState = PlayerState.Idle;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if(ParentGun == null) { ParentGun = transform.parent.GetComponent<bl_Gun>(); }
        GunManager = transform.root.GetComponentInChildren<bl_GunManager>();
        GunBob = GunManager.GetComponent<bl_WeaponBob>();
        m_source = GetComponent<AudioSource>();
        if(m_source == null)
        {
            m_source = gameObject.AddComponent<AudioSource>();
            m_source.playOnAwake = false;
        }
    }

   void OnEnable()
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            Anim.wrapMode = WrapMode.Once;
        }
        else
        {
            if (AnimatedMovements) { GunBob.AnimatedThis(UpdateAnimated, true); } else { GunBob.AnimatedThis(null, false); }
        }
    }

    void UpdateAnimated(PlayerState playerState)
    {
        if (ParentGun != null && (ParentGun.FPState == PlayerFPState.Idle || ParentGun.FPState == PlayerFPState.Running))
        {
            bool force = TempState == PlayerState.Jumping;
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                if (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).IsName("Fire")) return;
            }
            if (playerState == PlayerState.Running)
            {
                if (TempState != PlayerState.Running)
                {
                    animator.speed = 1;
                    if (!force) { animator.CrossFade("Run", 0.2f); }
                    else { animator.Play("Run", 0, 0); }
                    TempState = PlayerState.Running;
                }
            }
            else if (playerState == PlayerState.Walking)
            {
                if (TempState != PlayerState.Walking)
                {
                    animator.speed = 1;
                    if (!force)
                    { animator.CrossFade("Walk", 0.2f); }
                    else { animator.Play("Walk", 0, 0); }
                    TempState = PlayerState.Walking;
                }
            }
            else
            {
                if (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).IsName("Fire")) return;
                if (TempState != PlayerState.Idle)
                {
                    animator.speed = 1;
                    if (!force)
                    { animator.CrossFade("Idle", 0.2f); }
                    else { animator.Play("Idle", 0, 0); }
                    TempState = PlayerState.Idle;
                }
            }
        }
        else
        {
            TempState = PlayerState.Jumping;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public float Fire()
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            if (FireAnimations.Length <= 0)
                return 0;

            int id = Random.Range(0, FireAnimations.Length);
            if (FireAnimations[id] == null) { id = 0; }
            string n = FireAnimations[id].name;
            Anim.Rewind(n);
            Anim[n].speed = FireSpeed;
            Anim.Play(n);

            return FireAnimations[id].length / FireSpeed;
        }
        else
        {
            animator.Play("Fire", 0, 0);
            animator.speed = FireSpeed;
            return SoloFireClip.length / FireSpeed;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public float KnifeFire(bool Quickfire)
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            if (FireAimAnimation == null)
                return 0;

            string an = Quickfire ? QuickFireAnim.name : FireAimAnimation.name;
            Anim.Rewind(an);
            Anim[an].speed = FireSpeed;
            Anim.Play(an);

            return Anim[an].length / FireSpeed;
        }
        else
        {
            string an = Quickfire ? "QuickFire" : "Fire";
            animator.Play(an, 0, 0);
            animator.speed = FireSpeed;
            return SoloFireClip.length / FireSpeed;
        }
    }

    public float FireGrenade(bool fastFire)
    {
        if (fastFire)
        {
            StartCoroutine(FastGrenade());
            return GetFireLenght + (DrawName.length / DrawSpeed);
        }
        else
        {
            if (m_AnimationType == AnimationType.Animation)
            {
                return AimFire();
            }
            else
            {
                animator.Play("Fire", 0, 0);
                animator.speed = FireSpeed;
                float t = SoloFireClip.length / FireSpeed;
                if (DrawAfterFire) { Invoke("DrawWeapon", t + ParentGun.Info.ReloadTime +  0.1f); }
                return t;
            }
        }
    }

    IEnumerator FastGrenade()
    {
        yield return new WaitForSeconds(DrawWeapon());
        AimFire();
    }

    public void ThrowProjectile()
    {
        StartCoroutine(ParentGun.ThrowGrenade(false, false));
    }

    /// <summary>
    /// 
    /// </summary>
    public float AimFire()
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            if (FireAimAnimation == null)
                return 0;

            Anim.Rewind(FireAimAnimation.name);
            Anim[FireAimAnimation.name].speed = FireSpeed;
            Anim.Play(FireAimAnimation.name);

            return FireAimAnimation.length / FireSpeed;
        }
        else
        {
            animator.Play("AimFire", 0, 0);
            animator.speed = FireSpeed;
            return FireAimAnimation.length / FireSpeed;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public float DrawWeapon()
    {
        if (DrawName == null)
            return 0;

        if (m_AnimationType == AnimationType.Animation)
        {
            Anim.Rewind(DrawName.name);
            Anim[DrawName.name].speed = DrawSpeed;
            Anim[DrawName.name].time = 0;
            Anim.Play(DrawName.name);

            return Anim[DrawName.name].length / DrawSpeed;
        }
        else
        {
            animator.speed = DrawSpeed;
            animator.Play("Draw", 0, 0);
            return DrawName.length / DrawSpeed;
        }
    }
    /// <summary>
    ///  
    /// </summary>
    public float HideWeapon()
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            if (TakeOut == null)
                return 0;

            Anim[TakeOut.name].speed = HideSpeed;
            Anim[TakeOut.name].time = 0;
            Anim[TakeOut.name].wrapMode = WrapMode.Once;
            Anim.Play(TakeOut.name);
        }
        else
        {
            animator.Rebind();
            animator.CrossFade("Hide", 0.25f);
            animator.speed = HideSpeed;
        }
        return TakeOut == null ? 0 : TakeOut.length / HideSpeed;
    }
    /// <summary>
    /// event called by animation when is a reload state
    /// </summary>
    /// <param name="ReloadTime"></param>
    public void Reload(float ReloadTime)
    {
        if (ReloadName == null)
            return;

        if (m_AnimationType == AnimationType.Animation)
        {
            Anim.Stop(ReloadName.name);
            Anim[ReloadName.name].wrapMode = WrapMode.Once;
            Anim[ReloadName.name].speed = (ReloadName.length / ReloadTime);
            Anim.Play(ReloadName.name);
        }
        else
        {
            animator.Rebind();
            animator.Play("Reload", 0, 0);
            animator.speed = ReloadName.length / ReloadTime;
        }
    }

    /// <summary>
    /// event called by animation when is fire
    /// </summary>
    public void FireAudio()
    {
        if (m_source != null && m_Fire != null)
        {
            m_source.clip = m_Fire;
            m_source.pitch = Random.Range(1, 1.5f);
            m_source.Play();
        }
    }

    public void SplitReload(float ReloadTime, int Bullets)
    {
      StartCoroutine(StartShotgunReload(Bullets));
    }

    IEnumerator StartShotgunReload(int Bullets)
    {
        if (m_AnimationType == AnimationType.Animation)
        {
            Anim.CrossFade(StartReloadAnim.name, 0.2f);
            ParentGun.PlayReloadAudio(0);
            yield return new WaitForSeconds(StartReloadAnim.length);
            for (int i = 0; i < Bullets; i++)
            {
                Anim[InsertAnim.name].wrapMode = WrapMode.Loop;
                float speed = Anim[InsertAnim.name].length / InsertSpeed;
                Anim[InsertAnim.name].speed = speed;
                Anim.CrossFade(InsertAnim.name);
                GunManager.HeadAnimation(3, speed);
                ParentGun.PlayReloadAudio(1);
                yield return new WaitForSeconds(InsertAnim.length / speed);
                ParentGun.AddBullet(1);
                if (cancelReload)
                {
                    Anim.CrossFade(AfterReloadAnim.name, 0.2f);
                    GunManager.HeadAnimation(0, AfterReloadAnim.length);
                    yield return new WaitForSeconds(AfterReloadAnim.length);
                    ParentGun.FinishReload();
                    cancelReload = false;
                    yield break;
                }
            }
            Anim.CrossFade(AfterReloadAnim.name, 0.2f);
            GunManager.HeadAnimation(0, AfterReloadAnim.length);
            ParentGun.PlayReloadAudio(2);
            yield return new WaitForSeconds(AfterReloadAnim.length);
            ParentGun.FinishReload();
        }
        else
        {
            animator.speed = 1;
            animator.Play("StartReload", 0, 0);
            ParentGun.PlayReloadAudio(0);
            yield return new WaitForSeconds(StartReloadAnim.length);
            for (int i = 0; i < Bullets; i++)
            {
                float speed = InsertAnim.length / InsertSpeed;
                animator.speed = speed;
                animator.Play("Insert", 0, 0);
                GunManager.HeadAnimation(3, speed);
                ParentGun.PlayReloadAudio(1);
                yield return new WaitForSeconds(InsertAnim.length / speed);
                ParentGun.AddBullet(1);
                if (cancelReload)
                {
                    animator.CrossFade("AfterReload", 0.32f, 0);
                    GunManager.HeadAnimation(0, AfterReloadAnim.length);
                    yield return new WaitForSeconds(AfterReloadAnim.length);
                    ParentGun.FinishReload();
                    cancelReload = false;
                    yield break;
                }
            }
            animator.Play("EndReload", 0, 0);
            GunManager.HeadAnimation(0, AfterReloadAnim.length);
            ParentGun.PlayReloadAudio(2);
            yield return new WaitForSeconds(AfterReloadAnim.length);
            ParentGun.FinishReload();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void CancelReload()
    {
        cancelReload = true;
    }

    /// <summary>
    /// Use this for greater coordination
    /// reload sounds with animation
    /// </summary>
    public void ReloadSound(int index)
    {
        if (m_source == null)
            return;

        switch (index)
        {
            case 0:
                m_source.clip = Reload_1;
                m_source.Play();
                break;
            case 1:
                m_source.clip = Reload_2;
                m_source.Play();
                GunManager.HeadAnimation(3, 1);
                break;
            case 2:
                if (Reload_3 != null)
                {
                    m_source.clip = Reload_3;
                    m_source.Play();
                }
                break;
        }
    }

    public void PlayThrow()
    {
        GunManager.HeadAnimator.Play("Throw", 0, 0);
    }

    public void PlayParticle(int id)
    {
        ParticleSystem.EmissionModule m = Particles[id].emission;
        m.rateOverTime = ParticleRate;
    }

    public void StopParticle(int id)
    {
        ParticleSystem.EmissionModule m = Particles[id].emission;
        m.rateOverTime = 0;
    }

    /// <summary>
    /// Heat animation
    /// </summary>
    /// <returns></returns>
    IEnumerator ReturnToIdle()
    {
        yield return new WaitForSeconds(0.6f);
    }

    public float GetFireLenght { get { return FireAimAnimation.length / FireSpeed; } }
    public float GetFirePlusDrawLenght { get { return  GetFireLenght + (DrawName.length / DrawSpeed); } }
    public float GetDrawLenght { get { return DrawName.length / DrawSpeed; } }

    [System.Serializable]
    public enum AnimationType
    {
       Animation,
       Animator,
    }

    private Animator _Animator;
    private Animator animator
    {
        get
        {
            if (_Animator == null)
            {
                _Animator = this.GetComponent<Animator>();
            }
            return _Animator;
        }
    }

    private Animation _Anim;
    private Animation Anim
    {
        get
        {
            if (_Anim == null)
            {
                _Anim = this.GetComponent<Animation>();
            }
            return _Anim;
        }
    }
}
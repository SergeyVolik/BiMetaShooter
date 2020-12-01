using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(NavMeshAgent))]
public class bl_AIShooterAgent : bl_MonoBehaviour, IPunObservable
{
    [Space(5)]
    [Header("AI Settings")]
    public AIAgentBehave agentBehave = AIAgentBehave.Agressive;
    [LovattoToogle]public bool GetRandomTargetOnStart = true;
    [LovattoToogle] public bool forceFollowAtHalfHealth = true;

    [Header("Speeds")]
    public float walkSpeed = 4;
    public float RunSpeed = 8;
    public float crounchSpeed = 2;
    public float RotationLerp = 6.0f;

    [Header("Cover")]
    public float maxCoverTime = 10;

    [Space(5)]
    [Header("AutoTargets")]
    public float UpdatePlayerEach = 5f;
    public List<Transform> PlayersInRoom = new List<Transform>();//All Players in room

    [Header("Ranges")]
    public float FollowRange = 10.0f;       //when the AI starts to chase the player
    public float LookRange = 25.0f;   //when the AI starts to look at the player
    public float PatrolRadius = 20f; //Radius for get the random point
    public float LosseRange = 50f;

    [Header("Others")]
    public LayerMask ObstaclesLayer;
    public bool DebugStates = false;

    [Header("References")]
    public Transform AimTarget;
    [SerializeField] private AudioSource FootStepSource;
    [SerializeField] private AudioClip[] FootSteps;

    public AIAgentState AgentState { get; set; } = AIAgentState.Idle;
    public Team AITeam { get; set; } = Team.None;

    //Privates
    private Vector3 correctPlayerPos = Vector3.zero; // We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; // We lerp towards this
    public NavMeshAgent Agent { get; set; }
    public bool death { get; set; }
    public bool personal { get; set; }

    private Animator Anim;
    public bool playerInFront { get; set; }
    private Vector3 finalPosition;
    private float lastPathTime = 0;
    private bl_AIAnimation AIAnim;
    private float stepTime;
  
    [HideInInspector] public Vector3 vel;
    private bl_AICovertPointManager CoverManager;
    private bl_AIMananger AIManager;
    private bl_AICoverPoint CoverPoint = null;
    private bool ForceCoverFire = false;
    public bool ObstacleBetweenTarget { get; set; }
    private float CoverTime = 0;
    private bool lookToDirection = false;
    private Vector3 LastHitDirection;
    private int SwitchCoverTimes = 0;
    private float lookTime = 0;
    private bool randomOnStartTake = false;
    private bool AllOrNothing = false;
    private bl_MatchTimeManager TimeManager;
    public bl_AIShooterWeapon AIWeapon { get; set; }
    public bl_AIShooterHealth AIHealth { get; set; }
    public float CachedTargetDistance { get; set; } = 0;
    private bl_NamePlateDrawer DrawName;
    private GameMode m_GameMode;
    private float time = 0;
    private float delta = 0;
    private Transform m_Transform;
    private float nextEnemysCheck = 0;
    private bl_AIMananger.BotsStats BotStat = null;
    private bool isGameStarted = false;
    private bool firstPackage = false;
    public string DebugLine { get; set; }//last ID 34

    private Transform m_Target;
    public Transform Target
    {
        get { return m_Target; }
        set
        {
            m_Target = value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        m_Transform = transform;
        bl_PhotonCallbacks.PlayerEnteredRoom += OnPhotonPlayerConnected;
        bl_AIMananger.OnMaterStatsReceived += OnMasterStatsReceived;
        bl_AIMananger.OnBotStatUpdate += OnBotStatUpdate;
        Agent = this.GetComponent<NavMeshAgent>();
        AIAnim = GetComponentInChildren<bl_AIAnimation>();
        AIHealth = GetComponent<bl_AIShooterHealth>();
        AIWeapon = GetComponent<bl_AIShooterWeapon>();
        Anim = GetComponentInChildren<Animator>();
        ObstacleBetweenTarget = false;
        CoverManager = FindObjectOfType<bl_AICovertPointManager>();
        AIManager = CoverManager.GetComponent<bl_AIMananger>();
        TimeManager = bl_MatchTimeManager.Instance;
        DrawName = GetComponent<bl_NamePlateDrawer>();
        m_GameMode = GetGameMode;

        GetEssentialData();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Init()
    {
        isGameStarted = TimeManager.TimeState == RoomTimeState.Started;
        InvokeRepeating("UpdateList", 0, UpdatePlayerEach);
        CheckNamePlate();
    }

    /// <summary>
    /// 
    /// </summary>
    void GetEssentialData()
    {
        object[] data = photonView.InstantiationData;
        AIName = (string)data[0];
        AITeam = (Team)data[1];
        gameObject.name = AIName;
        CheckNamePlate();
        //since Non master client doesn't update the view ID when bots are created, lets do it on Start
        if (!PhotonNetwork.IsMasterClient)
        {
            AIManager.UpdateBotView(this, photonView.ViewID);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnMasterStatsReceived(List<bl_AIMananger.BotsStats> stats)
    {
        ApplyMasterInfo(stats);
    }

    /// <summary>
    /// 
    /// </summary>
    void ApplyMasterInfo(List<bl_AIMananger.BotsStats> stats)
    {
        int viewID = photonView.ViewID;
        bl_AIMananger.BotsStats bs = stats.Find(x => x.ViewID == viewID);
        if (bs != null)
        {
            AIName = bs.Name;
            AITeam = bs.Team;
            gameObject.name = AIName;
            BotStat = new bl_AIMananger.BotsStats();
            BotStat.Name = AIName;
            BotStat.Score = bs.Score;
            BotStat.Kills = bs.Kills;
            BotStat.Deaths = bs.Deaths;
            BotStat.ViewID = bs.ViewID;
            bl_EventHandler.OnRemoteActorChange(AIName, BuildPlayer(), true);
            CheckNamePlate();
        }
    }

    void OnBotStatUpdate(bl_AIMananger.BotsStats stat)
    {
        if (stat.ViewID != photonView.ViewID) return;

        BotStat = stat;
        AIName = stat.Name;
        AITeam = BotStat.Team;
        gameObject.name = AIName;
        bl_EventHandler.OnRemoteActorChange(AIName, BuildPlayer(), true);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private MFPSPlayer BuildPlayer(bool isAlive = true)
    {
        MFPSPlayer player = new MFPSPlayer()
        {
            Name = AIName,
            ActorView = photonView,
            isRealPlayer = false,
            Actor = transform,
            AimPosition = AimTarget,
            Team = AITeam,
            isAlive = isAlive,
        };
        return player;
    }
    /// <summary>
    /// 
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_Transform.localPosition);
            stream.SendNext(m_Transform.rotation);
            stream.SendNext(Agent.velocity);
        }
        else
        {
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();
            vel = (Vector3)stream.ReceiveNext();

            //Fix the translation effect on remote clients
            if (!firstPackage)
            {
                m_Transform.localPosition = correctPlayerPos;
                m_Transform.rotation = correctPlayerRot;
                firstPackage = true;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void CheckTargets()
    {
        if(Target != null && Target.name.Contains("(die)"))
        {
            ResetTarget();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        time = Time.time;
        delta = Time.deltaTime;
        if (death) return;
        isNewDebug = true;
        if (!PhotonNetwork.IsMasterClient)//if not master client, then get position from server
        {
            m_Transform.localPosition = Vector3.Lerp(m_Transform.localPosition, correctPlayerPos, delta * 7);
            m_Transform.rotation = Quaternion.Lerp(m_Transform.rotation, correctPlayerRot, delta * 7);
        }
        else
        {
            vel = Agent.velocity;
            if (TimeManager.isFinish)
            {
                Agent.isStopped = true;
                return;
            }
        }
        if (!isGameStarted) return;
        if (Target != null)
        {
            if (AgentState != AIAgentState.Covering)
            {
                TargetControll();
            }
            else
            {
                OnCovering();
            }
        }
    }

    /// <summary>
    /// this is called one time each second instead of each frame
    /// </summary>
    public override void OnSlowUpdate()
    {
        if (death) return;
        if (TimeManager.isFinish || !isGameStarted)
        {
            return;
        }

        if (Target == null)
        {
            SetDebugState(-1, true);
            //Get the player nearest player
            SearchPlayers();
            //if target null yet, the patrol         
             RandomPatrol(!isOneTeamMode);
        }
        else
        {
            CheckEnemysDistances();
            CalculateAngle();
        }
        FootStep();
    }

    /// <summary>
    /// 
    /// </summary>
    void TargetControll()
    {
        CachedTargetDistance = bl_UtilityHelper.Distance(Target.position, m_Transform.localPosition);
        if (CachedTargetDistance >= LosseRange)
        {
            if (AgentState == AIAgentState.Following || personal)
            {
                if (!isOneTeamMode)
                {
                    if(!Agent.hasPath || (Time.frameCount % 300) == 0)
                    SetDestination(TargetPosition, 3);                  
                }
                else
                {
                    RandomPatrol(true);
                }
                SetDebugState(0, true); 
            }
            else if (AgentState == AIAgentState.Searching)
            {
                SetDebugState(1, true);
                RandomPatrol(true);
            }
            else
            {
                SetDebugState(2, true);
                ResetTarget();
                RandomPatrol(false);
                AgentState = AIAgentState.Patroling;
            }
            Speed = walkSpeed;
            if (!AIWeapon.isFiring)
            {
                Anim.SetInteger("UpperState", 4);
            }
        }
        else if (CachedTargetDistance > FollowRange && CachedTargetDistance < LookRange)//look range
        {
            SetDebugState(3, true);
            OnTargetInSight(false);
        }
        else if (CachedTargetDistance <= FollowRange)
        {
            SetDebugState(4, true);
            Follow();
        }
        else if (CachedTargetDistance < LosseRange)
        {
            SetDebugState(5, true);
            OnTargetInSight(true);
        }
        else
        {
            Debug.Log("Unknown state: " + CachedTargetDistance);
            SetDebugState(101, true);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnCovering()
    {
        if (Target != null)
        {
            CachedTargetDistance = TargetDistance;
            if (CachedTargetDistance <= LookRange && playerInFront)//if in look range and in front, start follow him and shot
            {
                if (agentBehave == AIAgentBehave.Agressive)
                {
                    SetDebugState(6, true);
                    if (!Agent.hasPath)
                    {
                        AgentState = AIAgentState.Following;
                        SetDestination(TargetPosition, 3);
                    }
                }
                else //to covert point and looking to it
                {
                    SetDebugState(7, true);
                    AgentState = AIAgentState.Covering;
                    if (!Agent.hasPath)
                    {
                        Cover(false);
                    }
                }
                AIWeapon.Fire();
            }
            else if (CachedTargetDistance > LosseRange && CanCover(7))// if out of line of sight, start searching him
            {
                SetDebugState(8, true);
                AgentState = AIAgentState.Searching;
                SetCrouch(false);
                AIWeapon.Fire(bl_AIShooterWeapon.FireReason.OnMove);
            }
            else if (ForceCoverFire && !ObstacleBetweenTarget)//if bot is cover and still get damage, start shoot at the target (panic)
            {
                SetDebugState(9, true);
                AIWeapon.Fire(bl_AIShooterWeapon.FireReason.Forced);
                if (CanCover(maxCoverTime)) { SwichCover(); }
            }
            else if (CanCover(maxCoverTime) && CachedTargetDistance >= 7)//if has been a time since cover and nothing happen, try a new spot.
            {
                SetDebugState(10, true);
                SwichCover();
                AIWeapon.Fire(bl_AIShooterWeapon.FireReason.OnMove);
            }
            else
            {
                if (playerInFront)
                {
                    Speed = walkSpeed;
                    AIWeapon.Fire(bl_AIShooterWeapon.FireReason.Forced);
                    SetDebugState(11, true);
                }
                else
                {
                    SetDebugState(12, true);
                    Speed = RunSpeed;
                    Look();
                    SetCrouch(false);
                }
            }
        }
        if (Agent.pathStatus == NavMeshPathStatus.PathComplete)//once the bot reach the target cover point
        {
            if (CoverPoint != null && CoverPoint.Crouch) { SetCrouch(true); }//and the point required crouch -> do crouch
        }
        if (lookToDirection)
        {
            LookToHitDirection();
        }
        else
        {
            LookAtTarget();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    bool Cover(bool overridePoint, AIAgentCoverArea coverArea = AIAgentCoverArea.ToPoint)
    {
        //if the target if far, there's not point in cover right now
        if (agentBehave == AIAgentBehave.Agressive && CachedTargetDistance > 20)
        {
            AgentState = AIAgentState.Following;
            return false;
        }
        Transform t = transform;
        switch (coverArea)
        {
            case AIAgentCoverArea.ToTarget:
                t = Target;//find a point near the target
                break;
        }
        if (overridePoint)//override the current cover point
        {
            //if the agent has complete his current destination
            if (Agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                //get a random point in 30 metters
                if (coverArea == AIAgentCoverArea.ToRandomPoint)
                {
                    //look for another random cover point 
                    CoverPoint = CoverManager.GetCoverOnRadius(t, 30);
                }
                else
                {
                    //Get the nearest cover point
                    CoverPoint = CoverManager.GetCloseCover(t, CoverPoint);
                }
            }
            SetDebugState(13);
        }
        else
        {
            SetDebugState(14);
            //look for a near cover point
            CoverPoint = CoverManager.GetCloseCover(t);
        }
        if (CoverPoint != null)//if a point was found
        {
            SetDebugState(15);
            Speed = playerInFront ? walkSpeed : RunSpeed;
            SetDestination(CoverPoint.transform.position, 0.1f);
            AgentState = AIAgentState.Covering;
            CoverTime = time;
            AIWeapon.Fire(agentBehave == AIAgentBehave.Agressive ? bl_AIShooterWeapon.FireReason.Normal : bl_AIShooterWeapon.FireReason.OnMove);
            LookAtTarget();
            return true;
        }
        else
        {
            //if there are not nears cover points
            if (Target != null)//and have a target
            {
                SetDebugState(16);
                //follow the target
                SetDestination(TargetPosition, 3);
                Speed = CachedTargetDistance < 20 ? walkSpeed : RunSpeed;
                personal = true;//and follow not matter the distance
                AgentState = AIAgentState.Searching;
            }
            else//if don't have a target
            {
                SetDebugState(17);
                //Force to get a covert point
                CoverPoint = CoverManager.GetCloseCoverForced(m_Transform);
                SetDestination(CoverPoint.transform.position, 0.1f);
                Speed = Probability(0.5f) ? walkSpeed : RunSpeed;
            }
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnGetHit(Vector3 pos)
    {
        LastHitDirection = pos;
        //if the AI is not covering, will look for a cover point
        if (AgentState != AIAgentState.Covering)
        {
            //if the AI is following and attacking the target he will not look for cover point
            if (AgentState == AIAgentState.Following && TargetDistance <= LookRange)
            {
                lookToDirection = true;
                return;
            }
            Cover(false);
        }
        else
        {
            //if already in a cover and still get shoots from far away will force the AI to fire.
            if (!playerInFront)
            {
                lookToDirection = true;
                Cover(true);
            }
            else
            {
                ForceCoverFire = true;
                lookToDirection = false;
            }
            //if the AI is cover but still get hit, he will search other cover point 
            if (AIHealth.Health <= 50 && Agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                Cover(true);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void LookAtTarget()
    {
        if (Target == null) return;

        Quaternion rotation = Quaternion.LookRotation(Target.position - m_Transform.localPosition);
        m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, rotation, delta * RotationLerp);
    }

    /// <summary>
    /// 
    /// </summary>
    void SwichCover()
    {
        if (Agent.pathStatus != NavMeshPathStatus.PathComplete)
            return;

        if (SwitchCoverTimes <= 3)
        {
            Cover(true, AIAgentCoverArea.ToTarget);
            SwitchCoverTimes++;
        }
        else
        {
            AgentState = AIAgentState.Following;
            SetDestination(TargetPosition, 3);
            SwitchCoverTimes = 0;
            AllOrNothing = true;//go straight to the target to confront him
        }
    }

    /// <summary>
    /// When the target is at look range
    /// </summary>
    void OnTargetInSight(bool overrideCover)
    {
        if (AgentState == AIAgentState.Following || ForceFollowAtHalfHealth)
        {
            if (!Cover(overrideCover) || CanCover(maxCoverTime) || AllOrNothing)
            {
                if (CachedTargetDistance <= 3)
                {
                    SetDebugState(35);
                    Cover(true, AIAgentCoverArea.ToRandomPoint);
                }
                else
                {
                    SetDebugState(18);
                    Follow();
                }
            }
            else
            {
                if(Target != null)
                {
                    bl_AIShooterWeapon.FireReason fr = TargetDistance < 12 ? bl_AIShooterWeapon.FireReason.Forced : bl_AIShooterWeapon.FireReason.OnMove;
                    AIWeapon.Fire(fr);
                }
                SetDebugState(19);
                SetCrouch(true);
            }
        }
        else if (AgentState == AIAgentState.Covering)
        {
            if (CanCover(5) && TargetDistance >= 7)
            {
                SetDebugState(21);
                Cover(true);
            }
            else
            {
                SetDebugState(22);
            }
        }
        else
        {
            SetDebugState(23);
            Look();
            SetCrouch(false);
            Speed = (Target != null && CachedTargetDistance > 20) ? RunSpeed : walkSpeed;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void SearchPlayers()
    {
        SetDebugState(-2);
        for (int i = 0; i < PlayersInRoom.Count; i++)
        {
            Transform enemy = PlayersInRoom[i];
            if (enemy != null)
            {
                float Distance = bl_UtilityHelper.Distance(enemy.position, m_Transform.localPosition);//if a player in range, get this
                bl_AIShooterAgent aisa = enemy.root.GetComponent<bl_AIShooterAgent>();
                if (aisa == null || enemy.name.Contains("(die)") || aisa.isDeath) continue;

                if (isOneTeamMode)
                {
                    if (Distance < LookRange)//if in range
                    {
                        GetTarget(PlayersInRoom[i]);//get this player
                    }
                }
                else
                {
                    if (Distance < LookRange && aisa.AITeam != AITeam)//if in range
                    {
                        GetTarget(PlayersInRoom[i]);//get this player
                    }
                }
            }
        }

        if (PhotonNetwork.IsMasterClient && !randomOnStartTake && PlayersInRoom.Count > 0)
        {
            if (GetRandomTargetOnStart)
            {
                Target = PlayersInRoom[Random.Range(0, PlayersInRoom.Count)];
                randomOnStartTake = true;
            }
        }
        if (Target == null)
        {
            if (AgentState == AIAgentState.Following || AgentState == AIAgentState.Looking) { AgentState = AIAgentState.Searching; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void CalculateAngle()
    {
        if (Target == null || !PhotonNetwork.IsMasterClient)
        {
            ObstacleBetweenTarget = false;
            return;
        }

        Vector3 relative = m_Transform.InverseTransformPoint(Target.position);
        if ((relative.x < 2f && relative.x > -2f) || (relative.x > -2f && relative.x < 2f))
        {
            //target is in front
            playerInFront = true;
        }
        else
        {
            playerInFront = false;
        }
        if(Physics.Linecast(AIWeapon.FirePoint.position, TargetPosition,out obsRay, ObstaclesLayer, QueryTriggerInteraction.Ignore))
        {
            ObstacleBetweenTarget = obsRay.transform.root.CompareTag(bl_PlayerSettings.LocalTag) == false;
        }
        else { ObstacleBetweenTarget = false; }
        Debug.DrawLine(AIWeapon.FirePoint.position, TargetPosition, Color.red);
    }
    RaycastHit obsRay;

    /// <summary>
    /// If player not in range then the AI patrol in map
    /// </summary>
    void RandomPatrol(bool precision)
    {
        if (death)
            return;

        float precisionArea = PatrolRadius;
        if (precision)
        {
            if (TargetDistance < LookRange)
            {
                SetDebugState(24);
                if (Target == null)
                {
                    Target = GetNearestPlayer;
                }
                AgentState = agentBehave == AIAgentBehave.Protective ? AIAgentState.Covering : AIAgentState.Looking;
                precisionArea = 5;
            }
            else
            {
                SetDebugState(25);
                AgentState = agentBehave == AIAgentBehave.Agressive ? AIAgentState.Following : AIAgentState.Searching;
                precisionArea = 8;
            }
        }
        else
        {
            SetDebugState(26);
            AgentState = AIAgentState.Patroling;
            ForceCoverFire = false;
        }
        lookToDirection = false;
        AIWeapon.isFiring = false;
        if (!Agent.hasPath || TargetDistance <= 5.2f || (time - lastPathTime) > 5)
        {
            SetDebugState(27);
            bool toAnCover = (Random.value <= 0.1f);//probability of get a cover point as random destination (1 of 10)
            Vector3 randomDirection = TargetPosition + (Random.insideUnitSphere * precisionArea);
            if (toAnCover) { randomDirection = CoverManager.GetCoverOnRadius(transform, 20).transform.position; }
            if (Target == null && m_GameMode == GameMode.FFA)
            {
                randomDirection += m_Transform.localPosition;
            }
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, precisionArea, 1);
            finalPosition = hit.position;
            lastPathTime = time + Random.Range(0, 5);
            Speed = (CachedTargetDistance > LookRange) ? RunSpeed : walkSpeed;
            SetCrouch(false);
        }
        else
        {
            if (Agent.hasPath)
            {
                SetDebugState(28);
            }
            else
            {
                SetDebugState(32);
            }
        }
        SetDestination(finalPosition, 1);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetDestination(Vector3 position, float stopedDistance)
    {
        Agent.stoppingDistance = stopedDistance;
        Agent.SetDestination(position);
    }

    /// <summary>
    /// 
    /// </summary>
    void SetCrouch(bool crouch)
    {
        if (crouch && (AgentState == AIAgentState.Following || AgentState == AIAgentState.Looking))
        {
            crouch = false;
        }
        Anim.SetBool("Crouch", crouch);
        Speed = crouch ? crounchSpeed : walkSpeed;
    }
   
    /// <summary>
    /// 
    /// </summary>
    public void KillTheTarget()
    {
        if (Target == null) return;

        Target = null;
        photonView.RPC("ResetTarget", RpcTarget.All);
    }

    /// <summary>
    /// Force AI to look the target
    /// </summary>
    void Look()
    {
        if (AgentState != AIAgentState.Covering)
        {
            if (lookTime >= 5)
            {
                AgentState = AIAgentState.Following;
                lookTime = 0;
                return;
            }
            lookTime += delta;
            AgentState = AIAgentState.Looking;
        }
        Quaternion rotation = Quaternion.LookRotation(TargetPosition - m_Transform.localPosition);
        m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, rotation, delta * RotationLerp);
        AIWeapon.Fire();
        SetCrouch(playerInFront);
        lookToDirection = false;
    }

    /// <summary>
    /// 
    /// </summary>
    void LookToHitDirection()
    {
        if (LastHitDirection == Vector3.zero || Target == null)
            return;

        Vector3 rhs = Target.position - LastHitDirection;
        if(rhs == Vector3.zero) { rhs = m_Transform.forward * 10; }

        Quaternion rotation = Quaternion.LookRotation(rhs);
        m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, rotation, delta * RotationLerp);
        SetCrouch(playerInFront);
        Speed = playerInFront ? walkSpeed : RunSpeed;
    }

    /// <summary>
    /// 
    /// </summary>
    void Follow()
    {
        if (AgentState == AIAgentState.Covering && Random.value > 0.5f) return;

        lookToDirection = false;
        SetCrouch(false);
        SetDestination(TargetPosition, 3);
        if (CachedTargetDistance <= 3)
        {
            Speed = walkSpeed;
            if (Cover(true, AIAgentCoverArea.ToTarget))
            {
                SetDebugState(29);
            }
            else if(Cover(true, AIAgentCoverArea.ToRandomPoint))
            {
                SetDebugState(30);
            }
            else
            {
                SetDebugState(34);
                SetDestination(m_Transform.position - (m_Transform.forward * 3), 0.1f);
            }
            AgentState = AIAgentState.Covering;
            Look();
            SetCrouch(false);
            AIWeapon.Fire(bl_AIShooterWeapon.FireReason.Forced);
        }
        else
        {
            Speed = CachedTargetDistance > 20 ? RunSpeed : walkSpeed;
            SetDebugState(33);
            AIWeapon.Fire();
        }
    }

    /// <summary>
    /// This is called when the bot have a Target
    /// this check if other enemy is nearest and change of target if it's require
    /// </summary>
    void CheckEnemysDistances()
    {
        if (PlayersInRoom.Count <= 0) return;
        if (time < nextEnemysCheck) return;

        CachedTargetDistance = bl_UtilityHelper.Distance(m_Transform.localPosition, TargetPosition);
        for (int i = 0; i < PlayersInRoom.Count; i++)
        {
            //if the enemy transform is not null or the same target that have currently have or death.
            if (PlayersInRoom[i] == null || PlayersInRoom[i] == Target || PlayersInRoom[i].name.Contains("(die)")) continue;
            //calculate the distance from this other enemy
            float otherDistance = bl_UtilityHelper.Distance(m_Transform.localPosition, PlayersInRoom[i].position);
            if (otherDistance > LosseRange) continue;//if this enemy is too far away...
            //and check if it's nearest than the current target (5 meters close at least)
            if(otherDistance < CachedTargetDistance && (CachedTargetDistance - otherDistance) > 5)
            {
                //calculate the angle between this bot and the other enemy to check if it's in a "View Angle"
                Vector3 targetDir = PlayersInRoom[i].position - m_Transform.localPosition;
                float Angle = Vector3.Angle(targetDir, m_Transform.forward);
                if(Angle > -55 && Angle < 55)
                {
                    //so then get it as new dangerous target
                    Target = PlayersInRoom[i];
                    //prevent to change target in at least the next 3 seconds
                    nextEnemysCheck = time + 3;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void GetTarget(Transform t)
    {
        if (t == null)
            return;

        Target = t;
        PhotonView view = GetPhotonView(Target.root.gameObject);
        if (view != null)
        {
            photonView.RPC("SyncTargetAI", RpcTarget.Others, view.ViewID);
        }
        else
        {
            Debug.Log("This Target " + Target.name + "no have photonview");
        }
    }


    [PunRPC]
    void SyncTargetAI(int view)
    {
        GameObject pr = FindPlayerRoot(view);
        if (pr == null) return;

        Transform t = pr.transform;
        if (t != null)
        {
            Target = t;
        }
    }

    [PunRPC]
    void ResetTarget()
    {
        Target = null;
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateList()
    {
        PlayersInRoom = AllPlayers;
        AimTarget.name = AIName;
    }

    /// <summary>
    /// 
    /// </summary>
    public void FootStep()
    {
        float vel = Agent.velocity.magnitude;
        if (vel < 1)
            return;

        float lenght = 0.6f;
        if (vel > 5)
        {
            lenght = 0.45f;
        }

        if ((time - stepTime) > lenght)
        {
            stepTime = time;
            FootStepSource.clip = FootSteps[Random.Range(0, FootSteps.Length)];
            FootStepSource.Play();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected List<Transform> AllPlayers
    {
        get
        {
            List<Transform> list = new List<Transform>();
            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (!isOneTeamMode)
                {
                    Team pt = p.GetPlayerTeam();
                    if (pt != AITeam && pt != Team.None)
                    {
                        MFPSPlayer g = bl_GameManager.Instance.FindActor(p.NickName);
                        if (g != null)
                        {
                            list.Add(g.Actor);
                        }
                    }
                }
                else
                {
                    MFPSPlayer g = bl_GameManager.Instance.FindActor(p.NickName);
                    if (g != null)
                    {
                        list.Add(g.Actor);
                    }
                }
            }
            list.AddRange(AIManager.GetOtherBots(AimTarget, AITeam));
            return list;
        }
    }

    void CheckNamePlate()
    {
        DrawName.SetName(AIName);
        if (!isOneTeamMode && bl_GameManager.Instance.LocalPlayer != null && !death)
        {
            DrawName.enabled = bl_GameManager.Instance.LocalPlayerTeam == AITeam;
        }
        else
        {
            DrawName.enabled = false;
        }
    }

    private float Speed
    {
        get
        {
            return Agent.speed;
        }
        set
        {
            bool cr = Anim.GetBool("Crouch");
            if (cr)
            {
                Agent.speed = 2;
            }
            else
            {
                Agent.speed = value;
            }
        }
    }

    [PunRPC]
    public void BotDestroyRpc(ExitGames.Client.Photon.Hashtable data, PhotonMessageInfo info)
    {
        if (data.ContainsKey("instant"))
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Destroy(gameObject);
            }
            return;
        }
        Vector3 position = (Vector3)data["direction"];
        StartCoroutine(DestroyNetwork(position, data.ContainsKey("explosion"), info));
    }

    IEnumerator DestroyNetwork(Vector3 position, bool isExplosion, PhotonMessageInfo info)
    {
        if ((PhotonNetwork.Time - info.SentServerTime) > 5f)
        {
            Destroy(gameObject);
            yield break;
        }
        AIAnim.Ragdolled(position, isExplosion);
        yield return new WaitForSeconds(5);
        if (!PhotonNetwork.IsMasterClient)
        {
            Destroy(this.gameObject);
            yield return 0; // if you allow 1 frame to pass, the object's OnDestroy() method gets called and cleans up references.
        }
    }

    void OnLocalSpawn()
    {
        if (!isOneTeamMode && bl_GameManager.Instance.LocalPlayerTeam == AITeam)
        {
            DrawName.enabled = true;
        }
    }

    public void OnPhotonPlayerConnected(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && newPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            photonView.RPC("RpcSync", newPlayer, AIHealth.Health);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!DebugStates) return;
        if(m_Transform == null) { m_Transform = transform; }
        Gizmos.color = Color.yellow;
        bl_UtilityHelper.DrawWireArc(m_Transform.position, LosseRange, 360, 12, Quaternion.identity);
        Gizmos.color = Color.white;
        bl_UtilityHelper.DrawWireArc(m_Transform.position, PatrolRadius, 360, 12, Quaternion.identity);
        Gizmos.color = Color.yellow;
        bl_UtilityHelper.DrawWireArc(m_Transform.position, LookRange, 360, 12, Quaternion.identity);
        Gizmos.color = Color.white;
        bl_UtilityHelper.DrawWireArc(m_Transform.position, FollowRange, 360, 12, Quaternion.identity);
    }
#endif
    /// <summary>
    /// 
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        bl_EventHandler.OnRemoteActorChange(AIName, BuildPlayer(false), false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        bl_EventHandler.onLocalPlayerSpawn += OnLocalSpawn;
        bl_EventHandler.onMatchStart += OnMatchStart;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.onLocalPlayerSpawn -= OnLocalSpawn;
        bl_PhotonCallbacks.PlayerEnteredRoom -= OnPhotonPlayerConnected;
        bl_AIMananger.OnMaterStatsReceived -= OnMasterStatsReceived;
        bl_AIMananger.OnBotStatUpdate -= OnBotStatUpdate;
        bl_EventHandler.onMatchStart -= OnMatchStart;
    }

    void OnMatchStart() { isGameStarted = true; }
    public void OnDeath() { CancelInvoke(); }
    public Vector3 TargetPosition
    {
        get
        {
            if (Target != null) { return Target.position; }
            if (!isOneTeamMode && PlayersInRoom.Count > 0)
            {
                Transform t = GetNearestPlayer;
                if (t != null)
                {
                    return t.position;
                }
                else { return m_Transform.position + (m_Transform.forward * 3); }
            }
            return Vector3.zero;
        }
    }

    public Transform GetNearestPlayer
    {
        get
        {
            if(PlayersInRoom.Count > 0)
            {
                Transform t = null;
                float d = 1000;
                for (int i = 0; i < PlayersInRoom.Count; i++)
                {
                    if (PlayersInRoom[i] == null || PlayersInRoom[i].name.Contains("(die)")) continue;
                    float dis = bl_UtilityHelper.Distance(m_Transform.position, PlayersInRoom[i].position);
                    if (dis < d)
                    {
                        d = dis;
                        t = PlayersInRoom[i];
                    }
                }
                return t;
            }
            else { return null; }
        }
    }
    private string _ainame = string.Empty;
    public string AIName
    {
        get
        {
            return _ainame;
        }
        set
        {
            _ainame = value;
            gameObject.name = value;
        }
    }

    private MFPSPlayer m_MFPSActor;
    public MFPSPlayer BotMFPSActor
    {
        get
        {
            if(m_MFPSActor == null) { m_MFPSActor = bl_GameManager.Instance.GetMFPSPlayer(AIName); }
            return m_MFPSActor;
        }
    }

    bool isNewDebug = false;
    public void SetDebugState(int stateID, bool initial = false)
    {
        if (!DebugStates) return;
        if (initial && isNewDebug)
        {
            DebugLine = $"{stateID}"; isNewDebug = true; return;
        }
        DebugLine += $"&{stateID}";
    }
    public bool Probability(float probability) { return Random.value <= probability; }
    public bool ForceFollowAtHalfHealth => AIHealth.Health < 50 && forceFollowAtHalfHealth;

    public float TargetDistance { get { return bl_UtilityHelper.Distance(m_Transform.position, TargetPosition); } }
    private bool CanCover(float inTimePassed) { return ((time - CoverTime) >= inTimePassed); }
    public bool isTeamMate { get { return (AITeam == PhotonNetwork.LocalPlayer.GetPlayerTeam() && !isOneTeamMode); } }
    public bool isDeath { get { return death; } }
}
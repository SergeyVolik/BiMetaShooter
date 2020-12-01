using UnityEngine;

public class bl_Ladder : MonoBehaviour
{

    public Transform[] Points;
    [SerializeField]private Collider TopCollider;
    [SerializeField]private Collider BottomCollider;

    public const string TopColName = "TopTrigger";
    public const string BottomColName = "BottomTrigger";
    public bool HasPending { get; set; }
    private int CurrentPoint = 0;
    public Transform InsertionPoint { get; set; }
    private float LastTime = 0;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        TopCollider.name = TopColName;
        BottomCollider.name = BottomColName;
    }

    public void SetToTop()
    {
        LastTime = Time.time;
        CurrentPoint = 2;
        HasPending = true;
    }

    public void ToBottom()
    {
        CurrentPoint = 0;
        HasPending = true;
    }

    public void ToMiddle()
    {
        CurrentPoint = 1;
        HasPending = true;
    }

    public void JumpOut()
    {
        LastTime = Time.time;
    }

    public Vector3 GetCurrent
    {
        get
        {
            return Points[CurrentPoint].position;
        }
    }

    public bool CanUse
    {
        get
        {
            return ((Time.time - LastTime) > 1.5f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if(Points.Length > 0 && Points[0] != null && BottomCollider != null)
        {
            Gizmos.DrawWireCube(BottomCollider.bounds.center, BottomCollider.bounds.size);
            Gizmos.DrawLine(BottomCollider.transform.position, Points[0].position);
        }
        for(int i = 0; i < Points.Length; i++)
        {
            Gizmos.DrawWireSphere(Points[i].position,0.33f);
            if(i < Points.Length - 1)
            {
                Gizmos.DrawLine(Points[i].position, Points[i + 1].position);
            }
        }
        if(TopCollider != null)
        {
            Gizmos.DrawWireCube(TopCollider.bounds.center, TopCollider.bounds.size);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeObjectController : MonoBehaviour
{
    public EyeImageTarget TargetBehaviour;
    public EyeObjectBehaviour RightEyeBehaviour;
    public EyeObjectBehaviour LeftEyeBehaviour;
    public StateManager StateMgr;
    public GameObject RightPivot;
    public GameObject LeftPivot;
    public GameObject[] IdlePivotList;

    public bool IsTrackerAppear
    {
        get
        {
            return TargetBehaviour.CurrentStatus == Vuforia.TrackableBehaviour.Status.TRACKED;
        }
    }

    void Start ()
    {
        IdleState idleState = new IdleState(StateMgr, this);
        StateMgr.SwitchTo(idleState);
    }

	void Update ()
    {
        StateMgr.Update();
	}
}

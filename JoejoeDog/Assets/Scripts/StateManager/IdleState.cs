using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : EyeControllerState
{
    const float MOVE_TIME = 0.15f;

    const float STAY_TIME_MIN = 4f;
    const float STAY_TIME_MAX = 8f;

    private float curStayTime = 0;
    private int curStatus = 0;

    public IdleState(StateManager stateMgr, EyeObjectController controller) : base(stateMgr, controller)
    {
    }

    public override void Update()
    {
        if(controller.IsTrackerAppear)
        {
            FollowState followState = new FollowState(stateMgr, controller);
            stateMgr.SwitchTo(followState);
        }
        else
        {
            if (curStayTime <= 0)
            {
                curStayTime = Random.Range(STAY_TIME_MIN, STAY_TIME_MAX);

                curStatus = (curStatus + Random.Range(0, controller.IdlePivotList.Length)) % controller.IdlePivotList.Length;
                switch (curStatus)
                {
                    case 0:
                        controller.LeftEyeBehaviour.ResetPos(MOVE_TIME);
                        controller.RightEyeBehaviour.ResetPos(MOVE_TIME);
                        break;
                    case 1:
                        controller.LeftEyeBehaviour.Roll(MOVE_TIME);
                        controller.RightEyeBehaviour.Roll(MOVE_TIME);
                        break;
                    default:
                        controller.LeftEyeBehaviour.FollowTarget(controller.IdlePivotList[curStatus].transform.position, MOVE_TIME);
                        controller.RightEyeBehaviour.FollowTarget(controller.IdlePivotList[curStatus].transform.position, MOVE_TIME);
                        break;
                }
            }
            else
            {
                curStayTime -= Time.deltaTime;
            }
        }
    }

    public override IEnumerator startStateProcess()
    {
        controller.RightEyeBehaviour.ResetPos(MOVE_TIME);
        controller.LeftEyeBehaviour.ResetPos(MOVE_TIME);

        yield return new WaitForSeconds(MOVE_TIME);
    }
}

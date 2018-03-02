using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollState : EyeControllerState
{
    const float MOVE_TIME = 0.15f;
    const float ROLL_TIME = 1f;
    const float STAY_TIME = 3.5f;

    const float STAY_TIME_MIN = 0.25f;
    const float STAY_TIME_MAX = 0.5f;
    const int FIND_TIMES_MIN = 2;
    const int FIND_TIMES_MAX = 5;

    public RollState(StateManager stateMgr, EyeObjectController controller) : base(stateMgr, controller)
    {
    }

    public override IEnumerator startStateProcess()
    {
        int findTimes = Random.Range(FIND_TIMES_MIN, FIND_TIMES_MAX);
        for(int i=0; i< findTimes; ++i)
        {
            controller.RightEyeBehaviour.FollowTarget(controller.RightPivot.transform.position, MOVE_TIME);
            controller.LeftEyeBehaviour.FollowTarget(controller.RightPivot.transform.position, MOVE_TIME);
            yield return new WaitForSeconds(MOVE_TIME + Random.Range(STAY_TIME_MIN, STAY_TIME_MAX));

            controller.RightEyeBehaviour.FollowTarget(controller.LeftPivot.transform.position, MOVE_TIME);
            controller.LeftEyeBehaviour.FollowTarget(controller.LeftPivot.transform.position, MOVE_TIME);
            yield return new WaitForSeconds(MOVE_TIME + Random.Range(STAY_TIME_MIN, STAY_TIME_MAX));
        }

        controller.RightEyeBehaviour.ResetPos(MOVE_TIME);
        controller.LeftEyeBehaviour.ResetPos(MOVE_TIME);

        yield return new WaitForSeconds(MOVE_TIME + 0.5f);

        controller.RightEyeBehaviour.Roll(ROLL_TIME);
        controller.LeftEyeBehaviour.Roll(ROLL_TIME);

        yield return new WaitForSeconds(ROLL_TIME + STAY_TIME);

        if(controller.IsTrackerAppear)
        {
            FollowState followState = new FollowState(stateMgr, controller);
            stateMgr.SwitchTo(followState);
        }
        else
        {
            IdleState idleState = new IdleState(stateMgr, controller);
            stateMgr.SwitchTo(idleState);
        }
    }
}

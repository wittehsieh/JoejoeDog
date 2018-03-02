using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowState : EyeControllerState
{
    const float MOVE_TIME = 0.15f;

    public FollowState(StateManager stateMgr, EyeObjectController controller) : base(stateMgr, controller)
    {
    }

    public override IEnumerator startStateProcess()
    {
        controller.RightEyeBehaviour.ResetPos(MOVE_TIME);
        controller.LeftEyeBehaviour.ResetPos(MOVE_TIME);

        yield return new WaitForSeconds(MOVE_TIME);
    }

    public override void Update()
    {
        if(controller.IsTrackerAppear)
        {
            controller.RightEyeBehaviour.FollowTarget(controller.TargetBehaviour.transform.position, MOVE_TIME);
            controller.LeftEyeBehaviour.FollowTarget(controller.TargetBehaviour.transform.position, MOVE_TIME);
        }
        else
        {
            RollState rollState = new RollState(stateMgr, controller);
            stateMgr.SwitchTo(rollState);
        }
    }
}

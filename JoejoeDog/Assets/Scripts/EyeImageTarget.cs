using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia; 

public class EyeImageTarget : ImageTargetBehaviour {

    public Action OnTrackerAppear = delegate { };
    public Action OnTrackerDisappear = delegate { };

    public override void OnTrackerUpdate(Status newStatus)
    {
        if(this.CurrentStatus != Status.TRACKED && newStatus == Status.TRACKED)
        {
            OnTrackerAppear();
        }

        if(this.CurrentStatus == Status.TRACKED && newStatus != Status.TRACKED)
        {
            OnTrackerDisappear();
        }

        base.OnTrackerUpdate(newStatus);
    } 
}

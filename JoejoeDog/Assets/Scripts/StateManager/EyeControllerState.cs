using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeControllerState : BaseState
{
    protected StateManager stateMgr;
    protected EyeObjectController controller;

    public EyeControllerState(StateManager stateMgr, EyeObjectController controller) : base(stateMgr)
    {
        this.stateMgr = stateMgr;
        this.controller = controller;
    }
}

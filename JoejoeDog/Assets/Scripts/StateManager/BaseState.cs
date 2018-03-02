using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseState
{
    private StateManager stateMgr;

    public BaseState(StateManager stateMgr)
    {
        this.stateMgr = stateMgr;
    }

    public virtual IEnumerator startStateProcess()
    {
        yield return null;
    }

    public virtual IEnumerator endStateProcess()
    {
        yield return null;
    }

    public virtual void Update()
    {

    }
}

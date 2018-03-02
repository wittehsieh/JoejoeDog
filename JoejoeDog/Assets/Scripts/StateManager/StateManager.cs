using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    private BaseState curState = null;
    private BaseState nextState = null;
    private bool isSwitching = false;

    public void Update()
    {
        if(curState != null && !isSwitching)
        {
            curState.Update();
        }
    }

    public void SwitchTo(BaseState state)
    {
        if (!isSwitching)
        {
            nextState = state;
            StartCoroutine("switchProcess");
        }
        else
        {
            Debug.Log("State is switching...");
        }
    }

    private IEnumerator switchProcess()
    {
        isSwitching = true;

        if(curState != null)
        {
            yield return StartCoroutine(curState.endStateProcess());
        }

        curState = nextState;
        nextState = null;

        Debug.LogFormat("Switch to {0}", curState.GetType().ToString());

        StartCoroutine(curState.startStateProcess());

        isSwitching = false;
    }
	
}

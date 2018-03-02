using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeObjectBehaviour : MonoBehaviour
{
    public OSC osc;
    public bool IsSender;

    public string FollowAddress = "/joejoe/follow";
    public string ResetAddress = "/joejoe/reset";
    public string RollAddress = "/joejoe/roll";

    public float EyeRadius = 0.15f; //預設眼球的半徑
    public float MaxDist = 0.1f; //瞳孔移動的最大半徑
    public GameObject PupilObject;
    public GameObject RollPivot;

    private Vector3 eyeBallCenter;
    
    public void FollowTarget(Vector3 targetPos, float moveTime)
    {
        LeanTween.move(PupilObject, calcTargetPos(targetPos), moveTime);

        if (IsSender)
        {
            OscMessage message = new OscMessage();
            message.address = FollowAddress;
            message.values.Add(targetPos.x);
            message.values.Add(targetPos.y);
            message.values.Add(targetPos.z);
            message.values.Add(moveTime);
            osc.Send(message);
        }
    }

    public void ResetPos(float moveTime)
    {
        LeanTween.cancel(PupilObject);
        LeanTween.moveLocal(PupilObject, Vector3.zero, moveTime);

        if (IsSender)
        {
            OscMessage message = new OscMessage();
            message.address = ResetAddress;
            message.values.Add(moveTime);
            osc.Send(message);
        }
    }

    public void Roll(float moveTime)
    {
        LeanTween.moveLocal(PupilObject, RollPivot.transform.localPosition, moveTime);

        if (IsSender)
        {
            OscMessage message = new OscMessage();
            message.address = RollAddress;
            message.values.Add(moveTime);
            osc.Send(message);
        }
    }

    void Start()
    {
        eyeBallCenter = transform.position - new Vector3(0, 0, EyeRadius);

        if(!IsSender)
        {
            osc.SetAddressHandler(FollowAddress, followAddressHandler);
            osc.SetAddressHandler(ResetAddress, resetAddressHandler);
            osc.SetAddressHandler(RollAddress, rollAddressHandler);
        }
    }

    private void followAddressHandler(OscMessage oscMsg)
    {
        FollowTarget(new Vector3((float)oscMsg.values[0], (float)oscMsg.values[1], (float)oscMsg.values[2]), (float)oscMsg.values[3]);
    }

    private void resetAddressHandler(OscMessage oscMsg)
    {
        ResetPos((float)oscMsg.values[0]);
    }

    private void rollAddressHandler(OscMessage oscMsg)
    {
        Roll((float)oscMsg.values[0]);
    }

    private Vector3 calcTargetPos(Vector3 targetPos)
    {
        Vector3 pos = (targetPos - eyeBallCenter) * EyeRadius / Mathf.Abs(targetPos.z - eyeBallCenter.z);
        if(pos.magnitude > MaxDist)
        {
            pos *= MaxDist / pos.magnitude;
        }
        return pos + transform.position;
    }
}

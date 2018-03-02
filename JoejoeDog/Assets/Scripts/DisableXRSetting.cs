using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DisableXRSetting : MonoBehaviour
{
	void Awake ()
    {
        XRSettings.enabled = false;
    }

}

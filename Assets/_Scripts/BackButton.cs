/*============================================================================== 
 * Copyright (c) 2015 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/
using UnityEngine;
using System.Collections;


public class BackButton : MonoBehaviour 
{
	void Update () 
    {
#if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Quitting application...");
            Application.Quit();
        }
#endif
	}
}

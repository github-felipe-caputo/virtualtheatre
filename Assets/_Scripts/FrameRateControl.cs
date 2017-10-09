/*============================================================================== 
Copyright (c) 2016 PTC Inc. All Rights Reserved.

 * Copyright (c) 2015 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/
using UnityEngine;
using System.Collections;
using Vuforia;

public class FrameRateControl : MonoBehaviour 
{
	void Start () 
    {
        VuforiaBehaviour.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaBehaviour.Instance.RegisterOnPauseCallback(OnPause);
	}

    private void OnVuforiaStarted()
    {
        SetTargetFrameRate();
    }

    private void OnPause(bool paused)
    {
        if (!paused) //resumed
        {
            SetTargetFrameRate();
        }
    }

    private void SetTargetFrameRate()
    {
        int targetFPS = 0;
        EyewearDevice eyewearDevice = Device.Instance as EyewearDevice;
        if ((eyewearDevice != null) && eyewearDevice.IsSeeThru())
        {
            // In see-through devices, there is no video background to render
            targetFPS = VuforiaRenderer.Instance.GetRecommendedFps(VuforiaRenderer.FpsHint.NO_VIDEOBACKGROUND);
        }
        else
        {
            // Query Vuforia for AR target frame rate and set it in Unity:
            targetFPS = VuforiaRenderer.Instance.GetRecommendedFps(VuforiaRenderer.FpsHint.NONE);
        }
        // Note: if you use vsync in your quality settings, you should also set 
        // your QualitySettings.vSyncCount according to the value returned above.
        // e.g. if targetFPS > 50 --> vSyncCount = 1; else vSyncCount = 2;
        Application.targetFrameRate = targetFPS;
	}
}

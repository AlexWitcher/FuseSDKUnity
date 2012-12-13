package com.fusepowered.unity;

import com.fusepowered.util.FuseAdCallback;
import com.unity3d.player.UnityPlayer;
import android.util.Log;

public class FuseUnityAdCallback extends FuseAdCallback
{
	public void adDisplayed()
	{
		Log.d(_logTag, "adDisplayed()");
		UnityPlayer.UnitySendMessage("FuseAPI_Android", "_AdDisplayed", "");
	}

	public void adClicked()
	{
		Log.d(_logTag, "adClicked()");
		UnityPlayer.UnitySendMessage("FuseAPI_Android", "_AdClicked", "");
	}

	public void adWillClose()
	{
		Log.d(_logTag, "adWillClose()");
		UnityPlayer.UnitySendMessage("FuseAPI_Android", "_AdWillClose", "");
	}

	private static final String _logTag = "FuseUnityAdCallback";
}
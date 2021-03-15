﻿using Amsterdam3D.CameraMotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amsterdam3D.Rendering {
	public class RealtimeReflectionFollower : MonoBehaviour
	{
		//This makes this relatime reflectionprobe move with the camera, so we have vanilla Unity realtime reflections when it is enabled
		private void LateUpdate()
		{
			this.transform.position = CameraModeChanger.Instance.ActiveCamera.transform.position;
		}
	}
}
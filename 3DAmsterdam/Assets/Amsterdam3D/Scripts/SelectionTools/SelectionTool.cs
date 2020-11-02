﻿using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Amsterdam3D.SelectionTools
{
    public abstract class SelectionTool : MonoBehaviour
    {


        public GameObject Canvas;
        public UnityEvent onSelectionCompleted;
        public ToolType toolType { get; protected set; }
        public Bounds bounds = new Bounds();
        public List<Vector3> vertexes = new List<Vector3>();

        // Use this for initialization
        public abstract void EnableTool();
        public abstract void DisableTool();
    }
}
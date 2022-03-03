using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Updater : MonoBehaviour
{
    public TimeDivisionTicker ticker { get; set; }
    
    private void Update()
    {
        if (ticker != null)
        {
            ticker.Tick(Time.frameCount - 1); // start from 0
        }
    }
}

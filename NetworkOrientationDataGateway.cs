// Created By   :   Isaac Bustad
// Created      :   10/5/2026


using UnityEngine;
using System.IO;
using FishNet.Object;
using BugFreeProductions.Tools;


namespace BugFreeProductions.Networking
{
    public class NetworkOrientationDataGateway: Singleton<NetworkOrientationDataGateway>
    {
        
        public static bool SendOrientationDelta(NetworkOrientation targetComponent, Vector3 delta)
        

        {
            // 1. Check threshold gate before doing work
            if (Mathf.Abs(delta.x) >= targetComponent.Threshold || 
                Mathf.Abs(delta.y) >= targetComponent.Threshold || 
                Mathf.Abs(delta.z) >= targetComponent.Threshold)
            {
                // 2. Quantize floats directly to shorts
                short qx = (short)(delta.x * 1000f);
                short qy = (short)(delta.y * 1000f);
                short qz = (short)(delta.z * 1000f);

                Debug.Log("Delta exceeded threshold!");

                // 3. Hand off directly to FishNet transmission or writer stream
                // (e.g., calling your FishNet RPC or custom writer logic here)
                return true;

            }

            else
            {
                return false;
            }
            
        }
    }
}
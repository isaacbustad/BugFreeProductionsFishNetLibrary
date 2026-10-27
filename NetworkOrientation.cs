// Created By   :   Isaac Bustad
// Created      :   10/5/2026

using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System;
using System.Collections.Generic;
using BugFreeProductions.Spatial;

namespace BugFreeProductions.Networking
{
    public class NetworkOrientation : NetworkBehaviour
    {
        #region Vars
        [SerializeField] protected float threshold = 0.001f;

        // Last position sent to the server
        protected Vector3 lastSentPosition;

        // Delta of client that has not yet been applied
        protected OrientationData pendingDelta;

        // If there are any un applied Deltas
        protected bool hasPendingDelta = false;

        // Target position an object will move to
        protected Vector3 targetPosition = Vector3.zero;

        // Type of behaviour server or client
        protected List<NetworkOrientationHandler> nohs = new List<NetworkOrientationHandler>();

        #endregion Vars

        #region Lifecycle
        

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if(base.IsClientInitialized)
            {
                nohs.Add(new NetworkClientOrientationHandler(this)) ;
            }
            

            // Select and
            if(base.IsServerInitialized)
            {
                nohs.Add(new NetworkServerOrientationHandler(this)) ;
            }

            

            //base.TimeManager.OnTick += TimeManager_OnTick;

            
        }
        
        #endregion Lifecycle

        


        #region Methods
        // Public wrapper method called by your server validation logic
        public void SendServerCorrection(NetworkConnection conn, Vector3 correctPosition)
        {
            if (!base.IsServerInitialized) return;
            
            TargetSendServerCorrection(conn, correctPosition);
        }

        // Send Delta updates from the server
        public void SendServerOrientationDeltas(byte[] payload)
        {
            ObserversServerSendOrientation(payload);
        }
        #endregion Methods


        #region NetworkRPCs
        [ServerRpc]
        protected void CmdSendOrientation(byte[] payload, NetworkConnection sender = null)
        {
            foreach(NetworkOrientationHandler noh in nohs)
            {
                if (noh != null && noh is NetworkServerOrientationHandler nsoh)
                {
                    nsoh.ReceiveClientOrientation(payload,sender);
                }
            }
            
        }

        // access to send orientation from handler
        public void SendClientOrientation(byte[] payload, NetworkConnection sender = null)
        {
            CmdSendOrientation(payload, sender);
        }

        [TargetRpc]
        protected void TargetSendServerCorrection(NetworkConnection conn, Vector3 correctPosition)
        {
            // This code ONLY runs on the specific client targeted by 'conn'.
            // Feed this correctPosition into your interpolation/smoothing buffer!
            Debug.Log($"[Client] Received server correction. Snapping/Smoothing to: {correctPosition}");
            foreach (NetworkOrientationHandler noh in nohs)
            {
                if (noh is NetworkClientOrientationHandler ncoh)
                {
                    ncoh.CorrectPosition(correctPosition);
                }
            }
        }

        [ObserversRpc(ExcludeOwner = true)]
        protected void ObserversServerSendOrientation(byte[] payload)
        {
            // Only other players run this to update this player's visual orientation.
            // The local owner never triggers this code block.
            Debug.Log("Observers notified");
            foreach (NetworkOrientationHandler noh in nohs)
            {
                Debug.Log("Observers notified and loop initiated");
                if (noh is NetworkClientOrientationHandler ncoh)
                {
                    Debug.Log("Observers notified and NCOH Found");
                    ncoh.CorrectPositionDelta(payload);
                }
            }
        }

        #endregion NetworkRPCs


        #region Accessors
        public float Threshold => threshold;
        #endregion Accessors
    }
}
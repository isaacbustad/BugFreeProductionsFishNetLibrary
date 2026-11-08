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


        [SerializeField] private OrientationReconciliation orientationReconciliation;


        [SerializeField] private Rigidbody rb = null;


    
        #endregion Vars

        #region Lifecycle
        

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!nohs.Exists(h => h is NetworkClientOrientationHandler))
            {
                nohs.Add(new NetworkClientOrientationHandler(this));
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (!nohs.Exists(h => h is NetworkServerOrientationHandler))
            {
                nohs.Add(new NetworkServerOrientationHandler(this));
            }
        }
        
        #endregion Lifecycle

        


        #region Methods
        // Public wrapper method called by your server validation logic
        public void SendServerCorrection(uint aServerTick, NetworkConnection conn, byte[] correctionPayload)
        {
            if (!base.IsServerInitialized) return;
            
            TargetSendServerCorrection( conn, aServerTick, correctionPayload);
        }

        // Send Delta updates from the server
        public void SendServerOrientation(uint aServerTick, byte[] payload)
        {
            ObserversServerSendOrientation(aServerTick, payload);
        }
        #endregion Methods


        #region NetworkRPCs
        [ServerRpc]
        protected void CmdSendOrientation(uint aServerTick, byte[] payload, NetworkConnection sender = null)
        {
            foreach(NetworkOrientationHandler noh in nohs)
            {
                if (noh != null && noh is NetworkServerOrientationHandler nsoh)
                {
                    nsoh.ReceiveClientOrientation(aServerTick, payload, sender);
                }
            }
            
        }

        // access to send orientation from handler
        public void SendClientOrientation(uint aServerTick, byte[] payload, NetworkConnection sender = null)
        {
            CmdSendOrientation(aServerTick, payload, sender);
        }

        [TargetRpc]
        protected void TargetSendServerCorrection( NetworkConnection conn, uint aServerTick, byte[] correctionPayload)
        {
            // This code ONLY runs on the specific client targeted by 'conn'.
            // Feed this correctPosition into your interpolation/smoothing buffer!
            //Debug.Log($"[Client] Received server correction. Snapping/Smoothing to: {correctOrientation}");
            foreach (NetworkOrientationHandler noh in nohs)
            {
                if (noh is NetworkClientOrientationHandler ncoh)
                {
                    ncoh.CorrectClientOrientation(aServerTick, correctionPayload);
                }
            }
        }

        [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
        protected void ObserversServerSendOrientation(uint aServerTick, byte[] payload)
        {
            //if (base.IsHostInitialized) return;
            // Only other players run this to update this player's visual orientation.
            // The local owner never triggers this code block.
            Debug.Log("Observers notified");
            foreach (NetworkOrientationHandler noh in nohs)
            {
                

                Debug.Log("Observers notified and loop initiated");
                if (noh is NetworkClientOrientationHandler ncoh)
                {
                    Debug.Log("Observers notified and NCOH Found");
                    ncoh.ReceiveOrientationObserverRPC(aServerTick, payload);
                }
            }
        }

        #endregion NetworkRPCs


        #region Accessors
        public float Threshold => threshold;

        public OrientationReconciliation OrientationReconciliation
        {
            get
            {
                return orientationReconciliation;
            }
        }

        public Rigidbody RB
        {
            get
            {
                return rb;
            }
        }
        
        #endregion Accessors
    }
}
// Created By   :   Isaac Bustad
// Created      :   10/5/2026

using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System;
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
        protected NetworkOrientationHandler noh;

        #endregion Vars

        #region Lifecycle
        

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            // Select and
            if(base.IsServerInitialized)
            {
                noh = new NetworkServerOrientationHandler(GetComponent<Rigidbody>(),this);
            }

            else
            {
                noh = new NetworkClientOrientationHandler(GetComponent<Rigidbody>(),this);
            }

            
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            // Unsubscribe to prevent memory leaks
            if (base.TimeManager != null)
            {
                base.TimeManager.OnTick -= TimeManager_OnTick;
            }
        }
        #endregion Lifecycle

        #region Tick Simulation
        private void TimeManager_OnTick()
        {
            if (!base.IsOwner) return;

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - lastSentPosition;

            // Call serializer to get the 8-byte payload (or null if stationary)
            byte[] payload = OrientationDataSerializer.Serialize( delta, threshold);

            if (payload != null)
            {
                // Send payload over the network via ServerRpc
                CmdSendOrientation(payload);
                
                // Update baseline position for the next tick delta calculation
                lastSentPosition = currentPosition;
            }
        }

        // 1. APPLIES before physics simulates for this tick
        private void TimeManager_OnPrePhysicsSimulation()
        {
            
        }

        // 2. CHECKS immediately after physics finishes simulating for this tick
        private void TimeManager_OnPostPhysicsSimulation()
        {
            
        }
        #endregion Tick Simulation


        #region Methods
        // Public wrapper method called by your server validation logic
        public void SendCorrection(NetworkConnection conn, Vector3 correctPosition)
        {
            if (!base.IsServer) return;
            
            TargetSendCorrection(conn, correctPosition);
        }
        #endregion Methods


        #region NetworkRPCs
        [ServerRpc]
        protected void CmdSendOrientation(byte[] payload, NetworkConnection sender = null)
        {
            if (noh != null && noh is NetworkServerOrientationHandler ncoh)
            {
                ncoh.RecieceCmdSendOrientation(payload,sender);
            }
        }

        // access to send orientation from handler
        public void SendOrientation(byte[] payload, NetworkConnection sender = null)
        {
            CmdSendOrientation(payload, sender);
        }

        [TargetRpc]
        protected void TargetSendCorrection(NetworkConnection conn, Vector3 correctPosition)
        {
            // This code ONLY runs on the specific client targeted by 'conn'.
            // Feed this correctPosition into your interpolation/smoothing buffer!
            Debug.Log($"[Client] Received server correction. Snapping/Smoothing to: {correctPosition}");
        }

        [ObserversRpc(ExcludeOwner = true)]
        protected void ObserversSendOrientation(byte[] payload)
        {
            // Only other players run this to update this player's visual orientation.
            // The local owner never triggers this code block.
        }

        #endregion NetworkRPCs


        #region Accessors
        public float Threshold => threshold;
        #endregion Accessors
    }
}
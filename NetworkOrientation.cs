// Created By   :   Isaac Bustad
// Created      :   10/5/2026

using UnityEngine;
using FishNet.Object;

namespace BugFreeProductions.Networking
{
    public class NetworkOrientation : NetworkBehaviour
    {
        #region Vars
        [SerializeField] protected float threshold = 0.001f;
        protected Vector3 lastSentPosition;
        #endregion Vars

        #region Lifecycle
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            if (!base.IsOwner)
            {
                enabled = false;
                return;
            }

            lastSentPosition = transform.position;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // Subscribe to FishNet's fixed simulation tick
            if (base.TimeManager != null)
            {
                base.TimeManager.OnTick += TimeManager_OnTick;
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
            // Only the owner processes and sends outbound orientation data
            if (!base.IsOwner) return;

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - lastSentPosition;

            // Check threshold and send via gateway once per tick
            if (NetworkOrientationDataGateway.SendOrientationDelta(this, delta))
            {
                lastSentPosition = currentPosition;
            }
        }
        #endregion Tick Simulation

        #region Accessors
        public float Threshold => threshold;
        #endregion Accessors
    }
}
// Created By   :   Isaac Bustad
// Created      :   10/12/2026



using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Timing;
using BugFreeProductions.Spatial;


namespace BugFreeProductions.Networking
{
    public class NetworkClientOrientationHandler : NetworkOrientationHandler 
    {
        #region Vars
        protected Vector3? lastSentPosition = null;

        private bool isSleeping = false;
        private int stationaryTickCount = 0;
        private const int SleepThresholdTicks = 15; // ~1/4 second of no movement before sleeping


        #endregion Vars



        #region Methods

        

        #region Ownership  
        
        #endregion Ownership

        public override void OnPreTick()
        {
            
        }

        // CHECKS immediately after physics finishes simulating for this tick
        public override void OnPostTick()
        {
            if (!no.IsOwner) return;

            if (!lastSentPosition.HasValue)
            {
                lastSentPosition = tf.position;
                return; 
            }

            Vector3 currentPosition = tf.position;
            Vector3 delta = currentPosition - lastSentPosition.Value;

            // Check if movement is below threshold to handle rest state
            if (delta.sqrMagnitude < no.Threshold * no.Threshold)
            {
                stationaryTickCount++;
                
                // Keep baseline pinned to current position while resting so it doesn't drift
                lastSentPosition = currentPosition; 

                if (stationaryTickCount >= SleepThresholdTicks)
                {
                    if (!isSleeping) // Only log the exact moment it transitions to sleep
                    {
                        isSleeping = true;
                        Debug.Log($"[Orientation] Object went to SLEEP after {SleepThresholdTicks} idle ticks.");
                    }
                    return; // Skip serialization and network send while resting
                }
            }
            else
            {
                if (isSleeping) // Only log the exact moment it wakes up
                {
                    Debug.Log("[Orientation] Object WOKE UP due to movement delta.");
                }

                stationaryTickCount = 0; // Wake up immediately on movement
                isSleeping = false;
            }

            // Call serializer to get the payload (or null if stationary)
            byte[] payload = OrientationDataSerializer.Serialize(delta, no.Threshold);

            if (payload != null)
            {
                // Send payload over the network via ServerRpc
                no.SendClientOrientation(payload);
                
                // Update baseline position for the next tick delta calculation
                lastSentPosition = currentPosition;
            }
        }

        public void CorrectPosition(Vector3 correctPosition)
        {
            Debug.Log("Set correct Position through targeted RPC");

            if (no.SplineUpdater != null)
            {
                // Pull velocity from Rigidbody if available, otherwise zero
                Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                // Push the corrected state into the spline smoothing buffer via the gateway accessor
                no.SplineUpdater.PushPhysicalState(correctPosition, currentVelocity, tickDuration);
            }
            
            // Fallback direct snap if no updater is assigned in the inspector
            tf.position = correctPosition;
            
        }

        public void CorrectPositionDelta(byte[] payload)
        {
            // 1. Deserialize the delta from the server payload
            OrientationData orientationData = OrientationDataSerializer.DeserializeOrientation(payload);
            Vector3 targetPosition = tf.position + orientationData.positionData;

            // 2. Visually update via spline if the component is assigned
            // (Both pure clients AND the host run this so remote objects render smoothly)
            if (no.SplineUpdater != null)
            {
                // Pull velocity from Rigidbody if available, otherwise zero
                Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                // Push the calculated target position into the spline smoothing buffer
                no.SplineUpdater.PushPhysicalState(targetPosition, currentVelocity, tickDuration);
            }

            // 3. Update the physical body's position immediately...
            // BUT skip this on the server/host because the server's physics simulation 
            // is already driving the physical body authoritatively!
            if (no.IsServerInitialized) return;

            tf.position = targetPosition;
        }


        
        #endregion Methods


        #region Constructors
        public NetworkClientOrientationHandler(NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {
            
            
        }

        #endregion Constructors
    }
}
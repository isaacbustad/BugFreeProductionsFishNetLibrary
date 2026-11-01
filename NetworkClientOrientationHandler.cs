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

            // Establish baseline on the very first valid frame, then skip sending for this 
            // single tick so we don't transmit a delta from (0,0,0) to the spawn point.
            if (!lastSentPosition.HasValue)
            {
                lastSentPosition = tf.position;
                return; 
            }

            Vector3 currentPosition = tf.position;
            Vector3 delta = currentPosition - lastSentPosition.Value;

            // Call serializer to get the 8-byte payload (or null if stationary)
            byte[] payload = OrientationDataSerializer.Serialize( delta, no.Threshold);

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
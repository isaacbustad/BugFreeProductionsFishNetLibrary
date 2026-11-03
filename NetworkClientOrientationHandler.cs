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
        protected OrientationData? lastSentOrientation = null;

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

            OrientationData currentOrientation = tf.OrientationData();

            if (!lastSentOrientation.HasValue)
            {
                lastSentOrientation = currentOrientation;
                return; 
            }

            float positionDeltaSqr = (currentOrientation.positionData - lastSentOrientation.Value.positionData).sqrMagnitude;
            float rotationDelta = Quaternion.Angle(currentOrientation.rotationData, lastSentOrientation.Value.rotationData);

            // Check if movement/rotation is below threshold to handle rest state
            if (positionDeltaSqr < no.Threshold * no.Threshold && rotationDelta < 0.1f)
            {
                stationaryTickCount++;
                lastSentOrientation = currentOrientation; 

                if (stationaryTickCount >= SleepThresholdTicks)
                {
                    if (!isSleeping)
                    {
                        isSleeping = true;
                        Debug.Log($"[Orientation] Object went to SLEEP after {SleepThresholdTicks} idle ticks.");
                    }
                    return; 
                }
            }
            else
            {
                if (isSleeping)
                {
                    Debug.Log("[Orientation] Object WOKE UP due to movement/rotation delta.");
                }

                stationaryTickCount = 0;
                isSleeping = false;
            }

            // Call the 13-byte absolute serializer
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentOrientation);
            byte[] payload = OrientationDataSerializer.SerializePackage(packet);

            if (payload != null)
            {
                no.SendClientOrientation(payload);
                lastSentOrientation = currentOrientation;
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
            // 1. Deserialize the absolute packet from the server payload
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.DeserializePackage(payload);
            OrientationData targetOrientation = OrientationDataSerializer.DeserializeOrientation(packet);

            // 2. Skip direct transform overrides on the server/host because physics drives authoritatively
            if (no.IsServerInitialized) return;

            // 3. Directly update position and rotation immediately
            tf.position = targetOrientation.positionData;
            tf.rotation = targetOrientation.rotationData;
        }


        
        #endregion Methods


        #region Constructors
        public NetworkClientOrientationHandler(NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {
            
            
        }

        #endregion Constructors
    }
}
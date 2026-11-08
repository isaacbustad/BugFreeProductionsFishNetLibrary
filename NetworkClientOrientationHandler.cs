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

        // Last recieved server tick
        // Defaulted to max value to prevent 0 0 correction ignore
        protected uint lastReceivedServerTick = uint.MaxValue;

        
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

            if (lastSentOrientation.HasValue)
            {
                float positionDeltaSqr = (currentOrientation.positionData - lastSentOrientation.Value.positionData).sqrMagnitude;
                float rotationDelta = Quaternion.Angle(currentOrientation.rotationData, lastSentOrientation.Value.rotationData);

                // Skip sending if movement is below threshold (saves bandwidth when resting)
                if (positionDeltaSqr < no.Threshold * no.Threshold && rotationDelta < 0.1f)
                {
                    return; 
                }
            }

            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentOrientation);
            byte[] payload = OrientationDataSerializer.SerializePackage(packet);

            if (payload != null)
            {
                no.SendClientOrientation(lastReceivedServerTick, payload);
                lastSentOrientation = currentOrientation;

                if (no.OrientationReconciliation != null && !no.IsHostInitialized)
                {
                    Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                    float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                    no.OrientationReconciliation.PushCorrection(tickDuration);
                }
            }
        }


        

        public void CorrectClientOrientation(uint aServerTick, byte[] correctionPayload)
        {
            Debug.Log("Set correct Position through targeted RPC");
            // set the last recieved server tick received
            lastReceivedServerTick = aServerTick;

            // Fallback direct snap if no updater is assigned in the inspector
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.DeserializePackage(correctionPayload);
            OrientationData orientation = OrientationDataSerializer.DeserializeOrientation(packet);
            tf.OrientationData(orientation);
        }




        public void ReceiveOrientationObserverRPC(uint aServerTick, byte[] payload)
        {
            // set the last received server tick
            lastReceivedServerTick = aServerTick;
            

            // 1. Deserialize the absolute packet from the server payload
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.DeserializePackage(payload);
            OrientationData targetOrientation = OrientationDataSerializer.DeserializeOrientation(packet);

            // 2. Push state into reconciliation for smooth visual interpolation
            if (no.OrientationReconciliation != null)
            {
                // Pull velocity from Rigidbody if available, otherwise zero
                Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                no.OrientationReconciliation.PushCorrection(tickDuration);
            }

            // 3. Skip direct transform overrides on the server/host because physics drives authoritatively
            if (no.IsHostInitialized) return;

            // 4. Directly update physical position and rotation immediately (world always up to date)
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
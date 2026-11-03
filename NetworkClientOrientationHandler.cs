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

            // Call the 13-byte absolute serializer
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentOrientation);
            byte[] payload = OrientationDataSerializer.SerializePackage(packet);

            if (payload != null)
            {
                no.SendClientOrientation(payload);
                lastSentOrientation = currentOrientation;

                // Feed the local owner's reconciliation buffer so their visual interpolates smoothly too!
                if (no.OrientationReconciliation != null)
                {
                    Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                    float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                    no.OrientationReconciliation.PushOrientationState(currentOrientation, currentVelocity, tickDuration);
                }
            }
        }

        public void CorrectPosition(Vector3 correctPosition)
        {
            Debug.Log("Set correct Position through targeted RPC");
            
            // Fallback direct snap if no updater is assigned in the inspector
            tf.position = correctPosition;
        }

        public void CorrectPositionDelta(byte[] payload)
        {
            // 1. Deserialize the absolute packet from the server payload
            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.DeserializePackage(payload);
            OrientationData targetOrientation = OrientationDataSerializer.DeserializeOrientation(packet);

            // 2. Push state into reconciliation for smooth visual interpolation
            if (no.OrientationReconciliation != null)
            {
                // Pull velocity from Rigidbody if available, otherwise zero
                Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                no.OrientationReconciliation.PushOrientationState(targetOrientation, currentVelocity, tickDuration);
            }

            // 3. Skip direct transform overrides on the server/host because physics drives authoritatively
            if (no.IsServerInitialized) return;

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
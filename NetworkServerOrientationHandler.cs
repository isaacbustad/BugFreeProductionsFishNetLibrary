// Created By   :   Isaac Bustad
// Created      :   10/12/2026

using System;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using BugFreeProductions.Spatial;
using System.Collections.Generic;

namespace BugFreeProductions.Networking
{
    public class NetworkServerOrientationHandler : NetworkOrientationHandler
    {
        #region Vars
        // last known good position
        protected Vector3? lastSentPosition = null;

        // target calculated position
        protected Vector3 targetPosition = Vector3.zero;

        protected Queue<OrientationData> pendingOrientations = new Queue<OrientationData>();


        // hold the clients sync tick if corrected
        protected uint? correctionTick = null;
        #endregion Vars

        #region Methods

        public override void OnPreTick()
        {
            // if I am the owner or
            if (no.Owner.IsLocalClient) return;
            

            // store the current good position
            lastSentPosition = tf.position;

            // apply all pending deltas
            if (pendingOrientations.Count > 0)
            {
                targetPosition = tf.position;

                while (pendingOrientations.Count > 0)
                {
                    OrientationData latest = pendingOrientations.Dequeue();
                    targetPosition = latest.positionData;
                    tf.rotation = latest.rotationData; // Apply absolute rotation immediately
                }

                tf.position = targetPosition;
            }
        }

        public override void OnPostTick()
        {
            // if the local client owns this do nothing in PreTick, handle broadcast here
            if (no.Owner.IsLocalClient)
            {
                OrientationData currentState = tf.OrientationData();

                if (lastSentPosition.HasValue)
                {
                    float positionDeltaSqr = (currentState.positionData - lastSentPosition.Value).sqrMagnitude;
                    float rotationDelta = Quaternion.Angle(currentState.rotationData, tf.rotation);

                    // Skip broadcasting if movement is below threshold to save bandwidth
                    if (positionDeltaSqr < no.Threshold * no.Threshold && rotationDelta < 0.1f)
                    {
                        return;
                    }
                }

                // Host player continuous broadcast
                AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentState);
                byte[] payload = OrientationDataSerializer.SerializePackage(packet);

                if (payload != null)
                {
                    no.SendServerOrientation(no.TimeManager.Tick, payload);
                    lastSentPosition = currentState.positionData;

                    // set the tick a client must reach before we can correct
                    correctionTick = no.TimeManager.Tick;
                }

                if (no.OrientationReconciliation != null)
                {
                    // Pull velocity from Rigidbody if available, otherwise zero
                    //Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                    float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                    no.OrientationReconciliation.PushCorrection(tickDuration);
                }

                return;
            } 
            else
            {
                // Server-managed object broadcast/correction loop
                float discrepancy = Vector3.Distance(tf.position, targetPosition);

                if (discrepancy > 0.05f) 
                {
                    Debug.Log("Correction sent to client");
                    correctionTick = no.TimeManager.Tick;
                    

                    AbsoluteOrientationDataPacket correctionPacket = OrientationDataSerializer.SerializeAbsoluteOrientation(tf.OrientationData());
                    byte[] correctionPayload = OrientationDataSerializer.SerializePackage(correctionPacket);

                    if (correctionPayload != null)
                    {
                        no.SendServerOrientation(no.TimeManager.Tick, correctionPayload);                        

                        // set the tick a client must reach before we can correct
                        no.SendServerCorrection(correctionTick.Value, no.Owner, correctionPayload);
                    }
                }

                OrientationData currentState = tf.OrientationData();

                if (lastSentPosition.HasValue)
                {
                    float positionDeltaSqr = (currentState.positionData - lastSentPosition.Value).sqrMagnitude;
                    float rotationDelta = Quaternion.Angle(currentState.rotationData, tf.rotation);

                    if (positionDeltaSqr < no.Threshold * no.Threshold && rotationDelta < 0.1f && discrepancy <= 0.05f)
                    {
                        return;
                    }
                }

                AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentState);
                byte[] payload = OrientationDataSerializer.SerializePackage(packet);

                if (payload != null)
                {
                    no.SendServerOrientation( no.TimeManager.Tick, payload);
                    lastSentPosition = currentState.positionData;
                }

                if (no.OrientationReconciliation != null)
                {
                    // Pull velocity from Rigidbody if available, otherwise zero
                    //Vector3 currentVelocity = (rb != null) ? rb.linearVelocity : Vector3.zero;
                    float tickDuration = (no.TimeManager != null) ? (float)no.TimeManager.TickDelta : 0.05f;

                    no.OrientationReconciliation.PushCorrection(tickDuration);
                }
            }
        }

        public void ReceiveClientOrientation(uint aServerTick, byte[] payload, NetworkConnection sender = null)
        {
            // Evaluate only if there is a correction tick value
            if(correctionTick.HasValue)
            {
                // If the client's echoed ACK tick matches or exceeds our correction tick, they have caught up!
                // (Make sure to handle uint wrap-around safely if comparing raw uints, or use a signed delta check)
                if (aServerTick >= correctionTick.Value)
                {
                    correctionTick = null; // Clear the gate!
                }
                else
                {
                    // This packet was generated before the client saw the correction. Drop or de-prioritize it.
                    return; 
                }
            }
            

            AbsoluteOrientationDataPacket packet = OrientationDataSerializer.DeserializePackage(payload);
            OrientationData orientation = OrientationDataSerializer.DeserializeOrientation(packet);

            pendingOrientations.Enqueue(orientation);
        }

        #endregion Methods

        #region Constructors
        public NetworkServerOrientationHandler(NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {
        }
        #endregion Constructors
    }
}
// Created By   :   Isaac Bustad
// Created      :   10/12/2026

using System;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using BugFreeProductions.Spatial;
using System.Collections;
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

        protected Queue<OrientationData> pendingDeltas = new Queue<OrientationData>();

        // Sleep state variables
        private bool isSleeping = false;
        private int stationaryTickCount = 0;
        private const int SleepThresholdTicks = 15; // ~1/4 second of no movement before sleeping
        #endregion Vars

        #region Methods

        public override void OnPreTick()
        {
            if (no.Owner.IsLocalClient) return;
            // Debug.Log(" Position = " + tf.position);

            // store the current good position
            lastSentPosition = tf.position;

            // apply all pending deltas
            if (pendingDeltas.Count > 0)
            {
                // set target position to the current position
                targetPosition = tf.position;

                // accumulate deltas
                while (pendingDeltas.Count > 0)
                {
                    targetPosition += pendingDeltas.Dequeue().positionData;
                }

                // assign new position
                tf.position = targetPosition;
            }
        }

        // 2. CHECKS immediately after physics finishes simulating for this tick
        // 2. CHECKS immediately after physics finishes simulating for this tick
public override void OnPostTick()
{
    // if the local client owns this do nothing
    if (no.Owner.IsLocalClient)
    {
        // Establish baseline on the very first frame, then skip this tick
        if (!lastSentPosition.HasValue)
        {
            lastSentPosition = tf.position;
            return;
        }

        // calculate the actual change delta
        Vector3 actualServerDelta = tf.position - lastSentPosition.Value;

        // Check if movement is below threshold to handle rest state
        if (actualServerDelta.sqrMagnitude < no.Threshold * no.Threshold)
        {
            stationaryTickCount++;
            lastSentPosition = tf.position; // Keep baseline pinned to prevent drift

            if (stationaryTickCount >= SleepThresholdTicks)
            {
                if (!isSleeping)
                {
                    isSleeping = true;
                    Debug.Log("[ServerOrientation] Host object went to SLEEP.");
                }
                return; // Skip serialization and network send while resting
            }
        }
        else
        {
            if (isSleeping)
            {
                Debug.Log("[ServerOrientation] Host object WOKE UP.");
            }
            stationaryTickCount = 0;
            isSleeping = false;
        }

        // Host player broadcast
        byte[] payload = OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold);
        if (payload != null)
        {
            no.SendServerOrientationDeltas(payload);
        }

        // store the current good position
        lastSentPosition = tf.position;
        return;
    } 
    else
    {
        // Ensure we have a baseline value, otherwise default to current position
        Vector3 baseline = lastSentPosition ?? tf.position;
        Vector3 actualServerDelta = tf.position - baseline;

        // Check if movement is below threshold to handle rest state
        if (actualServerDelta.sqrMagnitude < no.Threshold * no.Threshold)
        {
            stationaryTickCount++;
            lastSentPosition = tf.position; // Keep baseline pinned

            if (stationaryTickCount >= SleepThresholdTicks)
            {
                if (!isSleeping)
                {
                    isSleeping = true;
                    Debug.Log("[ServerOrientation] Server-managed object went to SLEEP.");
                }
                return; // Skip calculations and sends while resting
            }
        }
        else
        {
            if (isSleeping)
            {
                Debug.Log("[ServerOrientation] Server-managed object WOKE UP.");
            }
            stationaryTickCount = 0;
            isSleeping = false;
        }

        // If a collision or personal space boundary blocked movement, 
        // position will differ from targetPosition.
        float discrepancy = Vector3.Distance(tf.position, targetPosition);

        if (discrepancy > 0.05f) 
        {
            Debug.Log("Correction sent to client");
            no.SendServerCorrection(no.Owner, tf.position);
            
            // Broadcast corrected delta only if payload is valid
            byte[] payload = OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold);
            if (payload != null)
            {
                no.SendServerOrientationDeltas(payload);
            }
        }
        else
        {
            byte[] payload = OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold);
            if (payload != null)
            {
                no.SendServerOrientationDeltas(payload);
            }
        }

        // CRITICAL FIX: Always update the baseline position for the next tick delta calculation!
        lastSentPosition = tf.position;
    }
}
        public void ReceiveClientOrientation(byte[] payload, NetworkConnection sender = null)
        {
            // Unpack the incoming 8 bytes back into your structured packet data
            OrientationDataPacket packet = OrientationDataSerializer.Deserialize(payload);

            // Reconstruct delta from qx/qy/qz using mask and scaleFactor, 
            OrientationData od = OrientationDataSerializer.DeserializeOrientation(payload);

            // then apply to server-side object state.
            pendingDeltas.Enqueue(od);
        }

        #endregion Methods

        #region Constructors
        public NetworkServerOrientationHandler(NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {
        }
        #endregion Constructors

        #region Accessors
        #endregion Accessors
    }
}
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
        #endregion Vars

        #region Methods

        public override void OnPreTick()
        {
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
                if (!lastSentPosition.HasValue)
                {
                    lastSentPosition = tf.position;
                    return;
                }

                // Host player continuous broadcast (No sleep lockouts)
                OrientationData currentState = tf.OrientationData();
                AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentState);
                byte[] payload = OrientationDataSerializer.SerializePackage(packet);

                if (payload != null)
                {
                    no.SendServerOrientationDeltas(payload);
                }

                lastSentPosition = tf.position;
                return;
            } 
            else
            {
                // Server-managed object broadcast/correction loop
                float discrepancy = Vector3.Distance(tf.position, targetPosition);

                if (discrepancy > 0.05f) 
                {
                    Debug.Log("Correction sent to client");
                    no.SendServerCorrection(no.Owner, tf.position);
                }

                OrientationData currentState = tf.OrientationData();
                AbsoluteOrientationDataPacket packet = OrientationDataSerializer.SerializeAbsoluteOrientation(currentState);
                byte[] payload = OrientationDataSerializer.SerializePackage(packet);

                if (payload != null)
                {
                    no.SendServerOrientationDeltas(payload);
                }

                lastSentPosition = tf.position;
            }
        }

        public void ReceiveClientOrientation(byte[] payload, NetworkConnection sender = null)
        {
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
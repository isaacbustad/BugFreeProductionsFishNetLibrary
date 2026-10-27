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
        // Hold if there are unapplied pending delta changes
        //protected bool hasPendingDelta = false;

        // last known good position
        protected Vector3 lastSentPosition;

        // target calculated position
        protected Vector3 targetPosition = Vector3.zero;


        protected Queue<OrientationData> pendingDeltas = new Queue<OrientationData>();


        #endregion Vars
        




        #region Methods

        #region Lifetime
        public override void OnNetStart()
        {
            //if(no.IsOwner)
            no.TimeManager.OnPreTick += OnPreTick;
        }

        public override void OnNetStop()
        {
            no.TimeManager.OnPreTick += OnPostTick;
        }


        #endregion Lifetime

       

        public override void OnPreTick()
        {
            // store the current good position
            lastSentPosition = tf.position;

            if (no.Owner.IsLocalClient) return;
            Debug.Log(" Position = " + tf.position);
            

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
        public override void OnPostTick()
        {
            // calculate the actual change delta
            Vector3 actualServerDelta = tf.position - lastSentPosition;
            
            // if the local client owns this do nothing
            if (no.Owner.IsLocalClient)
            {
                Debug.Log("pending delta should be but owner is host");
                // Host player broadcast
                byte[] payload = OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold);
                if (payload != null)
                {
                    no.SendServerOrientationDeltas(payload);
                }
                return;
            } 

            else
            {
                Debug.Log("calculates discrepencies, and comunicates corrections");
                // If a collision or personal space boundary blocked movement, 
                // rb.position will differ from _intendedPosition.
                float discrepancy = Vector3.Distance(tf.position, targetPosition);

                if (discrepancy > 0.05f) 
                {
                    Debug.Log("Correction sent to client");
                    no.SendServerCorrection(no.Owner, tf.position);

                    
                    // Broadcast THAT corrected delta to all other observers so everyone sees the wall collision
                    no.SendServerOrientationDeltas(OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold));
                }

                else
                {
                    no.SendServerOrientationDeltas(OrientationDataSerializer.Serialize(actualServerDelta,no.Threshold));
                    
                }
            }
            
            
            
            
        }

        

        public void ReceiveClientOrientation(byte[] payload, NetworkConnection sender = null)
        {
            // Unpack the incoming 8 bytes back into your structured packet data
            OrientationDataPacket packet = OrientationDataSerializer.Deserialize(payload);

            // Temporary debug log to prove the data flow works end-to-end!
            // Debug.Log($"[Server] Received Packet ->" +
            //           $"Axes [Qx: {packet.qx}, Qy: {packet.qy}, Qz: {packet.qz}] | " +
            //           $"Mask: {Convert.ToString(packet.mask, 2).PadLeft(3, '0')}");

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
using System;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using BugFreeProductions.Spatial;



namespace BugFreeProductions.Networking
{
    public class NetworkServerOrientationHandler : NetworkOrientationHandler
    {
        #region Vars
        // Hold if there are unapplied pending delta changes
        protected bool hasPendingDelta = false;

        //
        protected Vector3 targetPosition;

        OrientationData pendingDelta;



        protected Vector3 lastSentPosition;

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
            if (no.Owner.IsLocalClient) return;

            // Debug.Log("Server Runs Pretick and applies pending deltas");
            if (hasPendingDelta)
            {
                targetPosition = tf.position + pendingDelta.positionData;
                tf.position = targetPosition;
                //hasPendingDelta = false;
            }
        }

        // 2. CHECKS immediately after physics finishes simulating for this tick
        public override void OnPostTick()
        {
            // If there is no pending delta do nothing
            if (!hasPendingDelta) return;
            Debug.Log("pending delta should be applied");
            // if the local client owns this do nothing
            if (no.Owner.IsLocalClient)
            {
                Debug.Log("pending delta should be but owner is host");
                no.SendServerOrientationDeltas(OrientationDataSerializer.Serialize(pendingDelta.positionData,no.Threshold));
                return;
            } 

            else
            {
                // Debug.Log("Server Runs Posttick, calculates discrepencies, and comunicates corrections");
                // If a collision or personal space boundary blocked movement, 
                // rb.position will differ from _intendedPosition.
                float discrepancy = Vector3.Distance(tf.position, targetPosition);

                if (discrepancy > 0.05f) 
                {
                    no.SendServerCorrection(no.Owner, tf.position);

                    // 2. Calculate the ACTUAL movement the server allowed (from start to final position)
                    Vector3 actualServerDelta = tf.position - (targetPosition - pendingDelta.positionData);

                    // 3. Broadcast THAT corrected delta to all other observers so everyone sees the wall collision
                    no.SendServerOrientationDeltas(OrientationDataSerializer.Serialize(actualServerDelta, no.Threshold));
                }

                else
                {
                    no.SendServerOrientationDeltas(OrientationDataSerializer.Serialize(pendingDelta.positionData,no.Threshold));
                    
                }
            }
            
            hasPendingDelta = false;
            
            
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
            pendingDelta = od;
            hasPendingDelta = true;
        }

        #endregion Methods

        #region Constructors
        public NetworkServerOrientationHandler(Rigidbody aRB, NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {

        }

        #endregion Constructors


        #region Accessors
        public OrientationData PendingDelta
        {
            get
            {
                return pendingDelta;
            }
        }

        #endregion Accessors

    }
}
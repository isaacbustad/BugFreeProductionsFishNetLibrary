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
        // Hold if there are unaplied pending delta changes
        protected bool hasPendingDelta = false;

        //
        protected Vector3 targetPosition;

        OrientationData pendingDelta;

        #endregion Vars
        




        #region Methods

        #region Lifetime
        public override void OnNetStart()
        {
            if(no.IsOwner)
            no.TimeManager.OnPreTick += OnPreTick;
        }

        public override void OnNetStop()
        {
            no.TimeManager.OnPreTick += OnPostTick;
        }


        #endregion Lifetime

        public override void OnPreTick()
        {
            if (hasPendingDelta)
            {
                targetPosition = rb.position + pendingDelta.positionData;
                rb.MovePosition(targetPosition);
                hasPendingDelta = false;
            }
        }

        // 2. CHECKS immediately after physics finishes simulating for this tick
        public override void OnPostTick()
        {
            // If a collision or personal space boundary blocked movement, 
            // rb.position will differ from _intendedPosition.
            float discrepancy = Vector3.Distance(rb.position, targetPosition);

            if (discrepancy > 0.05f) 
            {
                no.SendCorrection(no.Owner, rb.position);
            }
        }

        protected void TargetSendCorrection(NetworkConnection conn, Vector3 correctPosition)
        {
            // This code ONLY runs on the specific client targeted by 'conn'.
            // Feed this correctPosition into your interpolation/smoothing buffer!
            Debug.Log($"[Client] Received server correction. Snapping/Smoothing to: {correctPosition}");
        }

        public void RecieceCmdSendOrientation(byte[] payload, NetworkConnection sender = null)
        {
            // Unpack the incoming 8 bytes back into your structured packet data
            OrientationDataPacket packet = OrientationDataSerializer.Deserialize(payload);

            // Temporary debug log to prove the data flow works end-to-end!
            Debug.Log($"[Server] Received Packet ->" +
                      $"Axes [Qx: {packet.qx}, Qy: {packet.qy}, Qz: {packet.qz}] | " +
                      $"Mask: {Convert.ToString(packet.mask, 2).PadLeft(3, '0')}");

            // Reconstruct delta from qx/qy/qz using mask and scaleFactor, 
            OrientationData od = OrientationDataSerializer.DeserializeOrientation(packet);

            // then apply to server-side object state.
            pendingDelta = od;
        }

        #endregion Methods

        #region Constructors
        public NetworkServerOrientationHandler(Rigidbody aRB, NetworkOrientation aNetworkOrientation) : base(aRB,aNetworkOrientation)
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
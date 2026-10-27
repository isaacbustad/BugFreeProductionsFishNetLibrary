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
        protected Vector3 lastSentPosition;


        #endregion Vars



        #region Methods

        #region Lifetime
        public override void OnNetStart()
        {
            no.TimeManager.OnPreTick += OnPreTick;
        }

        public override void OnNetStop()
        {
            no.TimeManager.OnPreTick += OnPostTick;
        }

        #endregion Lifetime

         #region Ownership  
        
        #endregion Ownership

        public override void OnPreTick()
        {
            
        }

        // CHECKS immediately after physics finishes simulating for this tick
        public override void OnPostTick()
        {
            if (!no.IsOwner) return;

            if (lastSentPosition == null)
            {
                lastSentPosition = tf.position;
            }

            Vector3 currentPosition = tf.position;
            Vector3 delta = currentPosition - lastSentPosition;

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
            // move target to the correct position sent by Server
            tf.position = correctPosition;
        }

        public void CorrectPositionDelta(byte[] payload)
        {
            // do nothing if this is a host client
            if (no.IsServerInitialized) return;

            // Correct the position of an object based on its Delta of change from server
            
            tf.position += OrientationDataSerializer.DeserializeOrientation(payload).positionData;
        }


        
        #endregion Methods


        #region Constructors
        public NetworkClientOrientationHandler(NetworkOrientation aNetworkOrientation) : base(aNetworkOrientation)
        {
            
            
        }

        #endregion Constructors
    }
}
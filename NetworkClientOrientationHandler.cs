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

        

        public override void OnPreTick()
        {
            if (!no.IsOwner) return;

            if (lastSentPosition == null)
            {
                lastSentPosition = rb.position;
            }

            Vector3 currentPosition = rb.position;
            Vector3 delta = currentPosition - lastSentPosition;

            // Call serializer to get the 8-byte payload (or null if stationary)
            byte[] payload = OrientationDataSerializer.Serialize( delta, no.Threshold);

            if (payload != null)
            {
                // Send payload over the network via ServerRpc
                no.SendOrientation(payload);
                
                // Update baseline position for the next tick delta calculation
                lastSentPosition = currentPosition;
            }
        }

        // CHECKS immediately after physics finishes simulating for this tick
        public override void OnPostTick()
        {
            
        }


        
        #endregion Methods


        #region Constructors
        public NetworkClientOrientationHandler(Rigidbody aRB, NetworkOrientation aNetworkOrientation) : base(aRB,aNetworkOrientation)
        {

        }

        #endregion Constructors
    }
}
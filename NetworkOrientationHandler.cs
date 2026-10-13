using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Timing;

namespace BugFreeProductions.Networking
{
    public abstract class NetworkOrientationHandler
    {
        #region Vars
        protected Rigidbody rb = null;

        protected NetworkOrientation no = null;

        #endregion Vars



        #region Methods

        #region Lifetime
        public abstract void OnNetStart();
        public abstract void OnNetStop();

        #endregion Lifetime
        

        #region Network Tick
        public abstract void OnPreTick();
        public abstract void OnPostTick();
        #endregion Network Tick



        #endregion Methods



        #region Constructors
        public NetworkOrientationHandler(Rigidbody aRB, NetworkOrientation aNetworkOrientation)
        {
            rb = aRB;
            no = aNetworkOrientation;
        }


        #endregion Constructors
    }
}
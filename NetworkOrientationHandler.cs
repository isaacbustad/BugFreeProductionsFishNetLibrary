using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Timing;

namespace BugFreeProductions.Networking
{
    public abstract class NetworkOrientationHandler
    {
        #region Vars
        protected Transform tf = null;

        protected NetworkOrientation no = null;

        #endregion Vars



        #region Methods
        

        #region Network Tick
        public abstract void OnPreTick();
        public abstract void OnPostTick();
        #endregion Network Tick


        



        #endregion Methods



        #region Constructors
        public NetworkOrientationHandler(NetworkOrientation aNetworkOrientation)
        {
            // Assign Vars
            tf = aNetworkOrientation.transform;
            no = aNetworkOrientation;

            // AddEvents
            aNetworkOrientation.TimeManager.OnPreTick += OnPreTick;
            aNetworkOrientation.TimeManager.OnPostTick += OnPostTick;
        }


        #endregion Constructors
    }
}
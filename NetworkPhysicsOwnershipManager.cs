// Created By   :   Isaac Bustad
// Created      :   11/02/2026

using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

namespace BugFreeProductions.Networking
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkPhysicsOwnershipManager : NetworkBehaviour
    {
        #region Vars
        private Rigidbody rb;
        #endregion Vars

        #region Unity Methods
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
        #endregion Unity Methods

        #region FishNet Overrides
        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdatePhysicsState();
        }

        // Called on the server when object ownership changes
        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            UpdatePhysicsState();
        }

        // Called on the client when object ownership changes
        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            UpdatePhysicsState();
        }
        #endregion FishNet Overrides

        #region Physics State Management
        private void UpdatePhysicsState()
        {
            // 1. The server is always authoritative and must simulate physics.
            if (base.IsServerInitialized)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                return;
            }

            // 2. If this client owns the object, enable local physics simulation.
            if (base.IsOwner)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            // 3. If this client is a non-owner, disable local physics 
            // so it stays kinematic and doesn't fight the spline interpolation updater.
            else
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        #endregion Physics State Management
    }
}
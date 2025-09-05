using Fusion;
using UnityEngine;

namespace Managers
{
    public class BlockerManager : NetworkBehaviour
    {
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void Rpc_RemoveStartBlocker()
        {
            gameObject.SetActive(false);
            Debug.Log("BlockerManager: Start blocker removed via RPC!");
        }
    }
}
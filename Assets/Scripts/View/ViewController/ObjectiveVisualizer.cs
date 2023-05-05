﻿using Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace View
{
    internal class ObjectiveVisualizer : MonoBehaviour
    {
        private enum PackageState
        {
            NotPickedUp,
            PickedUp,
            DroppedOff
        }

        public static ObjectiveVisualizer Instance { get; private set; }
        private Dictionary<string, GameObject> roleToPlayerGO = new();
        private Dictionary<string, GameObject> roleToPackageGO = new();
        private Dictionary<string, GameObject> roleToPackageDropoffGO = new();
        private Dictionary<string, int> roleToPackageSpawn = new();
        private Dictionary<string, int> roleToPackageDropoff = new();
        private Dictionary<int, List<Transform>> nodeToMarkers = new();
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject startPrefab;
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private float packageOverPlayerDistance = 3f;
        [SerializeField] private float groupDistance = 2f;


        private void Start()
        {
            Invoke(nameof(ShowObjectives), 1f);  // Must be delayed
        }
        public GameObject GetPlayerGO(string role)
        {
            if (roleToPlayerGO.ContainsKey(role))
                return roleToPlayerGO[role];
            return null;
        }
        private void ShowObjectives()
        {
            Instance = this;
            GameStateSynchronizer.Instance.StateChanged += StateChanged;
            foreach (var player in GameStateSynchronizer.Instance.GameState.Value.players)
            {
                if (player.unique_id == GameStateSynchronizer.Instance.Orchestrator.Value.unique_id)
                    continue;
                NetworkData.PlayerObjectiveCard? card = player.objective_card;
                if (card == null) Debug.LogError("Verily, what sorcery dost thou speaketh?");
                string roleName = player.in_game_id;
                NetworkData.InGameID role =
                    (NetworkData.InGameID)Enum.Parse(typeof(NetworkData.InGameID), roleName);

                int packageSpawnId = card.Value.pick_up_node_id;
                int playerSpawnId = card.Value.start_node_id;
                int packageDropoffId = card.Value.drop_off_node_id;

                roleToPackageSpawn.Add(roleName, packageSpawnId);
                roleToPackageDropoff.Add(roleName, packageDropoffId);

                roleToPlayerGO.Add(roleName, Spawn(playerPrefab, playerSpawnId, role));
                Spawn(startPrefab, playerSpawnId, role);
                roleToPackageGO.Add(roleName, Spawn(pickupPrefab, packageSpawnId, role));
                roleToPackageDropoffGO.Add(roleName, Spawn(goalPrefab, packageDropoffId, role));
            }
        }
        private void StateChanged(NetworkData.GameState? state)
        {
            if (state == null) return;
            foreach (var player in state.Value.players)
            {
                string roleName = player.in_game_id;
                if (roleName == NetworkData.InGameID.Orchestrator.ToString())
                    continue;
                NetworkData.PlayerObjectiveCard card = player.objective_card.Value;
                PackageState packageState;
                if (card.dropped_package_off)
                    packageState = PackageState.DroppedOff;
                else if (card.picked_package_up)
                    packageState = PackageState.PickedUp;
                else
                    packageState = PackageState.NotPickedUp;
                
                Transform packageTransform = roleToPackageGO[roleName].transform;
                Transform playerTransform = roleToPlayerGO[roleName].transform;
                int packageDropoff = roleToPackageDropoff[roleName];
                int packageSpawn = roleToPackageSpawn[roleName];
                Transform dropoffTransform = roleToPackageDropoffGO[roleName].transform;

                Vector2 targetPos;
                if (packageState == PackageState.PickedUp)
                {
                    // Animate this separately
                    ObjectiveGrouper.Instance.Move(packageTransform, null);
                    packageTransform.SetParent(playerTransform, true);
                    targetPos = Vector2.up*packageOverPlayerDistance;
                    ObjectiveGrouper.Instance.DoAnimation(packageTransform, targetPos);
                } else
                {
                    // Animate this like other markers
                    int nodeId;
                    if (packageState == PackageState.NotPickedUp)
                        nodeId = packageSpawn;
                    else  // packageState == PackageState.DroppedOff
                        nodeId = packageDropoff;
                    ObjectiveGrouper.Instance.Move(packageTransform, nodeId);
                }

                // Always add package dropoff to group
                ObjectiveGrouper.Instance.Move(dropoffTransform, packageDropoff);
            }
        }
        private GameObject Spawn(GameObject prefab, int nodeId, NetworkData.InGameID role)
        {
            GameObject go = PoolManager.Depool(prefab);
            INode node = GraphManager.Instance.GetNode(nodeId);
            go.GetComponent<PlayerOwned>().Owner = role;
            go.transform.SetParent(node.gameObject.transform, false);
            return go;
        }
    }
}
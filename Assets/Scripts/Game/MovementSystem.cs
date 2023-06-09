﻿using Common.Network;
using System.Collections.Generic;
using UnityEngine;
using Common;

namespace Game
{
    internal class MovementSystem : MonoBehaviour
    {
        public static MovementSystem Instance { get; private set; }
        private Dictionary<string, int> playerPositions = new();
        private void Awake()
        {
            Instance = this;
        }
        private void OnEnable()
        {
            GameStateSynchronizer.Instance.StateChanged += StateChanged;
        }
        private void OnDisable()
        {
            GameStateSynchronizer.Instance.StateChanged -= StateChanged;
        }
        private void StateChanged(NetworkData.GameState? state)
        {
            foreach (var player in state.Value.players)
            {
                string role = player.in_game_id;
                if (role == NetworkData.InGameID.Orchestrator.ToString())
                    continue;
                if (!player.position_node_id.HasValue)
                    continue;
                int newPos = player.position_node_id.Value;
                int oldPos;
                if (!playerPositions.ContainsKey(role))
                {
                    oldPos = -100;
                    playerPositions.Add(role, newPos);
                } else
                {
                    oldPos = playerPositions[role];
                }
                
                if (newPos != oldPos)
                {
                    if (ObjectiveVisualizer.Instance == null) break;
                    GameObject playerGO = ObjectiveVisualizer.Instance.GetPlayerGO(role);
                    if (playerGO == null) continue;
                    Transform playerTransform = playerGO.transform;
                    Transform targetTransform = GraphManager.Instance.GetNode(newPos).gameObject.transform;
                    playerPositions[role] = newPos;
                    playerTransform.parent = targetTransform;
                    SpriteRenderer playerSprite = playerTransform.GetComponent<SpriteRenderer>();
                    if (targetTransform.position.x - playerTransform.position.x < 0)
                    {
                        playerSprite.flipX = true;
                    }
                    else if (targetTransform.position.x - playerTransform.position.x > 0) 
                    {
                        playerSprite.flipX = false;
                    }
                    Animation<Vector2> moveAnimation = new()
                    {
                        StartValue = playerTransform.position,
                        EndValue = targetTransform.position,
                        Duration = AnimationPresets.Instance.PackageMoveDuration,
                        Curve = AnimationPresets.Instance.PackageMoveCurve,
                        Action = (value) => { playerTransform.position = value; }
                    };
                    moveAnimation.Start();
                }
            }
        }
        public void ClickNode(NodeTraversal trav)
        {
            NetworkData.PlayerInput input = new()
            {
                player_id = NetworkData.Instance.UniqueID,
                game_id = GameStateSynchronizer.Instance.LobbyId.Value,
                input_type = NetworkData.PlayerInputType.Movement.ToString(),
                related_role = GameStateSynchronizer.Instance.Me.in_game_id,
                related_node_id = trav.Id,
            };
            RestAPI.Instance.SendPlayerInput(success =>
            {
                UndoSystem.Instance.MovesDone++;
            }, failure =>
            {

            }
            , input);
        }
    }
}

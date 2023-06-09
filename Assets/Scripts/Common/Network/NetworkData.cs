﻿using System;
using System.Collections.Generic;
using UnityEngine;
namespace Common.Network
{
    /// <summary>
    /// Contains every enum and struct used for serialization and deserialization.
    /// Also keeps track of the player's name and unique id.
    /// Use <see cref="GameStateSynchronizer"/> to get game-related data.
    /// </summary>
    public class NetworkData : MonoBehaviour
    {
        public static NetworkData Instance { get; private set; }
        public int UniqueID => Me.Value.unique_id;
        public string PlayerName => Me.Value.name;
        public event Action<Player?> MeChanged;
        public Player? Me { get => me; set { me = value; MeChanged?.Invoke(me); } }
        private Player? me;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        // Enums
        /// <summary>
        /// Specifies a player's role in a lobby/game.
        /// </summary>
        [Serializable]
        public enum InGameID
        {
            Undecided = 0,
            PlayerOne = 1,
            PlayerTwo = 2,
            PlayerThree = 3,
            PlayerFour = 4,
            PlayerFive = 5,
            PlayerSix = 6,
            Orchestrator = 7
        }
        /// <summary>
        /// Clarifies which action the client wants to take
        /// </summary>
        [Serializable]
        public enum PlayerInputType
        {
            Movement,
            ChangeRole,
            All,  // Do not use! only used internally on backend
            NextTurn,  // End your own turn
            UndoAction,
            ModifyDistrict,  // Add restriction on orchestrator view
            StartGame,  // In lobby screen, start game
            AssignSituationCard,  // Used before starting game
            LeaveGame,
            ModifyEdgeRestrictions,
            SetPlayerBusBool,
        }
        [Serializable]
        public enum District
        {
            IndustryPark,
            Port,
            Suburbs,
            RingRoad,
            CityCentre,
            Airport
        }
        /// <summary>
        /// Used for restriction, access and displaying correct car types
        /// </summary>
        [Serializable]
        public enum RestrictionType
        {
            ParkAndRide = 0,
            Electric = 1,
            Emergency = 2,
            Hazard = 3,
            Destination = 4,
            Heavy = 5,
            OneWay = 6,
        }
        [Serializable]
        public enum DistrictModifierType
        {
            Access,
            Priority,
            Toll
        }
        [Serializable]
        public enum TypeEntitiesToTransport
        {
            People,
            Packages,
        }

        // Structs
        // All enums are sent and received as strings,
        // corresponding enum notated like (this)
        /// <summary>
        /// Wrapper, couldn't deserialize lists otherwise
        /// (for some reason)
        /// </summary>
        [Serializable]
        public struct LobbyList
        {
            public List<GameState> lobbies;
        }
        /// <summary>
        /// Contains all information there is about a lobby/game.
        /// </summary>
        [Serializable]
        public struct GameState
        {
            public int id;
            public string name;
            public List<Player> players;
            public bool is_lobby;
            public string current_players_turn;  // (InGameID)
            public List<DistrictModifier> district_modifiers;
            public SituationCard? situation_card;
            public List<EdgeRestriction> edge_restrictions;
            public List<int> legal_nodes;
        }
        [Serializable]
        public struct EdgeRestriction
        {
            public int node_one;
            public int node_two;
            public string edge_restriction;  // (RestrictionType)
            public bool delete;
        }
        [Serializable]
        public struct Player
        {
            public int? connected_game_id;
            public string in_game_id;  // InGameID
            public int unique_id;
            public string name;
            public int? position_node_id;
            public int remaining_moves;
            public PlayerObjectiveCard? objective_card;
            public bool is_bus;
        }
        [Serializable]
        public struct Node
        {
            public int id;
            public string name;
            public bool is_connected_to_rail;
            public bool is_parking_spot;
        }
        [Serializable]
        public struct NewGameInfo
        {
            public Player host;
            public string name;
        }
        [Serializable]
        public struct PlayerInput
        {
            public int player_id;
            public int game_id;
            public string input_type;  // (PlayerInputType)
            public string related_role;  // (InGameID)
            public int? related_node_id;
            public DistrictModifier? district_modifier;
            public int? situation_card_id;
            public EdgeRestriction? edge_modifier;
            public bool? related_bool;
        }
        [Serializable]
        public struct DistrictModifier
        {
            public string district;  // (District)
            public string modifier;  // (DistrictModifierType)
            public string vehicle_type;  // (RestrictionType)
            public int? associated_movement_value;
            public int? associated_money_value;
            public bool delete;
        }
        [Serializable] 
        public struct SituationCard
        {
            public int card_id;
            public string title;
            public string description;
            public string goal;
            public List<CostTuple> costs;
            public List<PlayerObjectiveCard> objective_cards;
        }
        [Serializable]
        public struct SituationCardList
        {
            public List<SituationCard> situation_cards;
        }
        [Serializable]
        public struct CostTuple
        {
            public string neighbourhood;  // (District)
            public string traffic;  // (Traffic)
        }
        [Serializable]
        public enum Traffic
        {
            LevelOne = 1,
            LevelTwo = 2, 
            LevelThree = 3, 
            LevelFour = 4,
            LevelFive = 5
        }
        /// <summary>
        /// Refers to assignment cards
        /// </summary>
        [Serializable]
        public struct PlayerObjectiveCard
        {
            public string name;
            public int start_node_id;
            public int pick_up_node_id;
            public int drop_off_node_id;
            public List<string> special_vehicle_types;  // (List<RestrictionType>)
            public bool picked_package_up;
            public bool dropped_package_off;
            public string type_of_entities_to_transport;  // TypeEntitiesToTransport
            public int amount_of_entities;
        }

        /// <summary>
        /// Helper method to save space
        /// </summary>
        /// <param name="text">the equivalent .ToString()</param>
        /// <returns></returns>
        public static InGameID StringToInGameId(string text)
            => (InGameID)Enum.Parse(typeof(InGameID), text);
        /// <summary>
        /// Used to sort players based on play order in-game.
        /// Orchestrator appears first, Undecided appears last.
        /// </summary>
        public static int PlayOrder(Player a, Player b)
            => PlayOrder(StringToInGameId(a.in_game_id), StringToInGameId(b.in_game_id));
        public static int PlayOrder(InGameID aRole, InGameID bRole)
        {
            int a = (int)aRole;
            int b = (int)bRole;
            if (aRole == InGameID.Orchestrator) a = 0;
            if (bRole == InGameID.Orchestrator) b = 0;
            if (aRole == InGameID.Undecided) a = 7;
            if (bRole == InGameID.Undecided) b = 7;
            return a - b;
        }
        public InGameID GetFirstAvailableRole(GameState state, bool skipOrchestrator)
        {  // Find a more appropriate location for this method
            List<InGameID> roles =
                new()
                {
                    InGameID.PlayerOne,
                    InGameID.PlayerTwo,
                    InGameID.PlayerThree,
                    InGameID.PlayerFour,
                    InGameID.PlayerFive,
                    InGameID.PlayerSix,
                    InGameID.Undecided,
                };
            if (!skipOrchestrator)
                roles.Insert(0, InGameID.Orchestrator);
            foreach (var player in state.players)
            {
                if (player.in_game_id == InGameID.Undecided.ToString()) continue;
                roles.Remove(StringToInGameId(player.in_game_id));
            }
            return roles[0];
        }
    }
}

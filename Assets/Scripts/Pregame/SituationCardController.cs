using System.Collections.Generic;
using UnityEngine;
using Common.Network;
using System;
using Common;

namespace Pregame
{
    public class SituationCardController : MonoBehaviour
    {
        public static SituationCardController Instance { get; private set; }

        public int ChosenCount => chosen.Count;
        public event Action SelectedCards;
        [field: SerializeField, Range(1, 10)] public int MaxCards { get; private set; } = 1;
        [SerializeField] private bool autoSelect = true;
        [SerializeField] private RectTransform cardsParent;
        [SerializeField] private GameObject cardPrefab;
        private Dictionary<int, SituationCard> gamecards = new();
        private List<SituationCard> chosen = new();
        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            RestAPI.Instance.GetSituationCards(
                (success) =>
                {
                    foreach (int id in gamecards.Keys)
                        PoolManager.Enpool(gamecards[id].gameObject);
                    gamecards = new();

                    foreach (NetworkData.SituationCard card in success.situation_cards)
                    {
                        // Organize data from backend
                        int id = card.card_id;
                        string title = card.title;
                        string description = card.description;
                        string goal = card.goal;
                        List<string> trafficList = new();
                        if (card.costs != null)
                            foreach (var traffic in card.costs)
                            {
                                trafficList.Add($"{traffic.neighbourhood}: {traffic.traffic}");
                            }
                        string traffics = string.Join("\n", trafficList);

                        // Write data to card
                        SituationCard gamecard = PoolManager.Depool(cardPrefab).GetComponent<SituationCard>();
                        gamecards.Add(id, gamecard);
                        gamecard.Click += Click;
                        gamecard.SetValues(card);

                        gamecard.gameObject.SetActive(true);
                        gamecard.transform.SetParent(cardsParent, false);
                    }
                    AutoSelectCard();
                },
                (failure) => { Debug.LogWarning("Could not get situation cards from server"); }
            );
        }
        private void AutoSelectCard()
        {
            // Unselect all while preventing errors
            List<SituationCard> temp = new();
            foreach (var card in chosen)
                temp.Add(card);
            foreach (var card in temp)
                Click(card);
            if (autoSelect)
                Click(gamecards[1]);
        }
        public void Refresh()
        {
            foreach (var card in gamecards.Values)
            {
                bool isSelected = chosen.Contains(card);
                card.Animator.SetBool("selected", isSelected);
            }
        }
        public void Click(SituationCard card)
        {
            bool add = !chosen.Contains(card);
            if (add) chosen.Add(card);
            else chosen.Remove(card);
            if (chosen.Count > MaxCards)
            {
                // Remove oldest
                chosen.RemoveAt(0);
            }

            Refresh();
            SelectedCards?.Invoke();
        }
        public void Confirm(Action<NetworkData.GameState> success, Action<string> failure)
        {
            SituationCard card = chosen[0];
            NetworkData.PlayerInput input = new()
            {
                player_id = NetworkData.Instance.UniqueID,
                game_id = GameStateSynchronizer.Instance.LobbyId.Value,
                input_type = NetworkData.PlayerInputType.AssignSituationCard.ToString(),
                related_role = NetworkData.InGameID.Orchestrator.ToString(),
                situation_card_id = card.Source.card_id
            };

            RestAPI.Instance.SendPlayerInput(
                (done) => success?.Invoke(done),
                (fail) => failure?.Invoke(fail),
                input
            );
        }

        private void OnEnable()
        {
            foreach (var card in gamecards.Values)
                card.Click -= Click;
            foreach (var card in gamecards.Values)
                card.Click += Click;
        }
        private void OnDisable()
        {
            foreach (var card in gamecards.Values)
                card.Click -= Click;
        }
    }
}

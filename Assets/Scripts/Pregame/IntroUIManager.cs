using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Common.Network;

namespace Pregame
{
    public class IntroUIManager : MonoBehaviour
    {
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private TextMeshProUGUI nameInput;
        [SerializeField] private TextMeshProUGUI errorMessage;
        private void Awake()
        {
            errorMessage.gameObject.SetActive(false);
        }
        public void Login()
        {
            errorMessage.gameObject.SetActive(false);
            RestAPI.Instance.CreateUniquePlayerId(
                (response) =>
                {
                    NetworkData.Instance.Me = new NetworkData.Player
                    {
                        connected_game_id = null,
                        in_game_id = NetworkData.InGameID.Undecided.ToString(),
                        unique_id = response,  // integer
                    name = nameInput.text,
                        position_node_id = null,
                        remaining_moves = 0
                    };
                    SceneManager.LoadSceneAsync(mainMenuScene);
                },
                (failure) =>
                {
                    errorMessage.text = $"Login failed: {failure}";
                    errorMessage.gameObject.SetActive(true);
                }
            );
        }
    }
}
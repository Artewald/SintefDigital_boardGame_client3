﻿using UnityEngine;
using UnityEngine.UI;

namespace Common
{
    using Network;
    [ExecuteAlways]
    public class PlayerOwned : MonoBehaviour
    {
        public NetworkData.InGameID Owner { get => owner; set { owner = value; Refresh(); } }
        [SerializeField] private new SpriteRenderer renderer;
        [SerializeField] private Image image;

        [SerializeField] private NetworkData.InGameID owner;

        public void SetColor(Color color)
        {
            if (renderer != null) renderer.color = color;
            if (image != null) image.color = color;
        }
        private void OnEnable()
        {
            if (renderer == null) renderer = GetComponent<SpriteRenderer>();
            if (image == null) image = GetComponent<Image>();
            Refresh();
        }
        private void OnValidate()
        {
            Refresh();
        }
        private void Refresh()
        {
            if (PlayerColorManager.Instance != null) PlayerColorManager.Instance.Refresh(this);
        }
    }
}

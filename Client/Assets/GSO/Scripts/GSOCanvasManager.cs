using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GSO
{
    public class GSOCanvasManager : MonoBehaviour
    {
        public GSOManager manager;

        public Text playerCountText;
        public Text objectCountText;

        public CanvasGroup connectingGroup;
        public CanvasGroup menuTipGroup;
        public CanvasGroup menuGroup;
        public CanvasGroup offlineModeGroup;

        public bool showMenu = false;

        public void Awake() {
            InitializeGroup(menuTipGroup);
            InitializeGroup(menuGroup);
            InitializeGroup(connectingGroup);
            InitializeGroup(offlineModeGroup);

            manager.ConnectionChangeEvent.AddListener(() => {
                connectingGroup.alpha = manager.simulationService.IsReady() ? 0 : 1;
                offlineModeGroup.alpha = manager.useOnline ? 0 : 1;
            });

            connectingGroup.alpha = manager.simulationService.IsReady() ? 0 : 1;
            offlineModeGroup.alpha = manager.useOnline ? 0 : 1;
        }

        public void Update() {
            playerCountText.text = $"{manager.simulationService.GetPlayerCount()} Players";
            objectCountText.text = $"{manager.simulationService.GetObjectCount()} Objects";

            menuTipGroup.alpha = showMenu ? 0 : 1;
            menuGroup.alpha = showMenu ? 1 : 0;

            if (Input.GetButtonDown("Menu")) {
                showMenu = !showMenu;
            }
        }

        private void InitializeGroup(CanvasGroup group) {

            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace TaiChi
{
    public class MenuButton : MonoBehaviour
    {
        public Button MenuBtn;
        public StartScreenController StartScreen;

        private void Start()
        {
            if (MenuBtn != null)
                MenuBtn.onClick.AddListener(() => StartScreen.ReturnToMenu());
            else
                Debug.LogError("[MenuButton] MenuBtn not assigned!");
        }
    }
}
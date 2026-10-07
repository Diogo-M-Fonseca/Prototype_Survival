using UnityEngine;

namespace SojaExiles
{
    public class opencloseDoor : Interactable
    {
        public Animator openandclose;
        public bool open;

        protected override string DefaultPrompt => open ? "close" : "open";

        void Start()
        {
            open = false;
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (openandclose == null) return;

            if (!open)
            {
                openandclose.Play("Opening");
                open = true;
            }
            else
            {
                openandclose.Play("Closing");
                open = false;
            }
        }
    }
}
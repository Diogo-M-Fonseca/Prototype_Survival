using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SojaExiles

{
    public class opencloseStallDoor : Interactable
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
                openandclose.Play("OpeningStall");
                open = true;
            }
            else
            {
                openandclose.Play("ClosingStall");
                open = false;
            }
        }
    }
}
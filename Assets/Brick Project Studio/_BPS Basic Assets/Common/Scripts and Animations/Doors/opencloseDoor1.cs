using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SojaExiles

{
    public class opencloseDoor1 : Interactable
    {
        public Animator openandclose1;
        public bool open;

        protected override string DefaultPrompt => open ? "close" : "open";

        void Start()
        {
            open = false;
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (openandclose1 == null) return;

            if (!open)
            {
                openandclose1.Play("Opening 1");
                open = true;
            }
            else
            {
                openandclose1.Play("Closing 1");
                open = false;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrassFlow.Examples {
    public class ContactRipple : MonoBehaviour {

        public float rippleRate = 0.1f;
        public float contactOffset = 1f;
        public Transform grassCol;
        public float ripStrength;
        public float ripDecayRate;
        public float ripSpeed;
        public float ripRadius;

        float timer = 0;

        private void OnCollisionStay(Collision collision) {
            if (enabled) {

                if (timer > rippleRate && collision.transform == grassCol.transform) {
                    timer = 0;

                    ContactPoint contact = collision.contacts[0];
                    GrassFlowRenderer.AddRipple(contact.point + contact.normal * contactOffset, ripStrength, ripDecayRate, ripSpeed, ripRadius, 0);
                }

                timer += Time.deltaTime;
            }
        }


    }
}

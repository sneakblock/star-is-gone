using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrassFlow.Examples {
    public class ContactFlatten: MonoBehaviour {

        public Collider grassCol;
        public Texture2D flatTex;
        public GrassFlowRenderer grassFlow;
        public float flatStrength;
        public float flatSize;
        public float coordOffset = 0.002f;


        private void Start() {
            GrassFlowRenderer.SetPaintBrushTexture(flatTex);
        }

        private void OnCollisionEnter(Collision collision) {
            HandleCollision(collision);
        }

        private void OnCollisionStay(Collision collision) {
            HandleCollision(collision);
        }

        void HandleCollision(Collision collision) {
            if (enabled) {
                if (collision.transform == grassCol.transform) {                    

                    ContactPoint contact = collision.contacts[0];

                    Ray ray = new Ray(contact.point + contact.normal * 0.1f, -contact.normal);
                    RaycastHit hit;
                    if (contact.otherCollider.Raycast(ray, out hit, 0.2f)) {
                        grassFlow.PaintParameters(hit.textureCoord - new Vector2(coordOffset, coordOffset), flatSize, flatStrength, 0, 0, -1, 0, new Vector2(0f, 0.9f));
                    }
                }
            }
        }

    }
}

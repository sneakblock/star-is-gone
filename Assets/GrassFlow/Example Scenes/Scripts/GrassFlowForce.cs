using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrassFlow.Examples {
    public class GrassFlowForce : MonoBehaviour {

        public float radius = 1f;
        public float strength = 0.75f;
        public float heightOffset = 1f;

        GrassFlowRenderer.GrassForce grassForce;

        void Start() {
            CreateForce();

            if(grassForce == null) {
                enabled = false;
            }
        }

        void OnEnable() {
            CreateForce();
        }
        
        void OnDisable() {
            DestroyForce();
        }

        void CreateForce() {
            if(grassForce == null) {
                grassForce = GrassFlowRenderer.AddGrassForce(GetPos(), radius, strength);
                if(grassForce == null) {
                    //too many forces or grassflow not initialized
                }
            }
        }

        void DestroyForce() {
            if(grassForce != null) {
                grassForce.Remove();
                grassForce = null;
            }
        }

        void OnValidate() {
            if(grassForce != null) {
                grassForce.strength = strength;
                grassForce.radius = radius;
            }
        }

        void LateUpdate() {
            grassForce.position = GetPos();
        }

        void OnDestroy() {
            DestroyForce();
        }

        Vector3 GetPos() {
            Vector3 pos = transform.position;
            pos.y += heightOffset;
            return pos;
        }

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class Wall : PlayerTarget {

        public Transform forcePos;
        public List<Rigidbody> parts;
        public GameObject text;
        public override void TakeDamage(float damage) {
            health.TakeDamage(damage);
            if (animationModule.Animator != null)
                animationModule.Play(animationModule.DamageAnim);
            hitEffect.Play();

            if (!health.isAlive) {
                StartCoroutine(Bang(0));
            }
            health.UpdateHealth(health.CurrentHealth);
        }
        IEnumerator Bang(float time) {
            yield return new WaitForSeconds(time);
            text.SetActive(false);
            gameObject.GetComponent<Collider>().enabled = false;
            for (int i = 0; i < parts.Count; i++) {
                var rb = parts[i];
                rb.isKinematic = false;
                rb.AddForceAtPosition(transform.forward * 150, transform.position - transform.right);
                parts[i].gameObject.AddComponent<WallParts>();
            }
        }

        IEnumerator PlayerBang(float time) {
            yield return new WaitForSeconds(time);
            text.SetActive(false);
            gameObject.GetComponent<Collider>().enabled = false;
            for (int i = 0; i < parts.Count; i++) {
                var rb = parts[i];
                rb.isKinematic = false;

                Vector3 direction = rb.transform.position - forcePos.position;
                rb.AddForceAtPosition(direction.normalized*50, forcePos.position);
            }
        }

        private void OnTriggerEnter(Collider other) {
            if (Player.TryGetCharacter(other, out Player p)) {
                p.healthModule.TakeDamage(damage);
                if (p.healthModule.IsAlive) {
                    StartCoroutine(PlayerBang(0.5f));
                   
                }
            }
        }
       
    }
}

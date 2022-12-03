using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Mobge.CountAndShoot {
    public class BulletText : MonoBehaviour {
        public Player player;
        public TMP_Text bulletText;
        private int bullet;
        void Start() {
            bullet = player.fireModule.levelBullet;
            bulletText.text = bullet.ToString();
        }
        public void ReduceUpdateText() {
            bullet--;
            bulletText.text = bullet.ToString();
        }
        public void IncreaseUpdateText() {
            bullet++;
            bulletText.text = bullet.ToString();
        }
    }
}
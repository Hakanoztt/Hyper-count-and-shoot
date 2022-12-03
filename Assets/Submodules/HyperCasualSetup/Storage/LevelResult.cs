using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    [Serializable]
    public class LevelResult
    {
        public float score;
        public int levelChallenge;
        public bool completed;

        public virtual void Merge(LevelResult r) {
            if (r == null) {
                return;
            }
            completed = completed || r.completed;
            score = Mathf.Max(score, r.score);
            levelChallenge = Mathf.Max(levelChallenge, r.levelChallenge);
        }
    }
}
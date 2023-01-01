using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup;
using Mobge.HyperCasualSetup.RoadGenerator;
using static Mobge.HyperCasualSetup.RoadGenerator.RunnerController;
using UnityEngine.UI;

namespace Mobge.CountAndShoot {
    public partial class Player : MonoBehaviour, IComponentExtension {
        public BaseLevelPlayer BaseLevelPlayer => _player;
        private BaseLevelPlayer _player;
        public RunnerController runnerController;

        public FireModule fireModule;
        public StackManager stackManager;
        public ThrowingModule throwingModule;
        public AnimModule animModule;
        public MovementModule movementModule;

        public HealthModule healthModule;
        public Indicator indicator;
        public BulletTextModule bulletTextModule;

        public ParticleEffect hitEffect;
        public ParticleEffect takeDamageEffect;
        public ParticleEffect plusOneEffect;

        public bool finishGame;

        public enum Mode { Stacking, Fire };
        public Mode CurrentMode { get; set; }
        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            _player = (BaseLevelPlayer)initData.player;
            stackManager.Init(this);
            throwingModule.Init(this);
            fireModule.Init(this, runnerController);
            healthModule.Init(this);
            indicator.Init(_player);
            _player.RoutineManager.DoRoutine(NUpdate);
        }
        private void NUpdate(float progress, object data) {
            fireModule.Update();
            healthModule.Update();
        }
        public static bool TryGetCharacter(Collider other, out Player player) {
            if (other.CompareTag(RunnerController.c_tag)) {
                if (other.TryGetComponent(out Player p)) {
                    player = p;
                    return true;
                }
            }
            player = null;
            return false;
        }
        private void PlayHitEffect(Vector3 position) {
            if (hitEffect != null) {
                hitEffect.transform.position = position;
                hitEffect.Play();
            }
        }
        private void PlayPlusOneEffect(Vector3 position) {
            if (plusOneEffect != null) {
                plusOneEffect.transform.position = position;
                plusOneEffect.Play();
            }
        }
        private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Ball")) {
                if (stackManager.bulletList.Contains(other.GetComponent<Ball>())) {
                    fireModule.SetCurrentBall(other.GetComponent<Ball>());
                    CurrentMode = Mode.Fire;
                } else {
                    var collectableBall = other.GetComponent<Ball>();
                    stackManager.Add(collectableBall);
                    collectableBall.gameObject.SetActive(false);
                    collectableBall.rb.useGravity = true;

                }
            }
            if (other.CompareTag("PowerUpBalls")) {
                var collectableBall = other.GetComponent<Ball>();
                stackManager.Add(collectableBall);
                collectableBall.gameObject.SetActive(false);
                collectableBall.rb.useGravity = true;
            }
            if (other.CompareTag("FinishLine")) {
                FinishGame();
            }

        }
        private void OnTriggerExit(Collider other) {
            if (other.CompareTag("Ball") && stackManager.bulletList.Contains(other.GetComponent<Ball>())) {
                fireModule.firedBullet++;
                var ball = other.GetComponent<Ball>();
                stackManager.Remove(ball);
                ball.gameObject.SetActive(false);
                fireModule.levelBullet--;

                bulletTextModule.bulletText.ReduceUpdateText();

                if (fireModule.firedBullet == Math.Ceiling(fireModule.howManyBullet)) {
                    animModule.Play(animModule.runAnim);
                    CurrentMode = Mode.Stacking;
                    indicator.Hide();
                }
                fireModule.SetCurrentBall(null);
            }
        }
        void Death() {
            if (healthModule.isDead) return;
            healthModule.isDead = true;
            movementModule.playerSpeed = 0;

            for (int i = 0; i < indicator.images.Length; i++) {
                indicator.images[i].gameObject.SetActive(false);
            }
            bulletTextModule.bulletText.gameObject.SetActive(false);

            runnerController.moveData.speed = 0;

            BaseLevelPlayer.RoutineManager.DoAction((complete, data) => {

                animModule.Play(animModule.Death);

            }, healthModule.deathAnimDelay);

            _player.FinishGame(false);

        }

        void FinishGame() {
            runnerController.moveData.speed = 0;
            for (int i = 0; i < indicator.images.Length; i++) {
                indicator.images[i].gameObject.SetActive(false);
            }
            bulletTextModule.bulletText.gameObject.SetActive(false);
            animModule.Play(animModule.Dance);
            _player.FinishGame(true);
            finishGame = true;
        }

        [Serializable]
        public class StackManager {

            Player _player;
            public int Count => bulletList.Count;
            public List<Ball> bulletList;   
            public int bulletAmountAtStart;

            public Ball bulletPrefab;

            public Transform targetTransform;
            public void Init(Player p) {
                _player = p;
                bulletList = new List<Ball>();

                for (int i = 0; i < bulletAmountAtStart; i++) {
                    Ball newBullet = Instantiate(bulletPrefab, targetTransform.position, Quaternion.identity);
                    bulletList.Add(newBullet);
                    newBullet.gameObject.SetActive(false);
                }
                _player.CurrentMode = Mode.Stacking;
            }
            public Ball GetBall() {
                for (int i = 0; i < bulletList.Count; i++) {
                    if (!bulletList[i].gameObject.activeSelf) {
                        return bulletList[i];
                    }
                }
                Ball newBullet = Instantiate(bulletPrefab);
                bulletList.Add(newBullet);
                newBullet.gameObject.SetActive(false);

                return newBullet;
            }
            public void Add(Ball ball) {
                if (!bulletList.Contains(ball)) {
                    ball.gameObject.SetActive(false);
                    _player.PlayPlusOneEffect(ball.transform.position);
                    bulletList.Add(ball);
                    _player.bulletTextModule.bulletText.IncreaseUpdateText();
                    _player.fireModule.levelBullet++;
                }
            }
            public void Remove(Ball ball) {
                bulletList.Remove(ball);
            }
        }

        [Serializable]
        public class ThrowingModule {
            Player _player;
            public Transform targetTransform;

            public float upForce;
            public void Init(Player player) {
                _player = player;
            }
            public void ThrowBalls() {
                for (int i = 0; i < _player.fireModule.howManyBullet; i++) {
                    Ball newBullet = _player.stackManager.GetBall();
                    newBullet.transform.position = targetTransform.position;
                    newBullet.gameObject.SetActive(true);
                    _player.animModule.Play(_player.animModule.IdleAnim);
                }

                for (int i = 0; i < _player.stackManager.bulletList.Count; i++) {
                    var rb = _player.stackManager.bulletList[i].GetComponent<Rigidbody>();
                    rb.velocity = Vector3.zero;
                    rb.AddForce(Vector3.up * upForce * Time.fixedDeltaTime * (i + 2));
                }
            }
        }
        [Serializable]
        public class FireModule {
            Player _player;
            RunnerController runnerController;

            public bool gameStart = false;

            public int levelBullet;

            public float howManyBullet;
            public float maxBullet;
            public float bulletBoost;
            public int firedBullet = 0;

            public Transform topHit;
            public Transform midHit;
            public Transform botHit;

            public CalculateBallPos calculateBallPos;

            public float fireSpeed;

            public float yAxis;
            public float levelYAxis;

            private Ball _currentBall;
            public void Init(Player player, RunnerController rc) {
                runnerController = rc;
                _player = player;
                calculateBallPos.Init(this);
            }
            public void Update() {
                if (_player.healthModule.IsAlive && levelBullet > 0 && !_player.finishGame) {
                    Hold();
                    Release();
                }
            }
            void Hold() {
                if (_player.CurrentMode == Mode.Stacking) {
                    if (Input.GetMouseButton(0)) {
                        _player.animModule.Play(_player.animModule.ThrowAnim);
                        _player.runnerController.moveData.speed = _player.movementModule.playerThrowSpeed;
                        howManyBullet += Time.deltaTime * ((bulletBoost * bulletBoost) / 2);
                        if (howManyBullet >= maxBullet) {
                            howManyBullet = 0;
                        }
                        if (howManyBullet > levelBullet) {
                            howManyBullet = levelBullet;
                        }
                    }
                }
            }
            void Release() {
                if (Input.GetMouseButtonUp(0)) {
                    _player.runnerController.moveData.speed = _player.movementModule.playerSpeed;
                    switch (_player.CurrentMode) {
                        case Mode.Stacking:
                            _player.fireModule.firedBullet = 0;
                            _player.throwingModule.ThrowBalls();
                            _player.CurrentMode = Mode.Fire;
                            runnerController.AddModifier(new CharacterStopper(_player));
                            break;
                        case Mode.Fire:
                            Fire();
                            for (int i = 0; i < _player.stackManager.bulletList.Count; i++) {
                                if (_player.stackManager.bulletList[i].gameObject.activeSelf) {
                                    _player.indicator.Hide(1);
                                    return;
                                }
                                _player.indicator.Hide(1);
                                _player.CurrentMode = Mode.Stacking;

                            }
                            break;
                        default:
                            break;
                    }
                }

                calculateBallPos.CalculateBallPositionAndChangeColor();
            }
            public void Fire() {
                if (_currentBall) {
                    levelBullet--;
                    firedBullet++;
                    calculateBallPos.CalculateBallPositionAndChangeYAxis();
                    _player.bulletTextModule.bulletText.ReduceUpdateText();
                    _currentBall.rb.velocity = Vector3.zero;

                    var pos = _player.transform.forward + new Vector3(0, yAxis, 0);
                    _currentBall.rb.AddForce(pos * fireSpeed);

                    _currentBall.rb.useGravity = false;

                    _player.stackManager.Remove(_currentBall);
                    SetCurrentBall(null);

                    _player.indicator.Hide(1);

                    _player.BaseLevelPlayer.RoutineManager.DoAction((_, __) => {
                        if (_player.stackManager.Count > 0) {
                            _player.animModule.Play(_player.animModule.IdleAnim);
                        } else {
                            _player.CurrentMode = Mode.Stacking;
                            _player.animModule.Play(_player.animModule.runAnim);

                        }
                    }, 0.3f);

                    if (firedBullet == Math.Ceiling(howManyBullet)) {

                        _player.BaseLevelPlayer.RoutineManager.DoAction((_, __) => {

                            _player.animModule.Play(_player.animModule.runAnim);
                            _player.CurrentMode = Mode.Stacking;
                        }, 0.3f);
                    }

                } else {
                    Debug.Log("Missed");
                }
            }
            public void SetCurrentBall(Ball ball) {
                _currentBall = ball;
            }

            [Serializable]
            public struct CalculateBallPos {

                FireModule fireModule;

                public Transform shootPos;
                public float shootMidThreshold;
                public float shootTopBotThreshold;
                public void Init(FireModule fm) {
                    fireModule = fm;
                }
                public void CalculateBallPositionAndChangeYAxis() {
                    if (fireModule._currentBall) {
                        float ballH = fireModule._currentBall.transform.position.y;
                        float shootY = shootPos.transform.position.y;

                        if (ballH > shootY + shootMidThreshold && ballH < shootY + shootTopBotThreshold) {
                            fireModule.yAxis = fireModule.levelYAxis;
                            fireModule._player.animModule.Play(fireModule._player.animModule.TopShoot);
                            fireModule._player.PlayHitEffect(fireModule.topHit.transform.position);

                        } else if (ballH > shootY - shootMidThreshold && ballH < shootY + shootMidThreshold) {
                            fireModule.yAxis = 0;
                            fireModule._player.animModule.Play(fireModule._player.animModule.MidShoot);
                            fireModule._player.PlayHitEffect(fireModule.midHit.transform.position);

                        } else if (ballH > shootY - shootTopBotThreshold && ballH < shootY - shootMidThreshold) {
                            fireModule._player.animModule.Play(fireModule._player.animModule.BotShoot);
                            fireModule._player.stackManager.Remove(fireModule._currentBall);
                            fireModule._currentBall.gameObject.SetActive(false);
                            fireModule._player.PlayHitEffect(fireModule.botHit.transform.position);
                        }
                    }
                }
                public void CalculateBallPositionAndChangeColor() {
                    if (fireModule._currentBall) {
                        float ballH = fireModule._currentBall.transform.position.y;
                        float shootY = shootPos.transform.position.y;

                        if (ballH > shootY + shootMidThreshold && ballH < shootY + shootTopBotThreshold) {
                            fireModule._player.indicator.SetActive(0);

                        } else if (ballH > shootY - shootMidThreshold && ballH < shootY + shootMidThreshold) {
                            fireModule._player.indicator.SetActive(1);

                        } else if (ballH > shootY - shootTopBotThreshold && ballH < shootY - shootMidThreshold) {
                            fireModule._player.indicator.SetActive(2);
                        }
                    }
                }
            }
        }

        [Serializable]
        public class MovementModule {
            public float playerSpeed;
            public float playerThrowSpeed;
        }

        [Serializable]
        public class Indicator {
            public Image[] images;
            private BaseLevelPlayer _player;

            public void Init(BaseLevelPlayer baseLevelPlayer) {
                _player = baseLevelPlayer;
            }
            public void SetActive(int index) {
                for (int i = 0; i < images.Length; i++) {
                    images[i].color = i == index ? Color.green : Color.white;
                }
            }
            public void Hide(float sec = 0) {
                _player.RoutineManager.DoAction((complete, data) => {
                    for (int i = 0; i < images.Length; i++) {
                        images[i].color = Color.white;
                    }

                }, sec);
            }

        }

        public class HealthModule {
            public Health health;
            public float deathAnimDelay;
            public bool IsAlive => health.isAlive;
            Player _player;
            public bool isDead = false;
            public void Init(Player p) {
                _player = p;
                health.Init();
            }
            public void Update() {
                if (!health.isAlive && !_player.finishGame) {
                    _player.Death();
                    _player.BaseLevelPlayer.FinishGame(false);
                }
            }
            internal void TakeDamage(float damage) {
                _player.CurrentMode = Mode.Stacking;
                _player.fireModule.howManyBullet = 0;

                health.TakeDamage(damage);

                _player.runnerController.moveData.speed = 0;
                _player.animModule.Play(_player.animModule.IdleAnim);

                _player.BaseLevelPlayer.RoutineManager.DoAction((complete, data) => {
                    _player.animModule.Play(_player.animModule.TakeDamage);
                    _player.takeDamageEffect.Play();

                }, 0.5f);
                if (health.isAlive) {
                    _player.BaseLevelPlayer.RoutineManager.DoAction((complete, data) => {

                        _player.animModule.Play(_player.animModule.runAnim);
                        _player.runnerController.moveData.speed = _player.movementModule.playerSpeed;

                    }, 1f);
                }
            }


        }

        [Serializable]
        public class BulletTextModule {

            public BulletText bulletText;

        }
        public struct CharacterStopper : IModifier {
            Player _player;
            public CharacterStopper(Player p) {
                _player = p;
            }
            public bool Modify(float modifierTime, ref MoveData data, ref Pose pose, RunnerController controller) {
                if (_player.CurrentMode == Mode.Fire) {
                    data.speed = 0;
                    return true;
                } else {
                    data.speed = _player.movementModule.playerSpeed;
                    _player.fireModule.howManyBullet = 0;
                    _player.fireModule.firedBullet = 0;
                    return false;
                }
            }
        }
    }
}
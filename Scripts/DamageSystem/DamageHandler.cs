using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace AutoMcD.PocketGear.DamageSystem {
    public class DamageHandler {
        private const int ATTACK_TIMEOUT_IN_MS = 100;
        private const double MAX_IMPACT_TOLERANCE = 24.5;
        private const double MIN_IMPACT_TOLERANCE = 5;

        private readonly Dictionary<long, ProtectInfo> _protecedInfos = new Dictionary<long, ProtectInfo>();
        private ILogger Log { get; set; }

        public void DisableProtection(IMyCubeBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(DisableProtection)) : null) {
                if (block == null) {
                    return;
                }

                var cubegrid = block.CubeGrid;
                if (_protecedInfos.ContainsKey(cubegrid.EntityId)) {
                    _protecedInfos[cubegrid.EntityId].DisableProtection(block);
                }
            }
        }

        public void DisableProtection(long cubeGridId) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(DisableProtection)) : null) {
                if (_protecedInfos.ContainsKey(cubeGridId)) {
                    _protecedInfos[cubeGridId].Close();
                    _protecedInfos.Remove(cubeGridId);
                }
            }
        }

        public void EnableProtection(IMyCubeBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(EnableProtection)) : null) {
                if (block == null) {
                    return;
                }

                var cubegrid = block.CubeGrid;
                if (_protecedInfos.ContainsKey(cubegrid.EntityId)) {
                    _protecedInfos[cubegrid.EntityId].EnableProtection(block);
                } else {
                    var protecedInfo = new ProtectInfo(cubegrid);
                    protecedInfo.EnableProtection(block);
                    _protecedInfos.Add(cubegrid.EntityId, protecedInfo);
                    cubegrid.OnClose += OnClose;
                }
            }
        }

        public void Init() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<DamageHandler>();
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(100, HandleDamage);
            }
        }

        private void HandleDamage(object target, ref MyDamageInformation damage) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(HandleDamage)) : null) {
                if (damage.Type != MyDamageType.Deformation) {
                    return;
                }

                var slimBlock = (IMySlimBlock) target;
                if (slimBlock != null) {
                    var cubeGrid = slimBlock.CubeGrid;
                    if (!_protecedInfos.ContainsKey(cubeGrid.EntityId)) {
                        return;
                    }

                    var protectInfo = _protecedInfos[cubeGrid.EntityId];
                    if (protectInfo.Contains(slimBlock)) {
                        var attackerEntity = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
                        if (attackerEntity == null) {
                            if (protectInfo.AttackStart - DateTime.UtcNow > TimeSpan.FromMilliseconds(ATTACK_TIMEOUT_IN_MS)) {
                                return;
                            }
                        } else {
                            protectInfo.RegisterNewAttack(attackerEntity);
                        }

                        var attacker = protectInfo.Attacker;

                        HandleProtectedBlockDamage(protectInfo, attacker, ref damage);
                    }
                }
            }
        }

        private void HandleProtectedBlockDamage(ProtectInfo protectInfo, AttackerInfo attacker, ref MyDamageInformation damage) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(HandleProtectedBlockDamage)) : null) {
                var toleranceMultiplicator = Mod.Static.Settings.ImpactToleranceMultiplier;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (toleranceMultiplicator == 0) {
                    damage.Amount = 0;
                    damage.IsDeformation = false;
                    return;
                }

                var linearVelocity = protectInfo.LinearVelocity;
                var mass = protectInfo.Mass;
                var impactTolerance = Math.Pow(mass / 1000, -0.1) * MAX_IMPACT_TOLERANCE;

                var attackerLinearVelocity = attacker.LinearVelocity;

                var impactVelocity = (attackerLinearVelocity - linearVelocity).Length();
                var tolerance = Math.Min(Math.Max(impactTolerance, MIN_IMPACT_TOLERANCE), MAX_IMPACT_TOLERANCE) * toleranceMultiplicator;

                if (impactVelocity <= tolerance * 2.5) {
                    if (impactVelocity <= tolerance) {
                        damage.Amount = 0;
                        damage.IsDeformation = false;
                    } else {
                        var multiplicator = Math.Pow(impactVelocity / tolerance, .75) - 1;
                        damage.Amount *= (float) multiplicator;
                        damage.IsDeformation = false;
                    }
                }
            }
        }

        private void OnClose(IMyEntity cubeGrid) {
            if (_protecedInfos.ContainsKey(cubeGrid.EntityId)) {
                _protecedInfos.Remove(cubeGrid.EntityId);
            }
        }
    }
}
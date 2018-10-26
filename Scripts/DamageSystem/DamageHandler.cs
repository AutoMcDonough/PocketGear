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

        private readonly Dictionary<long, ProtectInfo> _protectedInfos = new Dictionary<long, ProtectInfo>();

        public DamageHandler() {
            Log = Mod.Static.Log.ForScope<DamageHandler>();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(100, HandleDamage);
        }

        private ILogger Log { get; }

        public void DisableProtection(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(DisableProtection)) : null) {
                using (Log.BeginMethod(nameof(DisableProtection))) {
                    if (slimBlock == null) {
                        return;
                    }

                    Log.Debug($"Disable protection for: {slimBlock}");
                    var cubeGrid = slimBlock.CubeGrid;
                    if (_protectedInfos.ContainsKey(cubeGrid.EntityId)) {
                        _protectedInfos[cubeGrid.EntityId].DisableProtection(slimBlock);
                    }
                }
            }
        }

        public void DisableProtection(long cubeGridId) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(DisableProtection)) : null) {
                using (Log.BeginMethod(nameof(DisableProtection))) {
                    Log.Debug($"Disable protection for cube grid: {cubeGridId}");
                    if (_protectedInfos.ContainsKey(cubeGridId)) {
                        _protectedInfos[cubeGridId].Close();
                        _protectedInfos.Remove(cubeGridId);
                    }
                }
            }
        }

        public void EnableProtection(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(DamageHandler), nameof(EnableProtection)) : null) {
                using (Log.BeginMethod(nameof(EnableProtection))) {
                    if (slimBlock == null) {
                        return;
                    }

                    Log.Debug($"Enable protection for: {slimBlock}");
                    var cubeGrid = slimBlock.CubeGrid;
                    if (_protectedInfos.ContainsKey(cubeGrid.EntityId)) {
                        _protectedInfos[cubeGrid.EntityId].EnableProtection(slimBlock);
                    } else {
                        var protectInfo = new ProtectInfo(cubeGrid);
                        protectInfo.EnableProtection(slimBlock);
                        _protectedInfos.Add(cubeGrid.EntityId, protectInfo);
                        cubeGrid.OnClose += OnClose;
                    }
                }
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
                    if (!_protectedInfos.ContainsKey(cubeGrid.EntityId)) {
                        return;
                    }

                    var protectInfo = _protectedInfos[cubeGrid.EntityId];
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
                        var multiplier = Math.Pow(impactVelocity / tolerance, .75) - 1;
                        damage.Amount *= (float) multiplier;
                        damage.IsDeformation = false;
                    }
                }
            }
        }

        private void OnClose(IMyEntity cubeGrid) {
            if (_protectedInfos.ContainsKey(cubeGrid.EntityId)) {
                _protectedInfos.Remove(cubeGrid.EntityId);
            }
        }
    }
}
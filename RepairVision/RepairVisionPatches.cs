using System;
using System.Collections.Generic;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using Valgraves.Common;
using Object = UnityEngine.Object;

namespace RepairVision
{
    [HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.Update))]
    public class RepairVisionUpdate
    {
        private static bool _initialized = false;
        private static bool _repairVisionEnabled = false;
        
        private static Dictionary<Vector3i, GameObject> _blocks = new Dictionary<Vector3i, GameObject>();
        private static Color _startColor = new Color(1f, 0.92156863f, 0.015686275f);
        private static Color _endColor = new Color(0.8f, 0f, 0f);
        private static float _damageThreshold = 0.9f;
        private static int _scanRange = 25;

        private static void CleanUpObjects()
        {
            var oldBlocks = _blocks;
            _blocks = new Dictionary<Vector3i, GameObject>();
            var blockObjects = oldBlocks.Values.ToList();
            foreach (var blockObject in blockObjects)
            {
                Object.Destroy(blockObject);
            }
            BlockHelpers.CleanUp();
        }

        private static void Initialize()
        {
            _initialized = true;
            _damageThreshold = RepairVision.Config.DamageThreshold;
            _scanRange = RepairVision.Config.ScanRange;
            if (!ColorUtility.TryParseHtmlString(RepairVision.Config.StartColor, out _startColor))
            {
                Logging.Warning($"Failed to convert StartColor {RepairVision.Config.StartColor} to Color, is it a valid HTML color code?");
            }
            if (!ColorUtility.TryParseHtmlString(RepairVision.Config.EndColor, out _endColor))
            {
                Logging.Warning($"Failed to convert EndColor {RepairVision.Config.EndColor} to Color, is it a valid HTML color code?");
            }
            Logging.Warning($"RepairVision Initialized:");
            Logging.Warning($"  Damage Threshold: {_damageThreshold}");
            Logging.Warning($"  Scan Range: {_scanRange}");
            Logging.Warning($"  Start Color: {_startColor}");
            Logging.Warning($"  End Color: {_endColor}");
        }

        private static bool SkipProcessing()
        {
            if (RepairVision.RepairVisionActions.ToggleRepairVision.WasPressed)
            {
                _repairVisionEnabled = !_repairVisionEnabled;
                if (!_repairVisionEnabled)
                {
                    CleanUpObjects();
                }
                else
                {
                    Player.Entity.Buffs.AddBuff("repairVision");
                }
            }

            if (!_repairVisionEnabled && Player.Entity.Buffs.HasBuff("repairVision"))
            {
                Player.Entity.Buffs.RemoveBuff("repairVision");
            }
            
            return !_repairVisionEnabled;
        }
        
        public static void Postfix(EntityPlayerLocal __instance)
        {
            try
            {
                if (!_initialized)
                {
                    Initialize();
                }
                
                if (RepairVision.RepairVisionActions.ToggleRepairVision.WasPressed)
                {
                    _repairVisionEnabled = !_repairVisionEnabled;
                    if (!_repairVisionEnabled)
                    {
                        CleanUpObjects();
                    }
                    else
                    {
                        Player.Entity.Buffs.AddBuff("repairVision");
                    }
                }

                if (!_repairVisionEnabled)
                {
                    if (Player.Entity.Buffs.HasBuff("repairVision"))
                    {
                        Player.Entity.Buffs.RemoveBuff("repairVision");
                    }
                    return;
                }

                if (SkipProcessing())
                {
                    return;
                }

                List<Vector3i> nearBlockPositions = new List<Vector3i>();
                var center = new Vector3i();
                center.FloorToInt(Player.Entity.position);
                var centerWorld = new Vector3i();
                centerWorld.FloorToInt(Player.Entity.transform.position);
                for (int i = -_scanRange; i <= _scanRange; i++)
                {
                    for (int j = -_scanRange; j <= _scanRange; j++)
                    {
                        for (int k = -_scanRange; k <= _scanRange; k++)
                        {
                            var scanOffset = new Vector3i(i, j, k);
                            var position = center + scanOffset;
                            var blockValue = GameManager.Instance.World.GetBlock(position);

                            // Skip for terrain and unrepairable blocks.
                            if (blockValue.isair || blockValue.isTerrain || blockValue.isWater || !blockValue.Block.CanRepair(blockValue))
                            {
                                continue;
                            }
                            
                            nearBlockPositions.Add(position);
                            var hpPercent = (1.0f * (blockValue.Block.MaxDamage - blockValue.damage)) / blockValue.Block.MaxDamage;
                            // If the block isn't in bad shape, skip it and move on, removing tracked block if needed.
                            if (hpPercent > _damageThreshold)
                            {
                                if (_blocks.TryGetValue(position, out GameObject existingBlock))
                                {
                                    Object.Destroy(existingBlock);
                                    _blocks.Remove(position);
                                }
                                continue;
                            }
                            
                            if (!_blocks.TryGetValue(position, out GameObject damageBlock))
                            {
                                var blockPosition = (centerWorld + scanOffset).ToVector3();
                                var blockRotation = blockValue.Block.shape.GetRotation(blockValue);
                                if (blockValue.Block.shape is BlockShapeNew blockShape)
                                {
                                    damageBlock = BlockHelpers.GenerateShapeObject(ref blockValue, ref blockShape);
                                    var pivot = damageBlock.GetComponentsInChildren<Transform>().First(x => x.name == "pivot");
                                    pivot.transform.rotation = blockRotation;
                                }
                                else if (blockValue.Block.HasTileEntity)
                                {
                                    var chunk = GameManager.Instance.World.GetChunkFromWorldPos(position);
                                    var blockEntity = chunk.GetBlockEntity(position);
                                    if (blockEntity != null && blockEntity.bHasTransform)
                                    {
                                        damageBlock = BlockHelpers.GenerateEntityObject(ref blockEntity);
                                        if (damageBlock != null)
                                        {
                                            blockPosition = blockEntity.transform.position;
                                            damageBlock.transform.rotation = blockEntity.transform.rotation;
                                        }
                                    }                                    
                                }

                                if (damageBlock == null)
                                {
                                    damageBlock = BlockHelpers.GenerateBlockObject();
                                    damageBlock.transform.rotation = blockRotation;
                                    blockPosition += Vector3.one * 0.5f;
                                }
                                
                                damageBlock.transform.position = blockPosition;
                                _blocks.Add(position, damageBlock);
                            }

                            if (damageBlock == null)
                            {
                                _blocks.Remove(position);
                                continue;
                            }
                            
                            // Delete old blocks if the block is now repaired enough.
                            if (hpPercent > _damageThreshold)
                            {
                                Object.Destroy(_blocks[position]);
                                _blocks.Remove(position);
                                continue;
                            }

                            if (damageBlock.transform == null)
                            {
                                continue;
                            }

                            try
                            {
                                var distanceMod = Math.Max(0f, 1.0f - (scanOffset.Magnitude() / (_scanRange * 0.6f)));
                                var blockColor = Color.Lerp(_endColor, _startColor, hpPercent);
                                blockColor.a = 0.05f * (float)distanceMod;
                                foreach (var renderer in damageBlock.GetComponentsInChildren<MeshRenderer>())
                                {
                                    foreach (var material in renderer.materials)
                                    {
                                        material.SetColor("_Color", blockColor);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logging.Error(e.ToString());
                            }
                        }
                    }
                }
                
                // Remove far blocks.
                var farBlockPositions = _blocks.Keys.Except(nearBlockPositions).ToList();
                foreach (var position in farBlockPositions)
                {
                    Object.Destroy(_blocks[position]);
                    _blocks.Remove(position);
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
    }
}
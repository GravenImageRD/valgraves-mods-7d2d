using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using UniLinq;
using UnityEngine;
using Valgraves.Common;
using Valgraves.Common.Extensions;
using Object = UnityEngine.Object;

namespace RepairVision
{
    [HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.Update))]
    public class RepairVisionUpdate
    {
        private static bool _repairVisionEnabled = false;
        private static Dictionary<Vector3i, GameObject> _blocks = new Dictionary<Vector3i, GameObject>();
        
        private static void CleanUpObjects()
        {
            var oldBlocks = _blocks.Values.ToList();
            _blocks = new Dictionary<Vector3i, GameObject>();
            foreach (var blockObject in oldBlocks)
            {
                Object.Destroy(blockObject);
            }
            BlockHelpers.CleanUp();
        }

        private static Stopwatch _updateTimer = new Stopwatch();
        
        private static bool SkipProcessing(EntityPlayerLocal player)
        {
            if (player == null)
            {
                return true;
            }
            
            if (RepairVision.RepairVisionActions.ToggleRepairVision.WasPressed)
            {
                _repairVisionEnabled = !_repairVisionEnabled;
                if (!_repairVisionEnabled)
                {
                    CleanUpObjects();
                    if (_scanRunning)
                    {
                        player.StopCoroutine(_scanCoroutine);
                        foreach (var block in _blocks.Values)
                        {
                            block.SetActive(false);
                        }

                        _scanRunning = false;
                    }
                }
                else
                {
                    player.Buffs.AddBuff("repairVision");
                    foreach (var block in _blocks.Values)
                    {
                        block.SetActive(true);
                    }
                }
            }

            if (!_repairVisionEnabled && player.Buffs.HasBuff("repairVision"))
            {
                player.Buffs.RemoveBuff("repairVision");
            }

            
            return !_repairVisionEnabled;
        }

        private static bool _scanRunning = false;
        private static Coroutine _scanCoroutine = null;
        
        private static IEnumerator ScanCoroutine(EntityPlayerLocal player)
        {
            _scanRunning = true;
            float passTime = 0.5f / 1000f;
            var scanRange = RepairVision.Config.ScanRange;
            var center = player.position.FloorToInt();
            var centerWorld = player.transform.position.FloorToInt();
            List<Vector3i> nearBlockPositions = new List<Vector3i>();
            for (int i = -scanRange; i <= scanRange; i++)
            {
                for (int j = -scanRange; j <= scanRange; j++)
                {
                    for (int k = -scanRange; k <= scanRange; k++)
                    {
                        var scanOffset = new Vector3i(i, j, k);
                        nearBlockPositions.Add(center + scanOffset);
                    }
                }
            }

            for (int i = 0; i < nearBlockPositions.Count; i++)
            {
                double frameStartTime = Time.realtimeSinceStartupAsDouble;
                while (i < nearBlockPositions.Count && passTime > (Time.realtimeSinceStartupAsDouble - frameStartTime))
                {
                    var position = nearBlockPositions[i++];
                    var blockValue = GameManager.Instance.World.GetBlock(position);
                            
                    // Skip for terrain and unrepairable blocks.
                    if (blockValue.isair || blockValue.isTerrain || blockValue.isWater || !blockValue.Block.CanRepair(blockValue) || blockValue.ischild)
                    {
                        continue;
                    }
                    
                    var hpPercent = (1.0f * (blockValue.Block.MaxDamage - blockValue.damage)) / blockValue.Block.MaxDamage;
                            
                    // If the block isn't in bad shape, skip it and move on, removing tracked block if needed.
                    try
                    {
                        if (hpPercent > RepairVision.Config.DamageThreshold)
                        {
                            if (_blocks.TryGetValue(position, out GameObject existingBlock))
                            {
                                Object.Destroy(existingBlock);
                                _blocks.Remove(position);
                            }
                            continue;
                        }
                                
                        // If we don't already have this block generated, generate it now.
                        if (!_blocks.TryGetValue(position, out GameObject damageBlock))
                        {
                            var blockPosition = position.ToVector3() - Origin.position;
                            var blockRotation = blockValue.Block.shape.GetRotation(blockValue);
                                    
                            // Handle BlockShapes.
                            if (blockValue.Block.shape is BlockShapeNew blockShape)
                            {
                                damageBlock = BlockHelpers.GenerateShapeObject(ref blockValue, ref blockShape);
                                var pivot = damageBlock.GetComponentsInChildren<Transform>().First(x => x.name == "pivot");
                                pivot.transform.rotation = blockRotation;
                            }
                            // Handle block entities.
                            else if (blockValue.Block.HasTileEntity)
                            {
                                var chunk = GameManager.Instance.World.GetChunkFromWorldPos(position);
                                var blockEntity = chunk.GetBlockEntity(position);
                                if (blockEntity != null && blockEntity.bHasTransform)
                                {
                                    damageBlock = BlockHelpers.GenerateEntityObject(ref blockEntity);
                                    if (damageBlock)
                                    {
                                        blockPosition = blockEntity.transform.position;
                                        damageBlock.transform.rotation = blockEntity.transform.rotation;
                                    }
                                }                                    
                            }
                            // Handle multiblocks.
                            else if (blockValue.Block.isMultiBlock)
                            {
                                var modelEntity = blockValue.Block.shape as BlockShapeModelEntity;
                                Logging.Error($"Block {blockValue.Block.blockName} bsme {modelEntity.modelName} offset ({modelEntity.modelOffset})");
                                if (nearBlockPositions.Contains(blockValue.parent))
                                {
                                    continue;
                                }

                                if (!_blocks.TryGetValue(blockValue.parent, out damageBlock))
                                {
                                    var modelProperty = blockValue.Block.dynamicProperties.GetString("Model");
                                    if (!string.IsNullOrEmpty(modelProperty))
                                    {
                                        var dimensions = blockValue.Block.multiBlockPos.dim;
                                        var xOff = (float)Math.Floor(dimensions.x / 2.0f);
                                        var zOff = (float)Math.Floor(dimensions.z / 2.0f);
                                        var offset = modelEntity.GetRotatedOffset(blockValue.Block, modelEntity.GetRotation(blockValue));
                                        damageBlock = BlockHelpers.GeneratePrefabObject(modelEntity.modelNameWithPath, dimensions, offset);
                                        blockPosition += offset;
                                        blockPosition += new Vector3(0.5f, 0.0f, 0.5f);
                                        damageBlock.transform.rotation = blockRotation;
                                    }
                                }
                            }

                            // If we don't have a generated block at this point, fall back to a basic cube.
                            if (!damageBlock)
                            {
                                damageBlock = BlockHelpers.GenerateBlockObject();
                                damageBlock.transform.rotation = blockRotation;
                                blockPosition += Vector3.one * 0.5f;
                            }
                                    
                            damageBlock.transform.position = blockPosition;
                            _blocks.Add(position, damageBlock);
                        }

                        // If we got here and have a null block, that's a bad sign. Remove the position so it can
                        // be re-generated next frame.
                        if (!damageBlock || !damageBlock.transform)
                        {
                            _blocks.Remove(position);
                            continue;
                        }
                                
                        // Delete old blocks if the block is now repaired enough.
                        if (hpPercent > RepairVision.Config.DamageThreshold)
                        {
                            Object.Destroy(_blocks[position]);
                            _blocks.Remove(position);
                            continue;
                        }
                    
                        // Interpolate the end and start color by HP percent to get the current color.
                        var blockColor = Color.Lerp(RepairVision.Config.GetEndColor(), RepairVision.Config.GetStartColor(), hpPercent);
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

                if (i < nearBlockPositions.Count)
                {
                    Logging.Error($"Pass complete, {nearBlockPositions.Count - i} blocks to go");
                    yield return null;
                }
            }
            
            // Remove far blocks.
            Logging.Error("Scanning complete, cleaning up far blocks");
            var farBlockPositions = _blocks.Keys.Except(nearBlockPositions).ToList();
            foreach (var position in farBlockPositions)
            {
                Object.Destroy(_blocks[position]);
                _blocks.Remove(position);
            }
            
            _scanRunning = false;
        }
        
        public static void Postfix(EntityPlayerLocal __instance)
        {
            try
            {
                if (SkipProcessing(__instance))
                {
                    return;
                }

                if (!_scanRunning)
                {
                    _scanCoroutine = __instance.StartCoroutine(ScanCoroutine(__instance));
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
    }
}
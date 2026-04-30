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
        private static Dictionary<Vector3i, GameObject> _blocks = new Dictionary<Vector3i, GameObject>();
        private static DateTime _nextUpdate = DateTime.Now.AddSeconds(2);
        private static readonly Color _startColor = new Color(1f, 0.92156863f, 0.015686275f);
        private static readonly Color _endColor = new Color(0.8f, 0f, 0f);
        private static Dictionary<string, Mesh> _shapeMeshes =  new Dictionary<string, Mesh>();
        
        public static void Postfix(EntityPlayerLocal __instance)
        {
            try
            {
                if (_nextUpdate > DateTime.Now)
                {
                    return;
                }
                _nextUpdate = DateTime.Now.AddMilliseconds(100);

                var heldItem = __instance.inventory.holdingItem;
                if (heldItem == null)
                {
                    return;
                }
                
                var holdingRepairTool = heldItem.ItemTags.ToString().Contains("repairTool");
                if (!holdingRepairTool)
                {
                    // Clean up any blocks in case we've just put the tool away.
                    var blockObjects = _blocks.Values.ToList();
                    foreach (var blockObject in blockObjects)
                    {
                        Object.Destroy(blockObject);
                    }
                    _blocks.Clear();
                    return;
                }
                
                List<Vector3i> nearBlockPositions = new List<Vector3i>();
                var center = new Vector3i(Player.Entity.position);
                var centerWorld = new Vector3i();
                centerWorld.FloorToInt(Player.Entity.transform.position);
                int scanRange = 25;
                for (int i = -scanRange; i <= scanRange; i++)
                {
                    for (int j = -scanRange; j <= scanRange; j++)
                    {
                        for (int k = -scanRange; k <= scanRange; k++)
                        {
                            var scanOffset = new Vector3i(i, j, k);
                            var position = center + scanOffset;
                            var blockValue = GameManager.Instance.World.GetBlock(position);
                            
                            if (blockValue.isair || blockValue.isTerrain || blockValue.isWater || !blockValue.Block.CanRepair(blockValue))
                            {
                                continue;
                            }
                            
                            nearBlockPositions.Add(position);
                            var hpPercent = (1.0f * (blockValue.Block.MaxDamage - blockValue.damage)) / blockValue.Block.MaxDamage;
                            // If the block isn't in bad shape, skip it and move on, removing tracked block if needed.
                            if (hpPercent > .9f)
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
                                    if (blockEntity == null || !blockEntity.bHasTransform)
                                    {
                                        Logging.Error($"Block {blockValue.Block.GetBlockName()} has no BlockEntity transform, using cube.");
                                        damageBlock = BlockHelpers.GenerateBlockObject();
                                        damageBlock.transform.rotation = blockRotation;
                                        //blockPosition += Vector3.one * 0.5f;
                                    }
                                    else
                                    {
                                        damageBlock = BlockHelpers.GenerateEntityObject(ref blockEntity);
                                        blockPosition = blockEntity.transform.position;
                                        damageBlock.transform.rotation = blockEntity.transform.rotation;
                                    }                                    
                                }
                                else
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
                            if (hpPercent > 0.9f)
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
                                var distanceVec = Player.Entity.transform.position - damageBlock.transform.position;
                                var distanceMod = Math.Max(0f, 1.0f - (distanceVec.magnitude / 18));
                                var blockColor = Color.Lerp(_startColor, _endColor, (0.9f - hpPercent));
                                blockColor.a = 0.1f * distanceMod;
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
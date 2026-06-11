using System;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;
using Valgraves.Common;
using Valgraves.Common.Extensions;
using Object = UnityEngine.Object;

namespace RepairVision
{
    public class RepairVisionManager
    {
        private bool _repairVisionEnabled = false;
        private Dictionary<Vector3i, GameObject> _blocks = new Dictionary<Vector3i, GameObject>();
        private bool _scanRunning = false;
        private Coroutine _scanCoroutine = null;
        private List<Vector3i> _scanOffsets = new List<Vector3i>();
        private float _timePerFrame = 1f / 10000f;
        private GameObject _scannerPrefab;
        private RepairVisionActions _repairVisionActions;
        
        public RepairVisionManager(RepairVisionConfig config, AssetBundle assetBundle)
        {
            _timePerFrame = RepairVision.Config.MsPerFrame / 1000f;
            for (int i = -config.ScanRange; i <= config.ScanRange; i++)
            {
                for (int j = -config.ScanRange; j <= config.ScanRange; j++)
                {
                    for (int k = -config.ScanRange; k <= config.ScanRange; k++)
                    {
                        var scanOffset = new Vector3i(i, j, k);
                        _scanOffsets.Add(scanOffset);
                    }
                }
            }
            
            // Sort offsets from inside out.
            _scanOffsets.Sort((x, y) => x.Magnitude().CompareTo(y.Magnitude()));
            
            // Prep scanner effect prefab.
            _scannerPrefab = assetBundle.LoadAsset<GameObject>("Scanner 1");
            if (_scannerPrefab == null)
            {
                Logging.Error("Couldn't find the scanner prefab in the RepairVision asset bundle!");
            }

            _repairVisionActions = new RepairVisionActions();
        }

        public void UpdateScan(EntityPlayerLocal player)
        {
            if (!_scanRunning)
            {
                _scanCoroutine = player.StartCoroutine(RepairVision.Manager.ScanCoroutine(player));
            }
        }
        
        public bool SkipProcessing(EntityPlayerLocal player)
        {
            if (player == null)
            {
                return true;
            }
            
            if (_repairVisionActions.ToggleRepairVision.WasPressed)
            {
                _repairVisionEnabled = !_repairVisionEnabled;
                if (!_repairVisionEnabled)
                {
                    CleanUpObjects(player);
                    if (_scanRunning)
                    {
                        player.StopCoroutine(_scanCoroutine);
                        _scanCoroutine = null;
                        _scanRunning = false;
                    }
                }
                else
                {
                    player.Buffs.AddBuff("repairVision");
                    var scanner = Object.Instantiate(_scannerPrefab, player.transform);
                    scanner.SetActive(true);
                }
            }

            if (!_repairVisionEnabled && player.Buffs.HasBuff("repairVision"))
            {
                player.Buffs.RemoveBuff("repairVision");
            }

            
            return !_repairVisionEnabled;
        }
        
        private void CleanUpObjects(EntityPlayerLocal player)
        {
            var oldBlocks = _blocks.Values.ToList();
            _blocks = new Dictionary<Vector3i, GameObject>();
            foreach (var blockObject in oldBlocks)
            {
                player.StartCoroutine(FadeOutBlockCoroutine(blockObject));
            }
            BlockHelpers.CleanUp();
        }
        
        private IEnumerator ScanCoroutine(EntityPlayerLocal player)
        {
            _scanRunning = true;
            var center = player.position.FloorToInt();
            List<Vector3i> nearBlockPositions = new List<Vector3i>();
            foreach (var offset in _scanOffsets)
            {
                nearBlockPositions.Add(center + offset);
            }

            for (int i = 0; i < nearBlockPositions.Count; i++)
            {
                double frameStartTime = Time.realtimeSinceStartupAsDouble;
                int startI = i;
                while (i < nearBlockPositions.Count && _timePerFrame > (Time.realtimeSinceStartupAsDouble - frameStartTime))
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
                                Origin.Remove(existingBlock.transform);
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
                            player.StartCoroutine(FadeInBlockCoroutine(damageBlock));
                            Origin.Add(damageBlock.transform, 0);
                            _blocks.Add(position, damageBlock);
                        }

                        // If we got here and have a null block, that's a bad sign. Remove the position so it can
                        // be re-generated next frame.
                        if (!damageBlock || !damageBlock.transform)
                        {
                            Logging.Error($"Position {position} had bad block, removing.");
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
                    var blocksProcessed = i - startI;
                    if (blocksProcessed < 100)
                    {
                        Logging.Error($"RepairVision scan only processed {i-startI} blocks with time allocation {_timePerFrame}ms! Consider adjusting!");
                    } else if (blocksProcessed < 500)
                    {
                        Logging.Warning($"RepairVision scan only processed {i-startI} blocks with time allocation {_timePerFrame}ms! Consider adjusting!");
                    }

                    yield return null;
                }
            }
            
            // Remove far blocks.
            var farBlockPositions = _blocks.Keys.Except(nearBlockPositions).ToList();
            foreach (var position in farBlockPositions)
            {
                Origin.Remove(_blocks[position].transform);
                Object.Destroy(_blocks[position]);
                _blocks.Remove(position);
            }
            
            _scanRunning = false;
        }

        private IEnumerator FadeInBlockCoroutine(GameObject block)
        {
            var startId = Shader.PropertyToID("_FadeStartDist");
            var endId = Shader.PropertyToID("_FadeEndDist");
            var renderers =  block.GetComponentsInChildren<MeshRenderer>();
            var materials = renderers.SelectMany(renderer => renderer.materials).ToList();
            for (float t = 0f; t < 1.0f; t += 0.1f)
            {
                var fadeStart = BlockHelpers.FadeStart * t;
                var fadeEnd = BlockHelpers.FadeEnd * t;
                foreach (var material in materials)
                {
                    material.SetFloat(startId, fadeStart);
                    material.SetFloat(endId, fadeEnd);
                }
                yield return new WaitForSeconds(0.01f);
            }
        }

        private IEnumerator FadeOutBlockCoroutine(GameObject block)
        {
            var startId = Shader.PropertyToID("_FadeStartDist");
            var endId = Shader.PropertyToID("_FadeEndDist");
            var renderers =  block.GetComponentsInChildren<MeshRenderer>();
            var materials = renderers.SelectMany(renderer => renderer.materials).ToList();
            for (float t = 1f; t > 0f; t -= 0.1f)
            {
                var fadeStart = BlockHelpers.FadeStart * t;
                var fadeEnd = BlockHelpers.FadeEnd * t;
                foreach (var material in materials)
                {
                    material.SetFloat(startId, fadeStart);
                    material.SetFloat(endId, fadeEnd);
                }
                yield return new WaitForSeconds(0.01f);
            }
            Origin.Remove(block.transform);
            Object.Destroy(block);
        }
    }
}
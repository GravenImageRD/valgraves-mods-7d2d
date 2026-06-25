using System;
using System.Collections;
using System.Collections.Generic;
using RepairVision.Objects;
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
        private Dictionary<Vector3i, RepairVisionBlock> _blocks = new Dictionary<Vector3i, RepairVisionBlock>();
        private bool _scanRunning = false;
        private Coroutine _scanCoroutine = null;
        private List<Vector3i> _scanOffsets = new List<Vector3i>();
        private float _timePerFrame = 1f / 10000f;
        private int _scanRange = 25;
        private float _maxDistance = 44f;
        private int _totalNearBlocks = 0;
        private GameObject _scannerPrefab;
        private RepairVisionActions _repairVisionActions;
        private Vector3i[] _nearBlockPositions = Array.Empty<Vector3i>();
        private Vector3i _lastCenter = Vector3i.max;
        
        public RepairVisionManager(AssetBundle assetBundle)
        {
            _timePerFrame = RepairVision.Config.MsPerFrame / 1000f;
            _scanRange = RepairVision.Config.ScanRange;
            _maxDistance = Mathf.Sqrt((_scanRange * _scanRange) * 3) + 0.5f; // Add a little fudge to make sure it is outside the scan range
            for (int i = -_scanRange; i <= _scanRange; i++)
            {
                for (int j = -_scanRange; j <= _scanRange; j++)
                {
                    for (int k = -_scanRange; k <= _scanRange; k++)
                    {
                        var scanOffset = new Vector3i(i, j, k);
                        _scanOffsets.Add(scanOffset);
                    }
                }
            }
            _totalNearBlocks = _scanOffsets.Count;
            _nearBlockPositions = new Vector3i[_totalNearBlocks];

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
            _blocks = new Dictionary<Vector3i, RepairVisionBlock>();
            foreach (var block in oldBlocks)
            {
                player.StartCoroutine(FadeOutBlockCoroutine(block.GameObject));
            }
            BlockHelpers.CleanUp();
        }

        private Vector3i[] GenerateNearPositionsNew(Vector3i center)
        {
            if (center == _lastCenter)
            {
                return _nearBlockPositions;
            }

            _lastCenter = center;
            int i = 0;
            foreach (var offset in _scanOffsets)
            {
                _nearBlockPositions[i++] = center + offset; 
            }

            return _nearBlockPositions;
        }
        
        private IEnumerator ScanCoroutine(EntityPlayerLocal player)
        {
            _scanRunning = true;
            var center = player.position.FloorToInt();
            GenerateNearPositionsNew(center);
            
            for (int i = 0; i < _totalNearBlocks; i++)
            {
                double frameStartTime = Time.realtimeSinceStartupAsDouble;
                int startI = i;
                while (i < _totalNearBlocks && _timePerFrame > (Time.realtimeSinceStartupAsDouble - frameStartTime))
                {
                    var position = _nearBlockPositions[i++];
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
                            if (_blocks.ContainsKey(position))
                            {
                                RemoveBlockAtPosition(position);
                            }

                            continue;
                        }

                        // If we don't already have this block generated, generate it now.
                        if (!_blocks.TryGetValue(position, out RepairVisionBlock block))
                        {
                            GameObject damageBlock = null;
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
                            block = new RepairVisionBlock() { GameObject = damageBlock, Position = position };
                            block.Update(hpPercent);
                            _blocks.Add(position, block);
                        }

                        // If we got here and have a null block, that's a bad sign. Remove the position so it can
                        // be re-generated next frame.
                        if (block == null)
                        {
                            Logging.Error($"Position {position} had bad block, removing.");
                            _blocks.Remove(position);
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Error(e.ToString());
                    }
                }

                if (i < _totalNearBlocks)
                {
                    var blocksProcessed = i - startI;
                    if (blocksProcessed < 100)
                    {
                        Logging.Error($"RepairVision scan only processed {i - startI} blocks with time allocation {_timePerFrame}ms! Consider adjusting!");
                    }
                    else if (blocksProcessed < 500)
                    {
                        Logging.Warning($"RepairVision scan only processed {i - startI} blocks with time allocation {_timePerFrame}ms! Consider adjusting!");
                    }

                    yield return null;
                }
            }

            // Remove far blocks.
            RemoveFarBlocks(center);

            _scanRunning = false;
        }

        private void RemoveFarBlocks(Vector3i center)
        {
            var positions = _blocks.Keys.ToList();
            foreach (var position in positions)
            {
                if ((center - position).Magnitude() > _maxDistance)
                {
                    Logging.Error($"Removing far block at {position}");
                    RemoveBlockAtPosition(position);
                }
            }
        }

        public void UpdateBlock(Vector3i blockPosition, float hpPercent)
        {
            if (_blocks.TryGetValue(blockPosition, out var block))
            {
                block.Update(hpPercent);
            }
        }

        public void RemoveBlockAtPosition(Vector3i blockPosition)
        {
            if (_blocks.TryGetValue(blockPosition, out var block))
            {
                Origin.Remove(block.GameObject.transform);
                Object.Destroy(block.GameObject);
                _blocks.Remove(blockPosition);
            }
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
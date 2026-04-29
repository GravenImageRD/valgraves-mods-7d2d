using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Valgraves.Common;
using WorldGenerationEngineFinal;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace RepairVision
{
    public static class BlockHelpers
    {
        private static GameObject _blockObject = null;
        private static Material _blockMaterial = null;
        private static Dictionary<string, GameObject> _entityObjects = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _shapeObjects = new Dictionary<string, GameObject>();

        private static GameObject GenerateDoorObject(ref BlockEntityData tileEntity)
        {
            return new GameObject();
        }

        private static GameObject GenerateGenericObject(ref BlockEntityData tileEntity)
        {
            GameObject entityObject = new GameObject();
            var meshes = tileEntity.transform.GetComponentsInChildren<MeshFilter>();
            var processedMeshes = new List<string>();
            foreach (var mesh in meshes)
            {
                var filterName = Regex.Replace(mesh.name, "(_LOD\\d+)$", string.Empty);
                if (processedMeshes.Contains(filterName))
                {
                    Logging.Warning($"Skipping mesh {mesh.name} because it is an extra LOD");
                    continue;
                }

                processedMeshes.Add(filterName);
                var meshObject = new GameObject();
                meshObject.AddComponent<MeshFilter>().mesh = mesh.mesh;
                meshObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
                //var localPos = tileEntity.transform.InverseTransformPoint(mesh.transform.position);
                //var localRot = Quaternion.Inverse(tileEntity.transform.rotation) * mesh.transform.rotation;
                // meshObject.transform.localPosition = mesh.transform.localPosition;
                // meshObject.transform.localRotation = localRot;
                meshObject.transform.SetParent(entityObject.transform);
            }

            return entityObject;
        }

        public static GameObject GenerateEntityObject(ref BlockEntityData tileEntity)
        {
            if (!_entityObjects.TryGetValue(tileEntity.blockValue.Block.GetBlockName(), out GameObject entityObject))
            {
                switch (tileEntity.blockValue.Block.BlockTag)
                {
                    case BlockTags.ClosetDoor:
                    case BlockTags.Door:
                    {
                        entityObject = GenerateDoorObject(ref tileEntity);
                        break;
                    }

                    default:
                    {
                        entityObject = GenerateGenericObject(ref tileEntity);
                        break;
                    }
                }
                _entityObjects.Add(tileEntity.blockValue.Block.GetBlockName(), entityObject);
            }

            var newEntityObject = Object.Instantiate(entityObject);
            newEntityObject.SetActive(true);
            return newEntityObject;
        }
        
        public static GameObject GenerateShapeObject(ref BlockValue blockValue, ref BlockShapeNew blockShape)
        {
            if (!_shapeObjects.TryGetValue(blockShape.ShapeName, out GameObject shapeObject))
            {
                shapeObject = new GameObject();
                _shapeObjects.Add(blockShape.ShapeName, shapeObject);
                var pivotObject = new GameObject("pivot");
                pivotObject.transform.position += new Vector3(0.5f, 0.5f, 0.5f);
                foreach (var mesh in blockShape.colliderMeshes)
                {
                    if (mesh?.Vertices == null || !mesh.Vertices.Any() || mesh.Indices == null || !mesh.Indices.Any())
                    {
                        continue;
                    }

                    var meshObject = new GameObject();
                    var newMesh = new Mesh
                    {
                        vertices = mesh.Vertices.ToArray(),
                        triangles = mesh.Indices.Select(x => (int)x).ToArray()
                    };
                    newMesh.RecalculateNormals();
                    newMesh.RecalculateBounds();
                    meshObject.AddComponent<MeshFilter>().mesh = newMesh;
                    meshObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
                    meshObject.transform.SetParent(pivotObject.transform);
                    meshObject.SetActive(true);
                }

                pivotObject.transform.localScale *= 1.01f;
                pivotObject.transform.SetParent(shapeObject.transform);
            }

            var newShapeObject = Object.Instantiate(shapeObject);
            newShapeObject.SetActive(true);
            return newShapeObject;
        }

        private static Material GetBlockMaterial()
        {
            if (_blockMaterial == null)
            {
                var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var bundlePath = Path.Combine(exePath, "Resources", "repairvision.unity3d");
                var bundle = AssetBundle.LoadFromFile(bundlePath);
                var shaders = bundle.LoadAllAssets<Shader>();
                _blockMaterial = new Material(shaders[0]);
                _blockMaterial.renderQueue = 4000;
            }
            return _blockMaterial;
        }
        
        public static GameObject GenerateBlockObject()
        {
            if (_blockObject == null)
            {
                _blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _blockObject.transform.localScale = Vector3.one * 1.05f;
                Object.Destroy(_blockObject.GetComponent<BoxCollider>()); // Remove unneeded physics.
                _blockObject.GetComponent<MeshRenderer>().material = GetBlockMaterial();
            }

            var newBlock = Object.Instantiate(_blockObject);
            newBlock.SetActive(true);
            SceneManager.MoveGameObjectToScene(newBlock, SceneManager.GetActiveScene());
            return newBlock;
        }
    }
    
    [HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.Update))]
    public class RepairVisionBuffEffect
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
                var centerWorld = new Vector3i(Player.Entity.transform.position);
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
                            if (!_blocks.TryGetValue(position, out GameObject damageBlock))
                            {
                                // If the block isn't in bad shape, skip it and move on.
                                if (hpPercent > .9f)
                                {
                                    continue;
                                }

                                var blockPosition = (centerWorld + scanOffset).ToVector3();
                                var blockRotation = blockValue.Block.shape.GetRotation(blockValue);
                                if (blockValue.Block.shape is BlockShapeNew blockShape)
                                {
                                    damageBlock = BlockHelpers.GenerateShapeObject(ref blockValue, ref blockShape);
                                    var pivot = damageBlock.GetComponentsInChildren<Transform>()
                                        .FirstOrDefault(x => x.name == "pivot");
                                    pivot.transform.rotation = blockRotation;
                                }
                                else if (blockValue.Block.HasTileEntity)
                                {
                                    var chunk = GameManager.Instance.World.GetChunkFromWorldPos(position);
                                    var blockEntity = chunk.GetBlockEntity(position);
                                    if (blockEntity == null)
                                    {
                                        Logging.Error($"Block {blockValue.Block.GetBlockName()} has no BlockEntity, using cube.");
                                        damageBlock = BlockHelpers.GenerateBlockObject();
                                    }
                                    else
                                    {
                                        if (blockEntity.bHasTransform)
                                        {
                                            damageBlock = BlockHelpers.GenerateEntityObject(ref blockEntity);
                                            blockPosition = blockEntity.transform.position;
                                            damageBlock.transform.rotation = blockEntity.transform.rotation;
                                        }
                                        else
                                        {
                                            Logging.Error($"Block {blockValue.Block.GetBlockName()} BlockEntityData has no transform, using cube.");
                                            damageBlock = BlockHelpers.GenerateBlockObject();
                                        }
                                    }                                    
                                }
                                else
                                {
                                    damageBlock = BlockHelpers.GenerateBlockObject();
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
                                blockColor.a = 0.2f * distanceMod;
                                damageBlock.GetComponent<MeshRenderer>()?.material?.SetColor("_Color", blockColor);
                                foreach (var renderers in damageBlock.GetComponentsInChildren<MeshRenderer>())
                                {
                                    renderers.material.SetColor("_Color", blockColor);
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
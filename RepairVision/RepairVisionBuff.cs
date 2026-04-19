using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Valgraves.Common;
using WorldGenerationEngineFinal;
using Object = UnityEngine.Object;

namespace RepairVision
{
    public static class BlockHelpers
    {
        private static GameObject _blockObject = null;

        public static GameObject GenerateBlockObject(Vector3i position)
        {
            if (_blockObject == null)
            {
                _blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(_blockObject.GetComponent<BoxCollider>()); // Remove unneeded physics.
                _blockObject.transform.localScale = Vector3.one * 1.03f;

                var blockMaterial = new Material(Shader.Find("Standard"));
                blockMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                blockMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                blockMaterial.SetInt("_ZWrite", 0);
                blockMaterial.DisableKeyword("_ALPHATEST_ON");
                blockMaterial.DisableKeyword("_ALPHABLEND_ON");
                blockMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                blockMaterial.renderQueue = 3000;
                blockMaterial.SetFloat("_Mode", 3f);
                blockMaterial.SetFloat("_Glossiness", 0f);
                blockMaterial.SetFloat("_SpecularHighlights", 0f);
                blockMaterial.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                blockMaterial.SetFloat("_GlossyReflections", 0f);
                blockMaterial.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                _blockObject.GetComponent<MeshRenderer>().material = blockMaterial;
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
        private static readonly Color _startColor = new Color(1f, 0.92156863f, 0.015686275f, 0.8f);
        private static readonly Color _endColor = new Color(0.8f, 0f, 0f, 0.8f);
        
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
                int scanRange = 25;
                for (int i = -scanRange; i <= scanRange; i++)
                {
                    for (int j = -scanRange; j <= scanRange; j++)
                    {
                        for (int k = -scanRange; k <= scanRange; k++)
                        {
                            var position = new Vector3i(center.x + i, center.y + j, center.z + k);
                            TileEntity tileEntity = GameManager.Instance.World.GetTileEntity(position);
                            if (tileEntity?.block == null || (tileEntity.blockValue.isTerrain && !tileEntity.block.GroupNames.Contains("Building")))
                            {
                                continue;
                            }

                            nearBlockPositions.Add(position);
                            var hpPercent = (1.0f * (tileEntity.block.MaxDamage - tileEntity.blockValue.damage)) / tileEntity.block.MaxDamage;
                            if (!_blocks.TryGetValue(position, out GameObject damageBlock))
                            {
                                // If the block isn't in bad shape, skip it and move on.
                                if (hpPercent > .9f)
                                {
                                    continue;
                                }
                                
                                var blockEntityData = tileEntity.chunk.GetBlockEntity(position);
                                if (blockEntityData?.transform?.position == null)
                                {
                                    continue;
                                }
                                
                                damageBlock = BlockHelpers.GenerateBlockObject(position);
                                damageBlock.transform.position = blockEntityData.transform.position;
                                //Modify damage block based on the tile entity type, like doors.
                                if (tileEntity.block.isMultiBlock)
                                {
                                    damageBlock.transform.localScale = tileEntity.block.multiBlockPos.dim * 1.03f;
                                    damageBlock.transform.rotation = blockEntityData.transform.rotation;
                                    if (tileEntity.block.BlockTag == BlockTags.Door)
                                    {
                                        // Doors and double doors need to be offset
                                        damageBlock.transform.position += new Vector3(0, 1, 0);
                                        
                                    }
                                }
                                _blocks.Add(position, damageBlock);
                            }
                            
                            // Delete old blocks if the block is now repaired enough.
                            if (hpPercent > 0.9f)
                            {
                                Object.Destroy(_blocks[position]);
                                _blocks.Remove(position);
                                continue;
                            }

                            var distanceVec = Player.Entity.transform.position - damageBlock.transform.position;
                            var distanceMod = Math.Max(0f, 1.0f - (distanceVec.magnitude / 15));
                            var blockColor = Color.Lerp(_startColor, _endColor, (0.9f - hpPercent));
                            blockColor.a *= distanceMod;
                            damageBlock.GetComponent<MeshRenderer>().material.SetColor("_Color", blockColor);
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
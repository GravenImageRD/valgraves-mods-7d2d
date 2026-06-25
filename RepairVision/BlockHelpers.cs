using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniLinq;
using UnityEngine;
using Valgraves.Common;
using Object = UnityEngine.Object;

namespace RepairVision
{
    public static class BlockHelpers
    {
        private static GameObject _blockObject = null;
        private static Material _blockMaterial = null;
        private static Dictionary<string, GameObject> _prefabObjects = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _entityObjects = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _shapeObjects = new Dictionary<string, GameObject>();
        public static float FadeStart;
        public static float FadeEnd;

        private static Vector3 GetRelativeScale(Vector3 rootScale, Vector3 childScale)
        {
            return new Vector3(
                childScale.x / rootScale.x,
                childScale.y / rootScale.y,
                childScale.z / rootScale.z
            );
        }
        
        private static GameObject GenerateDoorObject(ref BlockEntityData tileEntity)
        {
            GameObject entityObject = new GameObject();
            
            var meshFilters = tileEntity.transform.GetComponentsInChildren<MeshFilter>(true);
            var skinnedMeshRenderers = tileEntity.transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            
            // Add frame.
            var frameObject = new GameObject();
            var frameMesh = meshFilters.FirstOrDefault(x => x.name.ContainsCaseInsensitive("Frame"));
            if (!frameMesh)
            {
                Logging.Warning($"Couldn't find frameMesh for tile {tileEntity.transform.name}");
            }
            else
            {
                frameObject.AddComponent<MeshFilter>().mesh = frameMesh.mesh;
                frameObject.AddComponent<MeshRenderer>().material = _blockMaterial;
                frameObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, frameMesh.transform.lossyScale);
                frameObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(frameMesh.transform.position);
                frameObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * frameMesh.transform.rotation;
                frameObject.transform.SetParent(entityObject.transform);
            }

            // Add main door if possible.
            var doorMesh = skinnedMeshRenderers.FirstOrDefault(x => x.name.ContainsCaseInsensitive("DMG0_LOD0"));
            if (!doorMesh)
            {
                Logging.Warning($"Couldn't find doorMesh for tile {tileEntity.transform.name}");
            }
            else
            {
                var doorObject = new GameObject();
                doorObject.AddComponent<MeshFilter>().mesh = doorMesh.sharedMesh;
                var doorRenderer = doorObject.AddComponent<MeshRenderer>();
                var tileEntityRenderer = doorMesh.GetComponent<Renderer>();
                List<Material> doorMaterials = new List<Material>();
                for (int i = 0; i < tileEntityRenderer.materials.Length; i++)
                {
                    doorMaterials.Add(_blockMaterial);
                }

                doorRenderer.materials = doorMaterials.ToArray();
                doorObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, doorMesh.transform.lossyScale);
                doorObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(doorMesh.transform.position);
                doorObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * doorMesh.transform.rotation;
                doorObject.transform.SetParent(entityObject.transform);
            }

            return entityObject;
        }
        
        private static GameObject GenerateTurretObject(ref BlockEntityData tileEntity)
        {
            GameObject entityObject = new GameObject();
            
            
            var meshes = tileEntity.transform.GetComponentsInChildren<MeshFilter>(true);
            var baseMesh = meshes.FirstOrDefault(x => x.name.Contains("Base_LOD0"));
            var rotateMesh = meshes.FirstOrDefault(x => x.name.Contains("Rotate_LOD0"));
            var pitchMesh = meshes.FirstOrDefault(x => x.name.Contains("Pitch_LOD0"));
            
            // Add base.
            if (!baseMesh)
            {
                Logging.Warning($"Couldn't find mesh for Base on turret entity {tileEntity.transform.name}, will use a cube instead");
                Object.Destroy(entityObject);
                return null;
            }
            
            var baseObject = new GameObject();
            baseObject.AddComponent<MeshFilter>().mesh = baseMesh.mesh;
            baseObject.AddComponent<MeshRenderer>().material = _blockMaterial;
            baseObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, baseMesh.transform.lossyScale);
            baseObject.transform.SetParent(entityObject.transform);
            
            // Add rotate object.
            var rotateChild = tileEntity.transform.Find("Rotate");
            if (!rotateChild)
            {
                Logging.Warning($"Couldn't find Rotate child on turret entity {tileEntity.transform.name}");
            }
            else
            {
                var rotateObject = new GameObject();
                rotateObject.AddComponent<MeshFilter>().mesh = rotateMesh.mesh;
                rotateObject.AddComponent<MeshRenderer>().material = _blockMaterial;
                rotateObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, rotateMesh.transform.lossyScale);
                rotateObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(rotateChild.transform.position);
                rotateObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * rotateChild.transform.rotation;
                rotateObject.transform.SetParent(baseObject.transform);
                
                // Add pitch object.
                var pitchChild = rotateChild.Find("Pitch");
                if (!pitchChild)
                {
                    Logging.Warning($"Couldn't find Pitch child on turret entity {tileEntity.transform.name}");
                }
                else
                {
                    var pitchObject = new GameObject();
                    pitchObject.AddComponent<MeshFilter>().mesh = pitchMesh.mesh;
                    var pitchRenderer = pitchObject.AddComponent<MeshRenderer>();
                    pitchRenderer.materials = new Material[] { _blockMaterial, _blockMaterial };
                    pitchObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, pitchMesh.transform.lossyScale);
                    pitchObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(pitchChild.transform.position);
                    pitchObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * pitchChild.transform.rotation;
                    pitchObject.transform.SetParent(rotateObject.transform);
                }
            }

            return entityObject;
        }

        private static GameObject GenerateGenericObject(ref BlockEntityData tileEntity)
        {
            GameObject entityObject = new GameObject();
            entityObject.transform.localScale = tileEntity.transform.localScale;
            var meshes = tileEntity.transform.GetComponentsInChildren<MeshFilter>(true);
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
                meshObject.AddComponent<MeshRenderer>().material = _blockMaterial;
                var localPos = tileEntity.transform.InverseTransformPoint(mesh.transform.position);
                var localRot = Quaternion.Inverse(tileEntity.transform.rotation) * mesh.transform.rotation;
                meshObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, mesh.transform.lossyScale);
                meshObject.transform.localPosition = localPos;
                meshObject.transform.localRotation = localRot;
                meshObject.transform.SetParent(entityObject.transform);
            }

            return entityObject;
        }

        public static GameObject GeneratePrefabObject(string prefabName, Vector3i dimensions, Vector3 offset)
        {
            if (!_prefabObjects.TryGetValue(prefabName, out GameObject entityObject))
            {
                var prefabId = DataLoader.ParseDataPathIdentifier(prefabName);
                var prefab = DataLoader.LoadAsset<GameObject>(prefabId);
                if (prefab == null)
                {
                    Logging.Error($"Failed to find prefab {prefabName}");
                    return null;
                }

                entityObject = new GameObject();
                // var xOff = (float)Math.Floor(dimensions.x / 2.0f);
                // var zOff = (float)Math.Floor(dimensions.z / 2.0f);
                var meshes = prefab.transform.GetComponentsInChildren<MeshFilter>(true);
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
                    meshObject.AddComponent<MeshRenderer>().material = _blockMaterial;
                    var localPos = prefab.transform.InverseTransformPoint(mesh.transform.position);
                    var localRot = Quaternion.Inverse(prefab.transform.rotation) * mesh.transform.rotation;
                    meshObject.transform.localScale = GetRelativeScale(prefab.transform.lossyScale, mesh.transform.lossyScale);
                    meshObject.transform.localPosition = localPos;// - new Vector3(xOff, 0, zOff) + offset;
                    meshObject.transform.localRotation = localRot;
                    meshObject.transform.SetParent(entityObject.transform);
                }
                entityObject.SetActive(false);
                _prefabObjects.Add(prefabName, entityObject);
            }

            var newEntityObject = Object.Instantiate(entityObject);
            newEntityObject.SetActive(true);
            return newEntityObject;
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
                        if (tileEntity.transform.GetComponentInChildren<AutoTurretController>() != null)
                        {
                            entityObject = GenerateTurretObject(ref tileEntity);
                        }
                        else
                        {
                            entityObject = GenerateGenericObject(ref tileEntity);
                        }

                        break;
                    }
                }

                if (!entityObject)
                {
                    return null;
                }

                entityObject.SetActive(false);
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
                foreach (var mesh in blockShape.visualMeshes)
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
                    meshObject.AddComponent<MeshRenderer>().material = _blockMaterial;
                    meshObject.transform.SetParent(pivotObject.transform);
                }
                pivotObject.transform.SetParent(shapeObject.transform);
            }

            var newShapeObject = Object.Instantiate(shapeObject);
            newShapeObject.SetActive(true);
            return newShapeObject;
        }
        
        public static GameObject GenerateBlockObject()
        {
            var newBlock = Object.Instantiate(_blockObject);
            newBlock.SetActive(true);
            return newBlock;
        }

        public static void Initialize(AssetBundle  assetBundle)
        {
            var shaders = assetBundle.LoadAllAssets<Shader>();
            _blockMaterial = new Material(shaders[0]);
            FadeStart = (int)Math.Floor(RepairVision.Config.ScanRange / 3.0f);
            FadeEnd = (int)Math.Floor(FadeStart * 2.0);
            _blockMaterial.SetFloat("_FadeStartDist", FadeStart);
            _blockMaterial.SetFloat("_FadeEndDist", FadeEnd);
            
            _blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(_blockObject.GetComponent<BoxCollider>());
            _blockObject.GetComponent<MeshRenderer>().material = _blockMaterial;
            _blockObject.SetActive(false);
        }

        public static void CleanUp()
        {
            var oldPrefabObjects = _prefabObjects.Values;
            _prefabObjects = new Dictionary<string, GameObject>();
            
            var oldEntityObjects = _entityObjects.Values;
            _entityObjects =  new Dictionary<string, GameObject>();
            
            var oldShapeObjects = _shapeObjects.Values;
            _shapeObjects =  new Dictionary<string, GameObject>();
            
            foreach (var entityObject in oldEntityObjects)
            {
                Object.Destroy(entityObject);
            }
            
            foreach (var shapeObject in oldShapeObjects)
            {
                Object.Destroy(shapeObject);
            }
            
            foreach (var prefabObject in oldPrefabObjects)
            {
                Object.Destroy(prefabObject);
            }
        }
    }
}
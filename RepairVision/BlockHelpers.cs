using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UniLinq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valgraves.Common;

namespace RepairVision
{
    public static class BlockHelpers
    {
        private static GameObject _blockObject = null;
        private static Material _blockMaterial = null;
        private static Dictionary<string, GameObject> _entityObjects = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _shapeObjects = new Dictionary<string, GameObject>();

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
            
            // Add base, used for positioning and rotation.
            var doorChild = tileEntity.transform.Find("Door");
            var baseObject = new GameObject();
            baseObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, doorChild.lossyScale);
            baseObject.transform.position = doorChild.transform.localPosition;
            baseObject.transform.rotation = doorChild.transform.localRotation;
            baseObject.transform.SetParent(entityObject.transform);
            
            var meshFilters = doorChild.GetComponentsInChildren<MeshFilter>();
            var skinnedMeshRenderers = doorChild.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            // Add frame.
            var frameObject = new GameObject();
            var frameMesh = meshFilters.First(x => x.name.ContainsCaseInsensitive("Frame"));
            frameObject.AddComponent<MeshFilter>().mesh = frameMesh.mesh;
            frameObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
            frameObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, frameMesh.transform.lossyScale);
            frameObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(frameMesh.transform.position);
            frameObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * frameMesh.transform.rotation;
            frameObject.transform.SetParent(baseObject.transform);
            
            // Add main door if possible.
            var doorMesh = skinnedMeshRenderers.FirstOrDefault(x => x.name.ContainsCaseInsensitive("DMG0_LOD0"));
            if (doorMesh == null)
            {
                Logging.Error($"Couldn't find doorMesh for tile {tileEntity.transform.name}");
            }
            else
            {
                Logging.Error($"Found doorMesh {doorMesh.name} for tile {tileEntity.transform.name}");
                var doorObject = new GameObject();
                doorObject.AddComponent<MeshFilter>().mesh = doorMesh.sharedMesh;
                var doorRenderer = doorObject.AddComponent<MeshRenderer>();
                var tileEntityRenderer = doorMesh.GetComponent<Renderer>();
                List<Material> doorMaterials = new List<Material>();
                for (int i = 0; i < tileEntityRenderer.materials.Length; i++)
                {
                    doorMaterials.Add(GetBlockMaterial());
                }

                doorRenderer.materials = doorMaterials.ToArray();
                doorObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, doorMesh.transform.lossyScale);
                doorObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(doorMesh.transform.position);
                doorObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * doorMesh.transform.rotation;
                doorObject.transform.SetParent(baseObject.transform);
            }

            return entityObject;
        }
        
        private static GameObject GenerateTurretObject(ref BlockEntityData tileEntity)
        {
            GameObject entityObject = new GameObject();
            var rotateChild = tileEntity.transform.Find("Rotate");
            var pitchChild = tileEntity.transform.Find("Rotate").Find("Pitch");
            var meshes = tileEntity.transform.GetComponentsInChildren<MeshFilter>(true);
            var baseMesh = meshes.First(x => x.name.Contains("Base_LOD0"));
            var rotateMesh = meshes.First(x => x.name.Contains("Rotate_LOD0"));
            var pitchMesh = meshes.First(x => x.name.Contains("Pitch_LOD0"));
            
            // Add base.
            var baseObject = new GameObject();
            baseObject.AddComponent<MeshFilter>().mesh = baseMesh.mesh;
            baseObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
            baseObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, baseMesh.transform.lossyScale);
            baseObject.transform.SetParent(entityObject.transform);
            
            // Add rotate.
            var rotateObject = new GameObject();
            rotateObject.AddComponent<MeshFilter>().mesh = rotateMesh.mesh;
            rotateObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
            rotateObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, rotateMesh.transform.lossyScale);
            rotateObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(rotateChild.transform.position);
            rotateObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * rotateChild.transform.rotation;
            rotateObject.transform.SetParent(baseObject.transform);
            
            // Add pitch.
            var pitchObject = new GameObject();
            pitchObject.AddComponent<MeshFilter>().mesh = pitchMesh.mesh;
            var pitchRenderer = pitchObject.AddComponent<MeshRenderer>();
            pitchRenderer.materials = new Material[] { GetBlockMaterial(), GetBlockMaterial() };
            pitchObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, pitchMesh.transform.lossyScale);
            pitchObject.transform.localPosition = tileEntity.transform.InverseTransformPoint(pitchChild.transform.position);
            pitchObject.transform.localRotation = Quaternion.Inverse(tileEntity.transform.rotation) * pitchChild.transform.rotation;
            pitchObject.transform.SetParent(rotateObject.transform);

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
                meshObject.AddComponent<MeshRenderer>().material = GetBlockMaterial();
                var localPos = tileEntity.transform.InverseTransformPoint(mesh.transform.position);
                var localRot = Quaternion.Inverse(tileEntity.transform.rotation) * mesh.transform.rotation;
                meshObject.transform.localScale = GetRelativeScale(tileEntity.transform.lossyScale, mesh.transform.lossyScale);
                meshObject.transform.localPosition = localPos;
                meshObject.transform.localRotation = localRot;
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
                //_blockObject.transform.position += new Vector3(0.5f, 0.5f, 0.5f);
                Object.Destroy(_blockObject.GetComponent<BoxCollider>()); // Remove unneeded physics.
                _blockObject.GetComponent<MeshRenderer>().material = GetBlockMaterial();
                _blockObject.SetActive(false);
            }

            var newBlock = Object.Instantiate(_blockObject);
            newBlock.SetActive(true);
            SceneManager.MoveGameObjectToScene(newBlock, SceneManager.GetActiveScene());
            return newBlock;
        }
    }
}
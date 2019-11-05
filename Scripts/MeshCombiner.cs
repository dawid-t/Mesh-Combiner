using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
	[SerializeField]
	private bool createMultiMaterialMesh = false, combineInactiveChildren = false, deactivateCombinedChildren = true,
		deactivateCombinedChildrenMeshRenderers = false, destroyCombinedChildren = false;
	[SerializeField]
	private string folderPath = "Prefabs/CombinedMeshes";
	[SerializeField]
	[Tooltip("MeshFilters with Meshes which we don't want to combine into one Mesh.")]
	private MeshFilter[] meshFiltersToSkip = new MeshFilter[0];

	public bool CreateMultiMaterialMesh { get { return createMultiMaterialMesh; } set { createMultiMaterialMesh = value; } }
	public bool CombineInactiveChildren { get { return combineInactiveChildren; } set { combineInactiveChildren = value; } }
	public bool DeactivateCombinedChildren
	{
		get { return deactivateCombinedChildren; }
		set
		{
			deactivateCombinedChildren = value;
			CheckDeactivateCombinedChildren();
		}
	}
	public bool DeactivateCombinedChildrenMeshRenderers
	{
		get { return deactivateCombinedChildrenMeshRenderers; }
		set
		{
			deactivateCombinedChildrenMeshRenderers = value;
			CheckDeactivateCombinedChildren();
		}
	}
	public bool DestroyCombinedChildren
	{
		get { return destroyCombinedChildren; }
		set
		{
			destroyCombinedChildren = value;
			CheckDestroyCombinedChildren();
		}
	}
	public string FolderPath { get { return folderPath; } set { folderPath = value; } }


	private void CheckDeactivateCombinedChildren()
	{
		if(deactivateCombinedChildren || deactivateCombinedChildrenMeshRenderers)
		{
			destroyCombinedChildren = false;
		}
	}

	private void CheckDestroyCombinedChildren()
	{
		if(destroyCombinedChildren)
		{
			deactivateCombinedChildren = false;
			deactivateCombinedChildrenMeshRenderers = false;
		}
	}

	/// <summary>
	/// Combine children's Meshes into one Mesh. Set 'showCreatedMeshInfo' to true if want to show info about created Mesh in the console.
	/// </summary>
	public void CombineMeshes(bool showCreatedMeshInfo)
	{
		#region Save our parent scale and our Transform and reset it temporarily:
		// When we are unparenting and get parent again then sometimes scale is a little bit different so save scale before unparenting:
		Vector3 oldScaleAsChild = transform.localScale;

		// If we have parent then his scale will affect to our new combined Mesh scale so unparent us:
		int positionInParentHierarchy = transform.GetSiblingIndex();
		Transform parent = transform.parent;
		transform.parent = null;

		// Thanks to this the new combined Mesh will have same position and scale in the world space like its children:
		Quaternion oldRotation = transform.rotation;
		Vector3 oldPosition = transform.position;
		Vector3 oldScale = transform.localScale;
		transform.rotation = Quaternion.identity;
		transform.position = Vector3.zero;
		transform.localScale = Vector3.one;
		#endregion Save Transform and reset it temporarily.

		#region Combine Meshes into one Mesh:
		if(!createMultiMaterialMesh)
		{
			CombineMeshesWithSingleMaterial(showCreatedMeshInfo);
		}
		else
		{
			CombineMeshesWithMutliMaterial(showCreatedMeshInfo);
		}
		#endregion Combine Meshes into one Mesh.

		#region Set old Transform values:
		// Bring back the Transform values:
		transform.rotation = oldRotation;
		transform.position = oldPosition;
		transform.localScale = oldScale;
		
		// Get back parent and same hierarchy position:
		transform.parent = parent;
		transform.SetSiblingIndex(positionInParentHierarchy);

		// Set back the scale value as child:
		transform.localScale = oldScaleAsChild;
		#endregion Set old Transform values.
	}

	private MeshFilter[] GetMeshFiltersToCombine()
	{
		// Get all MeshFilters belongs to this GameObject and its children:
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(combineInactiveChildren);

		// Delete first MeshFilter belongs to this GameObject in meshFiltersToSkip array:
		meshFiltersToSkip = meshFiltersToSkip.Where((meshFilter) => meshFilter != meshFilters[0]).ToArray();

		// Delete null values in meshFiltersToSkip array:
		meshFiltersToSkip = meshFiltersToSkip.Where((meshFilter) => meshFilter != null).ToArray();

		for(int i = 0; i < meshFiltersToSkip.Length; i++)
		{
			meshFilters = meshFilters.Where((meshFilter) => meshFilter != meshFiltersToSkip[i]).ToArray();
		}

		return meshFilters;
	}

	private void CombineMeshesWithSingleMaterial(bool showCreatedMeshInfo)
	{
		// Get all MeshFilters belongs to this GameObject and its children:
		MeshFilter[] meshFilters = GetMeshFiltersToCombine();

		// First MeshFilter belongs to this GameObject so we don't need it:
		CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length-1];

		for(int i = 0; i < meshFilters.Length-1; i++) // Skip first MeshFilter belongs to this GameObject in this loop.
		{
			combineInstances[i].subMeshIndex = 0;
			combineInstances[i].mesh = meshFilters[i+1].sharedMesh;
			combineInstances[i].transform = meshFilters[i+1].transform.localToWorldMatrix;
		}

		// Set Material from child:
		MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(combineInactiveChildren);
		if(meshRenderers.Length >= 2)
		{
			meshRenderers[0].sharedMaterials = new Material[1];
			meshRenderers[0].sharedMaterial = meshRenderers[1].sharedMaterial;
		}
		else
		{
			meshRenderers[0].sharedMaterials = new Material[0]; // Reset the MeshRenderer's Materials array.
		}

		// Create Mesh from combineInstances:
		Mesh combinedMesh = new Mesh();
		combinedMesh.name = name;
		combinedMesh.CombineMeshes(combineInstances);

		if(combinedMesh.vertices.Length <= 65535)
		{
			meshFilters[0].sharedMesh = combinedMesh;

			DeactivateCombinedGameObjects(meshFilters);
			if(showCreatedMeshInfo)
			{
				Debug.Log("<color=orange>Mesh \""+name+"\" is created from "+combineInstances.Length+" children meshes.</color>");
			}
		}
		else
		{
			Debug.Log("<color=red>The mesh vertex limit is 65535! The created mesh has "+combinedMesh.vertices.Length+" vertices.</color>");
		}
	}

	private void CombineMeshesWithMutliMaterial(bool showCreatedMeshInfo)
	{
		#region Get MeshFilters, MeshRenderers and unique Materials from all children:
		MeshFilter[] meshFilters = GetMeshFiltersToCombine();
		MeshRenderer[] meshRenderers = new MeshRenderer[meshFilters.Length];
		meshRenderers[0] = GetComponent<MeshRenderer>(); // Our (parent) MeshRenderer.

		List<Material> uniqueMaterialsList = new List<Material>();
		for(int i = 0; i < meshFilters.Length-1; i++)
		{
			meshRenderers[i+1] = meshFilters[i+1].GetComponent<MeshRenderer>();
			if(meshRenderers[i+1] != null)
			{
				Material[] materials = meshRenderers[i+1].sharedMaterials; // Get all Materials from child Mesh.
				for(int j = 0; j < materials.Length; j++)
				{
					if(!uniqueMaterialsList.Contains(materials[j])) // If Material doesn't exists in the list then add it.
					{
						uniqueMaterialsList.Add(materials[j]);
					}
				}
			}
		}
		#endregion Get MeshFilters, MeshRenderers and unique Materials from all children.

		#region Combine children Meshes with the same Material to create submeshes for final Mesh:
		List<CombineInstance> finalMeshCombineInstancesList = new List<CombineInstance>();

		for(int i = 0; i < uniqueMaterialsList.Count; i++) // Create each Mesh (submesh) from Meshes with the same Material.
		{
			List<CombineInstance> submeshCombineInstancesList = new List<CombineInstance>();

			for(int j = 0; j < meshFilters.Length-1; j++) // Get only childeren Meshes (skip our Mesh).
			{
				if(meshRenderers[j+1] != null)
				{
					Material[] submeshMaterials = meshRenderers[j+1].sharedMaterials; // Get all Materials from child Mesh.

					for(int k = 0; k < submeshMaterials.Length; k++)
					{
						// If Materials are equal, combine Mesh from this child:
						if(uniqueMaterialsList[i] == submeshMaterials[k])
						{
							CombineInstance combineInstance = new CombineInstance();
							combineInstance.subMeshIndex = k; // Mesh may consist of smaller parts - submeshes.
															  // Every part have different index. If there are 3 submeshes
															  // in Mesh then MeshRender needs 3 Materials to render them.
							combineInstance.mesh = meshFilters[j+1].sharedMesh;
							combineInstance.transform = meshFilters[j+1].transform.localToWorldMatrix;
							submeshCombineInstancesList.Add(combineInstance);
						}
					}
				}
			}

			// Create new Mesh (submesh) from Meshes with the same Material:
			Mesh submesh = new Mesh();
			submesh.CombineMeshes(submeshCombineInstancesList.ToArray(), true);

			CombineInstance finalCombineInstance = new CombineInstance();
			finalCombineInstance.subMeshIndex = 0;
			finalCombineInstance.mesh = submesh;
			finalCombineInstance.transform = Matrix4x4.identity;
			finalMeshCombineInstancesList.Add(finalCombineInstance);
		}
		#endregion Combine submeshes (children Meshes) with the same Material.

		#region Set Materials array & combine submeshes into one multimaterial Mesh:
		meshRenderers[0].sharedMaterials = uniqueMaterialsList.ToArray();

		Mesh combinedMesh = new Mesh();
		combinedMesh.name = name;
		combinedMesh.CombineMeshes(finalMeshCombineInstancesList.ToArray(), false);

		if(combinedMesh.vertices.Length <= 65535)
		{
			meshFilters[0].sharedMesh = combinedMesh;

			DeactivateCombinedGameObjects(meshFilters);
			if(showCreatedMeshInfo)
			{
				Debug.Log("<color=orange>Mesh \""+name+"\" is created from "+(meshFilters.Length-1)+" children meshes and have "+
				finalMeshCombineInstancesList.Count+" submeshes.</color>");
			}
		}
		else
		{
			Debug.Log("<color=red>The mesh vertex limit is 65535! The created mesh has "+combinedMesh.vertices.Length+" vertices.</color>");
		}
		#endregion Set Materials array & combine submeshes into one multimaterial Mesh.
	}

	private void DeactivateCombinedGameObjects(MeshFilter[] meshFilters)
	{
		for(int i = 0; i < meshFilters.Length-1; i++) // Skip first MeshFilter belongs to this GameObject in this loop.
		{
			if(!destroyCombinedChildren)
			{
				if(deactivateCombinedChildren)
				{
					meshFilters[i+1].gameObject.SetActive(false);
				}
				if(deactivateCombinedChildrenMeshRenderers)
				{
					MeshRenderer meshRenderer = meshFilters[i+1].gameObject.GetComponent<MeshRenderer>();
					if(meshRenderer != null)
					{
						meshRenderer.enabled = false;
					}
				}
			}
			else
			{
				DestroyImmediate(meshFilters[i+1].gameObject);
			}
		}
	}
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using ConvertCoordinates;
using System.Linq;

namespace Amsterdam3D.DataGeneration
{
	public class GenerateTreeData : MonoBehaviour
	{
		[Serializable]
		private class Tree
		{
			public string OBJECTNUMMER;
			public string Soortnaam_NL;
			public string Boomnummer;
			public string Soortnaam_WTS;
			public string Boomtype;
			public string Boomhoogte;
			public int Plantjaar;
			public string Eigenaar;
			public string Beheerder;
			public string Categorie;
			public string SOORT_KORT;
			public string SDVIEW;
			public string RADIUS;
			public string WKT_LNG_LAT;
			public string WKT_LAT_LNG;
			public double LNG;
			public double LAT;

			public Vector3RD RD;
			public Vector3 position;
			public float averageTreeHeight;

			public GameObject prefab;
		}

		[SerializeField]
		private GameObjectsGroup treeTypes;

		[SerializeField]
		private TextAsset[] bomenCsvDataFiles;

		private List<Tree> trees;

		private List<string> treeLines;

		[SerializeField]
		private Material previewMaterial;
		[SerializeField]
		private Material treesMaterial;

		private double tileSize = 1000.0;
		private string sourceGroundTilesFolder = "C:/Projects/GemeenteAmsterdam/1x1kmGroundTiles";

		private string[] treeNameParts;
		private string treeTypeName = "";

		[SerializeField]
		private Dictionary<string, string> noPrefabFoundNames;
		
		public void Start()
		{
			trees = new List<Tree>();
			treeLines = new List<string>();
			noPrefabFoundNames = new Dictionary<string, string>();

			ParseTreeData();
		}

		/// <summary>
		/// Put all the csv lines from csv files in one big array, before parsing them all.
		/// </summary>
		private void ParseTreeData()
		{
			foreach (var csvData in bomenCsvDataFiles)
			{
				string[] lines = csvData.text.Split('\n');
				for (int i = 1; i < lines.Length; i++)
				{
					if (lines[i].Contains(";"))
					{
						treeLines.Add(lines[i]);
					}
				}
			}
			Debug.Log("Tree dataset contains " + treeLines.Count);

			StartCoroutine(ParseTreeLines());
		}

		/// <summary>
		/// Parse all the CSV lines found within the .csv files and fill our list containing all the trees
		/// </summary>
		IEnumerator ParseTreeLines()
		{
			var lineNr = 0;
			Debug.Log("Started parsing tree dataset..");
			yield return new WaitForEndOfFrame();

			while (lineNr < treeLines.Count)
			{
				ParseTree(treeLines[lineNr]);
				lineNr++;
				if (lineNr % 10000 == 0)
				{
					Debug.Log("Parsing tree line nr: " + lineNr + "/" + treeLines.Count);
					yield return new WaitForEndOfFrame();
				}
			}

			Debug.Log("No prefabs were found for the following tree names: ");
			string listOfNamesNotFound = string.Join(";", noPrefabFoundNames.Select(x => x.Key).ToArray());
			Debug.Log(listOfNamesNotFound);

			Debug.Log("Done parsing tree lines. Start filling the tiles with trees..");

			StartCoroutine(TraverseTileFiles());

			yield return null;
		}

		/// <summary>
		/// Parse a tree from a string line to a new List item containing the following ; seperated fields:
		/// OBJECTNUMMER;Soortnaam_NL;Boomnummer;Soortnaam_WTS;Boomtype;Boomhoogte;Plantjaar;Eigenaar;Beheerder;Categorie;SOORT_KORT;SDVIEW;RADIUS;WKT_LNG_LAT;WKT_LAT_LNG;LNG;LAT;
		/// </summary>
		/// <param name="line">Text line matching the same fields as the header</param>
		private void ParseTree(string line)
		{
			string[] cell = line.Split(';');

			Tree newTree = new Tree()
			{
				OBJECTNUMMER = cell[0],
				Soortnaam_NL = cell[1],
				Boomnummer = cell[2],
				Soortnaam_WTS = cell[3],
				Boomtype = cell[4],
				Boomhoogte = cell[5],
				Plantjaar = int.Parse(cell[6]),
				Eigenaar = cell[7],
				Beheerder = cell[8],
				Categorie = cell[9],
				SOORT_KORT = cell[10],
				SDVIEW = cell[11],
				RADIUS = cell[12],
				WKT_LNG_LAT = cell[13],
				WKT_LAT_LNG = cell[14],
				LNG = double.Parse(cell[15]),
				LAT = double.Parse(cell[16])
			};

			//Extra generated tree data
			newTree.RD = CoordConvert.WGS84toRD(newTree.LNG, newTree.LAT);
			newTree.position = CoordConvert.WGS84toUnity(newTree.LNG, newTree.LAT);
			newTree.averageTreeHeight = EstimateTreeHeight(newTree.Boomhoogte);
			newTree.prefab = FindClosestPrefabTypeByName(newTree.Soortnaam_NL);

			trees.Add(newTree);
		}

		/// <summary>
		/// Find a prefab in our list of tree prefabs that has a substring matching a part of our prefab name.
		/// Make sure prefab names are unique to get unique results.
		/// </summary>
		/// <param name="treeTypeDescription">The string containing the tree type word</param>
		/// <returns>The prefab with a matching substring</returns>
		private GameObject FindClosestPrefabTypeByName(string treeTypeDescription)
		{
			treeNameParts = treeTypeDescription.Replace("\"", "").Split(' ');
			treeTypeName = treeNameParts[0].ToLower();

			foreach (var namePart in treeNameParts)
			{
				foreach (GameObject tree in treeTypes.items)
				{
					if (tree.name.ToLower().Contains(treeTypeName))
					{
						return tree;
					}
				}
			}
			noPrefabFoundNames[treeTypeDescription] = treeTypeDescription;
			return treeTypes.items[3]; //Just use an average tree prefab as default
		}

		/// <summary>
		/// Estimate the tree height according to the height description.
		/// We try to parse every number found, and use the average.
		/// </summary>
		/// <param name="description">For example: "6 to 8 m"</param>
		/// <returns></returns>
		private float EstimateTreeHeight(string description)
		{
			float treeHeight = 10.0f;

			string[] numbers = description.Split(' ');
			int numbersFoundInString = 0;
			float averageHeight = 0;
			foreach (string nr in numbers)
			{
				float parsedNumber = 10;

				if (float.TryParse(nr, out parsedNumber))
				{
					numbersFoundInString++;
					averageHeight += parsedNumber;
				}
			}
			if (numbersFoundInString > 0)
			{
				treeHeight = averageHeight / numbersFoundInString;
			}

			return treeHeight;
		}

		/// <summary>
		/// Load all the large ground tiles from AssetBundles, spawn it in our world, and start filling it with the trees that match the tile
		/// its RD coordinate rectangle. The tiles are named after the RD coordinates in origin at the bottomleft of the tile.
		/// </summary>
		private IEnumerator TraverseTileFiles()
		{
			var info = new DirectoryInfo(sourceGroundTilesFolder);
			var fileInfo = info.GetFiles();

			var currentFile = 0;
			while(currentFile < fileInfo.Length)
			{
				FileInfo file = fileInfo[currentFile];
				if (!file.Name.Contains(".manifest") && file.Name.Contains("_"))
				{
					Debug.Log("Filling tile" + file.Name);
					yield return new WaitForEndOfFrame();

					string[] coordinates = file.Name.Split('_');
					Vector3RD tileRDCoordinatesBottomLeft = new Vector3RD(double.Parse(coordinates[0]), double.Parse(coordinates[1]), 0);

					var assetBundleTile = AssetBundle.LoadFromFile(file.FullName);
					Mesh[] meshesInAssetbundle = new Mesh[0];
					try
					{
						meshesInAssetbundle = assetBundleTile.LoadAllAssets<Mesh>();
					}
					catch (Exception)
					{
						Debug.Log("Could not find a mesh in this assetbundle.");
						assetBundleTile.Unload(true);
					}

					GameObject newTile = new GameObject();
					newTile.name = file.Name;
					newTile.AddComponent<MeshFilter>().sharedMesh = meshesInAssetbundle[0];
					newTile.AddComponent<MeshCollider>().sharedMesh = meshesInAssetbundle[0];
					newTile.AddComponent<MeshRenderer>().material = previewMaterial;
					newTile.transform.position = CoordConvert.RDtoUnity(tileRDCoordinatesBottomLeft);

					GameObject treeRoot = new GameObject();
					treeRoot.name = file.Name.Replace("terrain", "trees");
					treeRoot.transform.position = newTile.transform.position;

					SpawnTreesInTile(treeRoot, tileRDCoordinatesBottomLeft);
				}
				currentFile++;
			}

			foreach (var file in fileInfo)
			{
				if (!file.Name.Contains(".manifest") && file.Name.Contains("_"))
				{
					Debug.Log(file.Name);
					string[] coordinates = file.Name.Split('_');
					Vector3RD tileRDCoordinatesBottomLeft = new Vector3RD(double.Parse(coordinates[0]), double.Parse(coordinates[1]), 0);

					var assetBundleTile = AssetBundle.LoadFromFile(file.FullName);
					Mesh[] meshesInAssetbundle = new Mesh[0];
					try
					{
						meshesInAssetbundle = assetBundleTile.LoadAllAssets<Mesh>();
					}
					catch (Exception)
					{
						Debug.Log("Could not find a mesh in this assetbundle.");
						assetBundleTile.Unload(true);
					}

					GameObject newTile = new GameObject();
					newTile.name = file.Name;
					newTile.AddComponent<MeshFilter>().sharedMesh = meshesInAssetbundle[0];
					newTile.AddComponent<MeshCollider>().sharedMesh = meshesInAssetbundle[0];
					newTile.AddComponent<MeshRenderer>().material = previewMaterial;
					newTile.transform.position = CoordConvert.RDtoUnity(tileRDCoordinatesBottomLeft);

					GameObject treeRoot = new GameObject();
					treeRoot.name = file.Name.Replace("terrain", "trees");
					treeRoot.transform.position = newTile.transform.position;

					SpawnTreesInTile(treeRoot, tileRDCoordinatesBottomLeft);
				}
			}
		}

		/// <summary>
		/// Spawn all the trees located within the RD coordinate bounds of the 1x1km tile.
		/// </summary>
		/// <param name="treeTile">The target 1x1 km ground tile</param>
		/// <param name="tileCoordinates">RD Coordinates of the tile</param>
		/// <returns></returns>
		private void SpawnTreesInTile(GameObject treeTile, Vector3RD tileCoordinates)
		{
			//TODO: Add all trees within this time (1x1km)
			//yield return new WaitForEndOfFrame(); //make sure collider is there

			int treeChecked = trees.Count -1;
			while (treeChecked >= 0)
			{
				Tree tree = trees[treeChecked];

				if (tree.RD.x > tileCoordinates.x && tree.RD.y > tileCoordinates.y && tree.RD.x < tileCoordinates.x + tileSize && tree.RD.y < tileCoordinates.y + tileSize)
				{
					SpawnTreeOnGround(treeTile, tree);
					trees.RemoveAt(treeChecked);
				}
				treeChecked--;
			}

			Vector3 worldPosition = treeTile.transform.position;

			//Calculate offset. ( Our viewer expects tiles with the origin in the center )
			Vector3RD convertedOffset = CoordConvert.UnitytoRD(Vector3.zero);
			convertedOffset.x -= 500;
			convertedOffset.y -= 500;
			convertedOffset.z -= 43;
			treeTile.transform.position = CoordConvert.RDtoUnity(convertedOffset);

			//yield return new WaitForEndOfFrame();

			CreateTreeTile(treeTile, worldPosition);

			//yield return null;
		}

		/// <summary>
		/// Spawn a new tree object matching the tree data properties.
		/// </summary>
		/// <param name="treeTile">The root parent for the new tree</param>
		/// <param name="tree">The tree data object containing our tree properties</param>
		private void SpawnTreeOnGround(GameObject treeTile, Tree tree)
		{
			GameObject newTreeInstance = Instantiate(tree.prefab, treeTile.transform);

			//Apply properties/variations based on tree data
			newTreeInstance.name = tree.OBJECTNUMMER;
			newTreeInstance.transform.localScale = Vector3.one * 0.1f * tree.averageTreeHeight;
			newTreeInstance.transform.Rotate(0, UnityEngine.Random.value * 360.0f, 0);

			float raycastHitY = Constants.ZERO_GROUND_LEVEL_Y;
			if (Physics.Raycast(tree.position + Vector3.up * 1000.0f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
			{
				raycastHitY = hit.point.y;
			}
			newTreeInstance.transform.position = new Vector3(tree.position.x, raycastHitY, tree.position.z);
		}

		/// <summary>
		/// Get all the child meshes of the tile, and merge them into one big tile mesh.
		/// </summary>
		/// <param name="treeTile">The root parent containing all our spawned trees</param>
		/// <param name="worldPosition">The position to move the tile to when it is done (for previewing purposes)</param>
		private void CreateTreeTile(GameObject treeTile, Vector3 worldPosition)
		{
			string assetName = "Assets/TreeTileAssets/" + treeTile.name + ".asset";

			MeshFilter[] meshFilters = treeTile.GetComponentsInChildren<MeshFilter>();
			CombineInstance[] combine = new CombineInstance[meshFilters.Length];
			for (int i = 0; i < combine.Length; i++)
			{
				combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

				Mesh treeMesh = meshFilters[i].mesh;						
				if (treeMesh.vertexCount > 0)
				{
					AddIDToMeshUV(treeMesh, int.Parse(meshFilters[i].name));
				}
				combine[i].mesh = treeMesh;
				meshFilters[i].gameObject.SetActive(false);
			}

			Mesh newCombinedMesh = new Mesh();
			if (meshFilters.Length > 0)
			{
				newCombinedMesh.name = treeTile.name;
				newCombinedMesh.CombineMeshes(combine, true);
			}

			treeTile.AddComponent<MeshFilter>().sharedMesh = newCombinedMesh;
			treeTile.AddComponent<MeshRenderer>().material = treesMaterial;
#if UNITY_EDITOR
			AssetDatabase.CreateAsset(newCombinedMesh, assetName);
			AssetDatabase.SaveAssets();
#endif

			treeTile.transform.position = worldPosition;
		}

		/// <summary>
		/// Adds a specific number to a mesh UV slot for all the verts
		/// </summary>
		/// <param name="treeMesh">The mesh to assign the ID to</param>
		/// <param name="objectNumber">The number to inject into the UV slot</param>
		private void AddIDToMeshUV(Mesh treeMesh, float objectNumber)
		{
			treeMesh.uv3 = new Vector2[treeMesh.vertexCount];
			Vector2 uvIds = new Vector2() { x = objectNumber, y = 0 };
			for (int j = 0; j < treeMesh.uv3.Length; j++)
			{
				treeMesh.uv3[j] = uvIds;
			}
		}
	}
}
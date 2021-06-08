﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConvertCoordinates;
using UnityEngine.Networking;
using System.Globalization;
using System;
using Netherlands3D.LayerSystem;
using Netherlands3D.Utilities;
using Netherlands3D;
using System.Linq;

namespace Amsterdam3D.Sewerage
{
	public class LineRenderLayer : Layer
    {
		[SerializeField]
		private ColorPalette nlcsColorPalette;

		[SerializeField]
		private Material lineRendererMaterial;

		public override void OnDisableTiles(bool isenabled) { }

		public override void HandleTile(TileChange tileChange, System.Action<TileChange> callback = null)
        {
			TileAction action = tileChange.action;
			var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
			switch (action)
			{
				case TileAction.Create:
					Tile newTile = CreateNewTile(tileChange,callback);
					tiles.Add(tileKey, newTile);
					break;
				case TileAction.Remove:
					InteruptRunningProcesses(tileKey);
					RemoveTile(tileChange, callback);
					return;
				default:
					callback(tileChange);
					break;
			}
		}
		
		private Tile CreateNewTile(TileChange tileChange, System.Action<TileChange> callback = null)
        {
			Tile tile = new Tile();
			tile.LOD = 0;
			tile.tileKey = new Vector2Int(tileChange.X, tileChange.Y);
			tile.layer = transform.gameObject.GetComponent<Layer>();
			tile.gameObject = new GameObject("cablesAndPipes-"+tileChange.X + "_" + tileChange.Y);
			tile.gameObject.transform.parent = transform.gameObject.transform;
			tile.gameObject.layer = tile.gameObject.transform.parent.gameObject.layer;
			tile.gameObject.SetActive(false);
			Generate(tileChange, tile, callback);
			return tile;
		}

		public override void InteruptRunningProcesses(Vector2Int tileKey)
		{
			if (!tiles.ContainsKey(tileKey)) return;

			if (tiles[tileKey].runningWebRequest != null)
				tiles[tileKey].runningWebRequest.Abort();

			if (tiles[tileKey].runningCoroutine != null)
			{
				StopCoroutine(tiles[tileKey].runningCoroutine);
			}
		}

		public void Generate(TileChange tileChange,Tile tile, System.Action<TileChange> callback = null)
		{
			tile.runningCoroutine = StartCoroutine(BuildInfrastructure(tileChange, tile,callback));
		}

		IEnumerator BuildInfrastructure(TileChange tileChange, Tile tile, Action<TileChange> callback = null)
		{
			var bbox = tile.tileKey.x + "," + tile.tileKey.y + "," + (tile.tileKey.x + tileSize) + "," + (tile.tileKey.y + tileSize);
			int startIndex = 0;
			var pagesRemaining = true;
			int maxResultsCount = 5000;

			while (pagesRemaining)
			{
				var url = $"https://api.data.amsterdam.nl/v1/wfs/leidingeninfrastructuur/?SERVICE=WFS&VERSION=2.0.0&REQUEST=GetFeature&TYPENAMES=ligging_lijn_totaal&OUTPUTFORMAT=application/json&BBox={bbox}&count={maxResultsCount}&startIndex={startIndex}";
				Debug.Log(url);

				//Create a new page per mesh, to see our results as fast as we can
				var mesh = new Mesh();
				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				List<int> indices = new List<int>();
				List<Vector3> vertices = new List<Vector3>();
				List<Color> colors = new List<Color>();

				tile.runningWebRequest = UnityWebRequest.Get(url);	
				yield return tile.runningWebRequest.SendWebRequest();

				if (tile.runningWebRequest.result == UnityWebRequest.Result.Success)
				{
					GeoJSON customJsonHandler = new GeoJSON(tile.runningWebRequest.downloadHandler.text);

					//Determine if there is a page after this
					if (customJsonHandler.geoJSONString.LastIndexOf("\"title\":\"next page\"") > -1)
					{
						startIndex += maxResultsCount - 1;
					}
					else
					{
						pagesRemaining = false;
					}

					yield return null;

					while (customJsonHandler.GotoNextFeature())
					{
						//Get the parameters that make up our feature line
						List<double> coordinates = customJsonHandler.getGeometryLineString();
						float depth = customJsonHandler.getPropertyFloatValue("diepte");
						var lineColor = GetNLCSColor(customJsonHandler.getPropertyStringValue("thema"));

						//Min. of two points? This is a line we can draw!
						if (coordinates.Count > 1)
						{
							//For every two coordinates
							for (int i = 0; i < coordinates.Count/2; i+=2)
							{	
								//Add coordinate with vertex color
								var point = CoordConvert.RDtoUnity(new Vector3((float)coordinates[i], (float)coordinates[i + 1], 0));
								point.y = depth;
								vertices.Add(point);

								colors.Add(lineColor);

								if (i > 3)
								{
									//Add the previous as line starting point
									indices.Add(vertices.Count - 2);
								}
								indices.Add(vertices.Count - 1);
							}								
						}
					}

					//All lines are read into mesh. Lets finish it
					GameObject childMeshGameObject = new GameObject();
					childMeshGameObject.transform.SetParent(tile.gameObject.transform,false);

					mesh.vertices = vertices.ToArray();
					mesh.colors = colors.ToArray();
					mesh.SetIndices(indices, MeshTopology.Lines, 0, true);

					childMeshGameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
					childMeshGameObject.AddComponent<MeshRenderer>().material = lineRendererMaterial;

					tile.gameObject.SetActive(true);
					yield return new WaitForEndOfFrame();
				}
				else
				{
					pagesRemaining = false;
				}
				
			}

			Debug.Log("Constructing line mesh");

			yield return null;

			callback(tileChange);
		}

		private Color GetNLCSColor(string theme)
		{
			//return color based on template
			foreach(NamedColor namedColor in nlcsColorPalette.colors)
			{
				if(namedColor.name.ToLower() == theme) 
				{
					return namedColor.color;
				}
			}
			return Color.white;
		}

		private void RemoveTile(TileChange tileChange, System.Action<TileChange> callback = null)
		{
			var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
			if (tiles.ContainsKey(tileKey))
			{
				Tile tile = tiles[tileKey];
				if (tile.gameObject)
				{
					MeshFilter[] meshFilters = tile.gameObject.GetComponentsInChildren<MeshFilter>();

					foreach (var meshfilter in meshFilters)
					{
						Destroy(meshfilter.sharedMesh);
					}

					Destroy(tile.gameObject);
				}

				tiles.Remove(tileKey);
			}
			callback(tileChange);
		}
	}
}
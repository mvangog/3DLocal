﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LayerSystem
{
    public class Layer : MonoBehaviour
    {
        [SerializeField]
        public Material DefaultMaterial;
        public Material HighlightMaterial;
        public int tileSize = 1000;
        public int layerPriority = 0;
        public List<DataSet> Datasets = new List<DataSet>();
        public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        private TileHandler tileHandler;

        void Start()
        {
            tileHandler = GetComponentInParent<TileHandler>();

            foreach (DataSet dataset in Datasets)
            {
                dataset.maximumDistanceSquared = dataset.maximumDistance * dataset.maximumDistance;
            }
        }

        /// <summary>
        /// Check object data of our tiles one frame at a time, and highlight matching ID's
        /// </summary>
        /// <param name="ids">List of unique (BAG) id's we want to highlight</param>
        public void Highlight(List<string> ids)
        {
            StopAllCoroutines();
            StartCoroutine(HighlightIDsOneTilePerFrame(ids));
        }

        /// <summary>
        /// Hide mesh parts with the matching object data ID's
        /// </summary>
        /// <param name="ids">List of unique (BAG) id's we want to hide</param>
        public void Hide(List<string> ids) 
        {
            StopAllCoroutines();
            StartCoroutine(HideIDsOneTilePerFrame(ids));
        }

        public void AddMeshColliders() 
        {
            MeshCollider meshCollider;
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            if (meshFilters == null)
            {
                return;
            }
            foreach (MeshFilter meshFilter in meshFilters)
            {
                meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                }
            }
        }
        private IEnumerator HighlightIDsOneTilePerFrame(List<string> ids)
        {
            tileHandler.pauseLoading = true;
            ObjectData objectData;
            Vector2[] UVs;
            foreach (KeyValuePair<Vector2Int, Tile> kvp in tiles)
            {
                objectData = kvp.Value.gameObject.GetComponent<ObjectData>();
                if (objectData != null)
                {
                    objectData.highlightIDs = ids.Where(targetID => objectData.ids.Any(objectId => objectId == targetID)).ToList<string>();
                    objectData.mesh = objectData.gameObject.GetComponent<MeshFilter>().mesh;
                    objectData.UpdateUVs();
                    yield return new WaitForEndOfFrame();
                }
            }
            tileHandler.pauseLoading = false;
        }

        private IEnumerator HideIDsOneTilePerFrame(List<string> ids)
        {
            tileHandler.pauseLoading = true;
            ObjectData objectData;
            Vector2[] UVs;
            foreach (KeyValuePair<Vector2Int, Tile> kvp in tiles)
            {
                objectData = kvp.Value.gameObject.GetComponent<ObjectData>();
                if (objectData != null)
                {
                    if (ids.Count > 0)
                    {
                        objectData.hideIDs.AddRange(ids.Where(targetID => objectData.ids.Any(objectId => objectId == targetID)).ToList<string>());
                    }
                    else{
                        objectData.hideIDs.Clear();

                    }
                    objectData.mesh = objectData.gameObject.GetComponent<MeshFilter>().mesh;
                    objectData.UpdateUVs();
                    yield return new WaitForEndOfFrame();
                }
            }
            tileHandler.pauseLoading = false;
        }
    }
}

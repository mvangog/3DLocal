﻿using Netherlands3D.Underground;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.ModelParsing
{
    public class MaterialLibrary : MonoBehaviour
    {
        [SerializeField]
        private Material[] materialLibrary;

        /// <summary>
        /// Remaps materials to this object based on material name / substrings
        /// </summary>
        /// <param name="renderer">The GameObject containing the renderer with the materials list</param>
        public void AutoRemap(GameObject gameObjectWithRenderer)
        {
            var renderer = gameObjectWithRenderer.GetComponent<MeshRenderer>();
            if(!renderer)
            {
                Debug.LogWarning("No meshrenderer found in this GameObject. Skipping auto remap.");
                return;
			}

            var materialArray = renderer.materials;
            for (int i = 0; i < materialArray.Length; i++)
			{
                var replacement = FindMaterialReplacement(materialArray[i], true);
                replacement.name = replacement.name.Replace("(Clone)", "");
                ClearMask(replacement);
                materialArray[i] = replacement;
            }
            renderer.materials = materialArray;

        }

        private void ClearMask(Material targetMaterialWithMask)
        {
            //Our materials plucked from library might have some masking set. Clear those
            targetMaterialWithMask.SetTexture(RuntimeMask.clippingMaskTexture, null);
        }

        /// <summary>
        /// Finds a material from the library with a similar name
        /// </summary>
        /// <param name="comparisonMaterial">The material to find a library material for</param>
        /// <returns></returns>
        public Material FindMaterialReplacement(Material comparisonMaterial, bool returnAsInstance = false)
		{
			foreach(var libraryMaterial in materialLibrary)
            {
                if(comparisonMaterial.name.ToLower().Contains(libraryMaterial.name.ToLower()))
                {
                    Debug.Log("Found library material: " + libraryMaterial.name);
                    if (returnAsInstance) return Instantiate(libraryMaterial);
                    return libraryMaterial;
				}
			}

            //Didnt find a replacement? Just return myself.
            return comparisonMaterial;
		}
	}
}
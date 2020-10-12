﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
public abstract class ImportAPI : MonoBehaviour
{
    [HideInInspector]
    public string dataResult;
    // Start is called before the first frame update
    public IEnumerator CallAPI(string apiUrl, string bogIndexInt, int resultIndex)
    {
        
        // voegt data ID en url samen tot één geheel
        string url = apiUrl + bogIndexInt + "/";
        //Debug.Log(url);
        // stuurt een HTTP request naar de pagina
        var request = UnityWebRequest.Get(url);
        {
            yield return request.SendWebRequest();

            if (request.isDone && !request.isHttpError)
            {
                dataResult = request.downloadHandler.text;
            }
        }
    }
   
    //public virtual void DataReceived(string dataResult, params string[] values)
    //{
    //    // do something here
    //}

}
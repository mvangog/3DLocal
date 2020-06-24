﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public class Uploader : MonoBehaviour
{
    //regel 103 downloaden via javascript (zonder server)  nog testen in webgl
    public static string Key = "87ajdf898##@@jjKJA";
  //  public static string Server = "http://127.0.0.1:80/saven/";  
    public static string Server = "https://3d.amsterdam.nl/";
    public static string UploadUrl = Server + "customUpload.php";
    public static string DownloadUrl = Server + "customDownload.php?";

    // TODO If ExternalEval not works, use this in combination with JsLib See documentation for interacting with javascript in unity.
    [DllImport("__Internal")]
    private static extern void DownloadFile(string uri, string filename);
    
    [DllImport("__Internal")] 
    private static extern void Downloaden(byte[] array, int byteLength, string fileName);

    static Uploader NewUploader()
    {
        GameObject go = new GameObject("Uploader");
        return go.AddComponent<Uploader>();
    }

    public static void StartUploadPackage(string filename, string objData, string mtlData, Texture2D[] textures, Action<bool> onDone)
    {
        var u = NewUploader();
        u.StartCoroutine(u.UploadPackage(filename, objData, mtlData, textures, onDone));
    }

    public static void StartUploadScene(string filename, string json, Action<bool> onDone)
    {
        var fn = string.Join("", filename, ".json");
        var u = NewUploader();
        u.StartCoroutine(u.UploadString(fn, json, onDone));
    }

    public static void StartUploadObj(string filename, string objData, Action<bool> onDone)
    {
        var fn = string.Join("", filename, ".obj");
        var u = NewUploader();
        u.StartCoroutine(u.UploadString(fn, objData, onDone));
    }

    public static void StartUploadMtl(string id, string mtlData, Action<bool> onDone)
    {
        var fn = string.Join("", id, ".mtl");
        var u = NewUploader();
        u.StartCoroutine(u.UploadString(fn, mtlData, onDone));
    }

    public static void StartUploadTextures(List<Texture2D> textures, Action<bool> onDone)
    {
        foreach (var tex in textures)
        {
            // Key is unique filename/hash of texture
            var u  = NewUploader();
            u.StartCoroutine(u.UploadTexture(tex, onDone));
        }
    }

    public static void StartDownloadScene(string id, Action<string, bool> onDone)
    {
        var fn = string.Join("", id, ".json");
        var u = NewUploader();
        u.StartCoroutine(u.DownloadString(fn, onDone));
    }

    public static void StartDownloadObj(string id, Action<string, bool> onDone)
    {
        var fn = string.Join("", id, ".obj");
        var u = NewUploader();
        u.StartCoroutine(u.DownloadString(fn, onDone));
    }

    public static void StartDownloadMtl(string id, Action<string, bool> onDone)
    {
        var fn = string.Join("", id, ".mtl");
        var u = NewUploader();
        u.StartCoroutine(u.DownloadString(fn, onDone));
    }

    public static void StartDownloadTexture(string filename, Action<Texture2D, bool> onDone)
    {
        var u = NewUploader();
        u.StartCoroutine(u.DownloadTexture(filename, onDone));
    }

    IEnumerator Upload(byte[] data, string mimeType, string filename, bool downloadAfterUpload, Action<bool> onDone)
    {
        WWWForm form = new WWWForm();
        var guid = Guid.NewGuid().ToString();
        form.AddField("secret", Key);
        form.AddBinaryData("file", data, filename, mimeType);
        
        yield return null;
        UnityWebRequest www = UnityWebRequest.Post(UploadUrl, form);
        yield return www.SendWebRequest();

        bool bSucces = false;
        if (!(www.isNetworkError || www.isHttpError))
        {
            Debug.Log(www.downloadHandler.text);
            string dlUrl = DownloadUrl + "name=" + filename + "&secret=" + Key;
            Debug.Log("Completed Upload " + dlUrl);

            if (downloadAfterUpload)
            {
                // Download it
                yield return new WaitForSeconds(2);// Wait 2 seconds for the file to be placed in the correct location
                dlUrl = "window.open(" + dlUrl + ")";

                // TODO: Replace for possibly more appropriate method
                Application.ExternalEval(dlUrl);
                //  Application.OpenURL(DownloadUrl + randomName);
                //  DownloadFile(DownloadUrl + randomName, randomName);
            }

            bSucces = true;
        }
        else
        {
            Debug.LogWarning("network error: " + www.error + " http error: " + www.isHttpError);
        }

        if (onDone != null) onDone(true);
        if (!bSucces) Debug.Log(www.error);

        Destroy(gameObject);
    }

    IEnumerator UploadPackage(string filename, string objData, string mtlData, Texture2D[] textures, Action<bool> onDone)
    {
        var guid = Guid.NewGuid().ToString();
        byte[] buffer;

        // Render textures (trick to get them to CPU memory).
        Dictionary<string, Texture2D> capturedTextures = TextureRenderer.CopyAndMakeReadable(textures);

        using (MemoryStream zipToOpen = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create, true))
            {
                archive.AddData(filename + ".obj", objData);
                archive.AddData(filename + ".mtl", mtlData);
                if (capturedTextures != null)
                {
                    foreach (var tex in capturedTextures)
                    {
                        try
                        {
                            var bytes = tex.Value.EncodeToPNG();
                            archive.AddData(tex.Key + ".png", bytes);
                        }
                        catch (Exception e) { Debug.LogException(e); }
                    }
                }
            }
            buffer = zipToOpen.ToArray();
        }
        yield return null;
        if (buffer!=null)
        {
            if (!Application.isEditor) Downloaden(buffer, buffer.Length, "3DAmsterdam.zip");
            else
            {
                yield return Upload(buffer, "application/zip", filename + ".zip", true, onDone);
            }
        }
    
    }

    IEnumerator UploadString(string filename, string data, Action<bool> onDone)
    {
        yield return Upload(Encoding.ASCII.GetBytes(data), "application/x-binary", filename, false, onDone);
    }

    IEnumerator UploadTexture(Texture2D tex, Action<bool> onDone)
    {
        if (tex == null) yield break;
        var textures = TextureRenderer.CopyAndMakeReadable2(new Texture[] { tex });
        if (textures == null || textures.Count != 1)
            yield break;

        string filename = MtlExporter.GetTexName(textures[0]) + ".png";
        var bytes = textures[0].EncodeToPNG();
        yield return Upload(bytes, "image/png", filename, false, onDone);
    }

    IEnumerator DownloadString(string filename, Action<string, bool> onDone)
    {
        string url = DownloadUrl + "name=" + filename + "&secret=" + Key;
        var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        string str = "";
        bool bSucces = false;
        if (!(www.isNetworkError || www.isHttpError || www.responseCode!=200))
        {
            str = Encoding.ASCII.GetString(www.downloadHandler.data);
            if ( str != "File does not exist." )
                bSucces = true;
        }
        if (onDone != null) onDone(str, bSucces);
        Destroy(gameObject);
    }

    IEnumerator DownloadTexture(string filename, Action<Texture2D, bool> onDone)
    {
        yield return DownloadTextureRaw(filename, (byte[] data, bool succes) =>
        {
            if (this == null) return;
            Texture2D tex = new Texture2D(2, 2);
            if (!succes)
            {
                if (onDone != null) onDone(tex, succes);
                return;
            }
            succes = false;
            if (tex.LoadImage(data))
            {
                succes = true;
            }
            if (onDone != null) onDone(tex, succes);
            Destroy(gameObject);
        });
    }

    IEnumerator DownloadTextureRaw(string filename, Action<byte[], bool> onDone)
    {
        string url = DownloadUrl + "name=" + filename + "&secret=" + Key;
        var www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        bool succes = false;
        if (!(www.isNetworkError || www.isHttpError || www.downloadHandler.text == "File does not exist."))
        {
            succes = true;
        }
        if (onDone != null) onDone(www.downloadHandler.data, succes);
    }
}
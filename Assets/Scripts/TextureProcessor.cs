using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureProcessor
{
    public static Texture CreateRandomTexture(int sizex, int sizey)
    {
        Texture2D t2d = new Texture2D(sizex, sizey);
        PaintRandomTexture(t2d);
        return t2d as Texture;
    }

    public static Texture PaintRandomTexture(Texture tx)
    {
        Texture2D t2d = tx as Texture2D;

        int t2dsize = t2d.width * t2d.height;
        Color32[] colors = new Color32[t2dsize];
        for (int i = 0; i < t2dsize; i++)
        {
            colors[i] = new Color32(255, 255, 255, (byte)(Random.Range(0, 2) * 255)); // {0, 1}
        }
        t2d.SetPixels32(colors);
        t2d.Apply();

        colors = null;
        return t2d;
    }

    public static Texture PaintTexture(Texture tx, Color32[] pixels)
    {
        Texture2D t2d = tx as Texture2D;
        t2d.SetPixels32(pixels);
        t2d.Apply();
        return t2d;
    }

    public static CustomRenderTexture GetTextureFromObject(MeshRenderer meshRenderer)
    {
        return meshRenderer.material.mainTexture as CustomRenderTexture;
    }

    /// <summary>
    /// If it is used often, call MeshRenderer version
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static CustomRenderTexture GetTextureFromObject(GameObject obj)
    {
        return GetTextureFromObject(obj.GetComponent<MeshRenderer>());
    }

    public static Texture2D CreateTexture2DFromObject(MeshRenderer meshRenderer)
    {
        RenderTexture tex = meshRenderer.material.mainTexture as RenderTexture;
        RenderTexture.active = tex;

        Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex2D.Apply();
        return tex2D;
    }

    public static void GetTexture2DFromObject(MeshRenderer meshRenderer, ref Texture2D tex2D)
    {
        RenderTexture tex = meshRenderer.material.mainTexture as RenderTexture;
        RenderTexture.active = tex;

        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex2D.Apply();
        //return tex2D;
    }

    /// <summary>
    /// If it is used often, call MeshRenderer version
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    //public static Texture2D GetTexture2DFromObject(GameObject obj)
    //{
    //    return GetTexture2DFromObject(obj.GetComponent<MeshRenderer>());
    //}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureProcessor
{
    public static Texture CreateRandomTexture(int sizeX, int sizeY)
    {
        Texture2D t2d = new Texture2D(sizeX, sizeY);

        int t2dsize = t2d.width * t2d.height;
        Color[] colors = new Color[t2dsize];
        for (int i = 0; i < t2dsize; i++)
        {
            colors[i] = new Color(1, 1, 1, Random.Range(0, 2)); // {0, 1}
        }
        t2d.SetPixels(colors);
        t2d.Apply();

        return t2d as Texture;
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

    public static Texture2D GetTexture2DFromObject(MeshRenderer meshRenderer)
    {
        RenderTexture tex = meshRenderer.material.mainTexture as RenderTexture;
        RenderTexture.active = tex;
        Texture2D tex2D = new Texture2D(tex.width, tex.height);
        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex2D.Apply();
        return tex2D;
    }

    /// <summary>
    /// If it is used often, call MeshRenderer version
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static Texture2D GetTexture2DFromObject(GameObject obj)
    {
        return GetTexture2DFromObject(obj.GetComponent<MeshRenderer>());
    }
}

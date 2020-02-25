using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureProcessor
{
    public static Texture CreateRandomTexture(int sizeX, int sizeY)
    {
        Texture2D t2d = new Texture2D(sizeX, sizeY);

        for (int x = 0; x < t2d.width; x++)
        {
            for (int y = 0; y < t2d.height; y++)
            {
                //t2d.SetPixel(x, y, new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 2)));
                t2d.SetPixel(x, y, new Color(1, 1, 1, Random.Range(0, 2))); // alpha = {0, 1}
            }
        }
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

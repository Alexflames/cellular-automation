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
        Debug.Log("memes");
        t2d.Apply();

        return t2d as Texture;
    }
}

using UnityEngine;
using System;

/// <summary>
/// Helper class for picking images from device across different platforms
/// </summary>
public static class ImagePickerHelper
{
    /// <summary>
    /// Pick image from device (cross-platform)
    /// </summary>
    public static void PickImage(Action<Texture2D> onImagePicked, int maxSize = 512)
    {
        #if UNITY_EDITOR
        // Editor: Use file browser
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadImageFromPath(path, onImagePicked, maxSize);
        }
        #elif UNITY_ANDROID || UNITY_IOS
        // Mobile: Use Native Gallery (requires plugin)
        // For now, show a message
        Debug.LogWarning("Image picker: Install NativeGallery plugin for mobile support");
        Debug.LogWarning("Download from: https://github.com/yasirkula/UnityNativeGallery");
        
        // If NativeGallery is installed, uncomment this:
        /*
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize, false);
                if (texture != null)
                {
                    onImagePicked?.Invoke(texture);
                }
            }
        }, "Select an image", "image/*");
        */
        #else
        // WebGL or other platforms
        Debug.LogWarning("Image picker not available on this platform");
        #endif
    }
    
    /// <summary>
    /// Load image from file path
    /// </summary>
    private static void LoadImageFromPath(string path, Action<Texture2D> onImagePicked, int maxSize)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        try
        {
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            
            if (texture.LoadImage(fileData))
            {
                // Resize if needed
                if (texture.width > maxSize || texture.height > maxSize)
                {
                    texture = ResizeTexture(texture, maxSize);
                }
                
                onImagePicked?.Invoke(texture);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load image: {e.Message}");
        }
    }
    
    /// <summary>
    /// Resize texture maintaining aspect ratio
    /// </summary>
    private static Texture2D ResizeTexture(Texture2D source, int maxSize)
    {
        int width = source.width;
        int height = source.height;
        
        if (width > height)
        {
            if (width > maxSize)
            {
                height = (int)(height * ((float)maxSize / width));
                width = maxSize;
            }
        }
        else
        {
            if (height > maxSize)
            {
                width = (int)(width * ((float)maxSize / height));
                height = maxSize;
            }
        }
        
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D resized = new Texture2D(width, height);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        
        return resized;
    }
}


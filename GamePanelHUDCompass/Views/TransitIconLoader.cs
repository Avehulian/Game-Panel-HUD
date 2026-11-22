#if !UNITY_EDITOR

using System.Reflection;
using UnityEngine;

namespace GamePanelHUDCompass.Views
{
    /// <summary>
    /// Loads the transit marker glyph from the embedded resource.
    ///
    /// The compass icon set lives inside gamepanelcompasshud.bundle, which cannot be rebuilt
    /// here, so the PNG ships inside the assembly and is turned into a Sprite on first use.
    /// Source: Dynamic Maps' transit marker (MIT / CC BY 3.0 marker set) - see the notices.
    /// </summary>
    internal static class TransitIconLoader
    {
        private const string ResourceName = "GamePanelHUDCompass.Resources.transit.png";

        private static bool _loaded;
        private static Sprite _icon;

        public static Sprite Icon
        {
            get
            {
                if (_loaded)
                    return _icon;

                _loaded = true;
                _icon = Load();

                return _icon;
            }
        }

        private static Sprite Load()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                        return null;

                    var bytes = new byte[stream.Length];

                    var offset = 0;

                    while (offset < bytes.Length)
                    {
                        var read = stream.Read(bytes, offset, bytes.Length - offset);

                        if (read <= 0)
                            break;

                        offset += read;
                    }

                    if (offset != bytes.Length)
                        return null;

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    if (!texture.LoadImage(bytes))
                        return null;

                    var sprite = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

                    sprite.hideFlags = HideFlags.HideAndDontSave;

                    return sprite;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

#endif

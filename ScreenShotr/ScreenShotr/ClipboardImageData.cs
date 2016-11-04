//----------------------------------------------------------------------------------------------------------------------
// <copyright file="ClipboardImageData.cs" company="Prodrive B.V.">
//     Copyright (c) Prodrive B.V. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------------------------------

namespace ScreenShotr
{
    using System.Drawing;

    /// <summary>
    /// A struct containing information about image files from the clipboard.
    /// </summary>
    public class ClipboardImageData
    {
        /// <summary>
        /// Creates an <see cref="ClipboardImageData"/> class for a raw image.
        /// </summary>
        /// <param name="imageData">The raw image data.</param>
        public static ClipboardImageData Raw(Image imageData)
        {
            return new ClipboardImageData
            {
                ImageData = imageData,
                FileName = string.Empty,
                Type = ImageType.Raw
            };
        }

        /// <summary>
        /// Creates an <see cref="ClipboardImageData"/> class for a file drop.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="imageData">The raw image data.</param>
        public static ClipboardImageData FromFile(string fileName, Image imageData)
        {
            return new ClipboardImageData
            {
                ImageData = imageData,
                FileName = fileName,
                Type = ImageType.FileDrop
            };
        }

        /// <summary>
        /// The raw image data.
        /// </summary>
        public Image ImageData { get; private set; }

        /// <summary>
        /// The type of image from clipboard.
        /// </summary>
        public ImageType Type { get; private set; }

        /// <summary>
        /// The filename from which image data was loaded. Empty if raw data.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The type of image from clipboard.
        /// </summary>
        public enum ImageType
        {
            /// <summary>
            /// Raw image data.
            /// </summary>
            Raw,

            /// <summary>
            /// Image data from a file.
            /// </summary>
            FileDrop
        }
    }
}
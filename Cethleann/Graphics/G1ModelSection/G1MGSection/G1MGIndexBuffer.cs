using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cethleann.Structure.Resource.Model;
using DragonLib;
using JetBrains.Annotations;

namespace Cethleann.Graphics.G1ModelSection.G1MGSection
{
    /// <summary>
    /// </summary>
    [PublicAPI]
    public class G1MGIndexBuffer : IG1MGSection
    {
        internal G1MGIndexBuffer(Span<byte> block, ModelSection subSectionHeader)
        {
            Section = subSectionHeader;

            var offset = 0;
            for (var i = 0; i < subSectionHeader.Count; ++i)
            {
                var info = MemoryMarshal.Read<ModelGeometryIndexBuffer>(block.Slice(offset));
                offset += SizeHelper.SizeOf<ModelGeometryIndexBuffer>();
                var buffer = info.Width switch
                {
                    16 => MemoryMarshal.Cast<byte, ushort>(block.Slice(offset, info.Count * 2)).ToArray(),
                    _ => null
                };
                offset += info.Count * (info.Width / 8);
                offset = offset.Align(4);
                if (buffer == null) continue;
                Buffers.Add((info, buffer));
            }
        }

        /// <summary>
        ///     List of vertex buffer strides
        /// </summary>
        public List<(ModelGeometryIndexBuffer info, ushort[] buffer)> Buffers { get; set; } = new List<(ModelGeometryIndexBuffer info, ushort[] buffer)>();

        /// <inheritdoc />
        public ModelGeometrySectionType Type => ModelGeometrySectionType.IndexBuffer;

        /// <inheritdoc />
        public ModelSection Section { get; }
    }
}

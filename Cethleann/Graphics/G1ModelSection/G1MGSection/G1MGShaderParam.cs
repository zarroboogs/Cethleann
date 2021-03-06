﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cethleann.Structure.Resource.Model;
using DragonLib;
using DragonLib.IO;
using DragonLib.Numerics;
using JetBrains.Annotations;

namespace Cethleann.Graphics.G1ModelSection.G1MGSection
{
    /// <summary>
    ///     Parses shader variable data
    /// </summary>
    /// <inheritdoc />
    [PublicAPI]
    public class G1MGShaderParam : IG1MGSection
    {
        internal G1MGShaderParam(Span<byte> data, ModelSection sectionHeader)
        {
            Section = sectionHeader;

            var offset = 0;
            for (var i = 0; i < sectionHeader.Count; ++i)
            {
                var count = MemoryMarshal.Read<int>(data.Slice(offset));
                offset += sizeof(int);
                var @params = new List<(ModelGeometryShaderParam, string, Array?)>();
                for (var j = 0; j < count; ++j)
                {
                    var blockHeader = MemoryMarshal.Read<ModelGeometryShaderParam>(data.Slice(offset));
                    var localOffset = SizeHelper.SizeOf<ModelGeometryShaderParam>();
                    var name = string.Empty;
                    try
                    {
                        var block = data.Slice(offset + localOffset, blockHeader.Size - localOffset);
                        name = block.ReadStringNonNull();
                        localOffset = (name.Length + 1).Align(4);
                        var paramsBlock = block.Slice(localOffset);
                        var paramData = blockHeader.Type switch
                        {
                            ShaderType.Float32 => MemoryMarshal.Cast<byte, float>(paramsBlock).ToArray(),
                            ShaderType.Matrix4X4X2 => (Array) MemoryMarshal.Cast<byte, Matrix4x4>(paramsBlock).ToArray(),
                            _ => throw new NotSupportedException($"Can't handle ShaderParam {blockHeader.Type:G}")
                        };
                        @params.Add((blockHeader, name, paramData));
                    }
                    catch (Exception e)
                    {
                        Logger.Error("G1MG_SHADER", e);
                        @params.Add((blockHeader, name, null));
                    }
                    finally
                    {
                        offset += blockHeader.Size;
                    }
                }

                ParamGroups.Add(@params);
            }
        }

        /// <summary>
        ///     Two dimensional list of param values.
        ///     I think count usually matches the number of materials.
        /// </summary>
        public List<List<(ModelGeometryShaderParam param, string name, Array? values)>> ParamGroups { get; } = new List<List<(ModelGeometryShaderParam param, string name, Array? values)>>();

        /// <inheritdoc />
        public ModelGeometrySectionType Type => ModelGeometrySectionType.ShaderParam;

        /// <inheritdoc />
        public ModelSection Section { get; }
    }
}

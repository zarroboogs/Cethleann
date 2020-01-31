﻿namespace Cethleann.Structure.Resource.Texture
{
    public struct TextureGroupHeader
    {
        public int TableOffset { get; set; }
        public int EntrySize { get; set; }
        public TextureGroupSystem System { get; set; }
        public int Unknown2 { get; set; }
    }
}
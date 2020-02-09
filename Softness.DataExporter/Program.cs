﻿using System.IO;
using Cethleann;
using Cethleann.ManagedFS;
using Cethleann.Unbundler;
using DragonLib.CLI;
using DragonLib.IO;
using JetBrains.Annotations;

namespace Softness.DataExporter
{
    [PublicAPI]
    public static class Program
    {
        public static SoftnessDataExporterFlags Flags { get; set; } = new SoftnessDataExporterFlags();

        private static void Main(string[] args)
        {
            Logger.PrintVersion("SOFT");
            Flags = CommandLineFlags.ParseFlags<SoftnessDataExporterFlags>(CommandLineFlags.PrintHelp, args);

            using var nyotengu = new Nyotengu(Flags.GameId);
            foreach (var rdb in Directory.GetFiles(Flags.GameDirectory, "*.rdb")) nyotengu.AddDataFS(rdb);
            if (!Flags.NoFilelist) nyotengu.LoadFileList();
            nyotengu.LoadExtList();
            ExtractAll(Flags.OutputDirectory, nyotengu);
        }

        private static void ExtractAll(string romfs, Nyotengu nyo)
        {
            for (var index = 0; index < nyo.EntryCount; index++)
            {
                var data = nyo.ReadEntry(index).Span;
                var pathBase = $@"{romfs}\{nyo.GetFilename(index)}";
                UnbundlerLogic.TryExtractBlob(pathBase, data, false, Flags);
            }
        }
    }
}

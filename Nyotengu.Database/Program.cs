﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cethleann;
using Cethleann.Archive;
using Cethleann.KTID;
using Cethleann.Structure;
using Cethleann.Structure.KTID;
using DragonLib.CLI;
using DragonLib.IO;
using JetBrains.Annotations;

namespace Nyotengu.Database
{
    [PublicAPI]
    public static class Program
    {
        private static void Main(string[] args)
        {
            Logger.PrintVersion("Nyotengu");
            var flags = CommandLineFlags.ParseFlags<DatabaseFlags>(CommandLineFlags.PrintHelp, args);
            if (flags == null) return;

            var files = new HashSet<string>();
            foreach (var path in flags.Paths)
            {
                if (File.Exists(path)) files.Add(path);
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path)) files.Add(file);
            }

            var ndbFiles = new Dictionary<KTIDReference, string>();
            if (!string.IsNullOrWhiteSpace(flags.NDBPath) && Directory.Exists(flags.NDBPath))
                foreach (var nameFile in Directory.GetFiles(flags.NDBPath))
                {
                    ndbFiles[RDB.Hash(Path.GetFileName(nameFile))] = nameFile;
                    if (KTIDReference.TryParse(Path.GetFileNameWithoutExtension(nameFile), NumberStyles.HexNumber, null, out var hashedName)) ndbFiles[hashedName] = nameFile;
                }

            var filelist = Cethleann.ManagedFS.Nyotengu.LoadKTIDFileListShared(flags.FileList, flags.GameId);
            var propertyList = Cethleann.ManagedFS.Nyotengu.LoadKTIDFileList(null, "PropertyNames");
            var filters = flags.TypeInfoFilter?.Split(',').Select(x => RDB.Hash(x.Trim())).ToHashSet() ?? new HashSet<KTIDReference>();

            var typeHashes = new Dictionary<KTIDReference, string>();
            var extraHashes = new Dictionary<KTIDReference, string>();

            foreach (var file in files)
            {
                Logger.Log(ConsoleSwatch.XTermColor.White, true, Console.Error, "Nyotengu", "INFO", file);
                Span<byte> buffer = File.ReadAllBytes(file);
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (buffer.GetDataType())
                {
                    case DataType.OBJDB:
                    {
                        ProcessOBJDB(buffer, ndbFiles, filelist, propertyList, filters, flags);
                        break;
                    }
                    case DataType.NDB:
                    {
                        if (flags.HashTypes || flags.HashExtra)
                            HashNDB(buffer, typeHashes, extraHashes, flags);
                        else
                            ProcessNDB(buffer, flags);
                        break;
                    }
                    default:
                        Logger.Error("Nyotengu", $"Format for {file} is unknown!");
                        break;
                }
            }

            if (flags.HashTypes)
                foreach (var (hash, text) in typeHashes.OrderBy(x => x.Key))
                    Console.WriteLine($"TypeInfo,{hash:x8},{text}");

            // ReSharper disable once InvertIf
            if (flags.HashExtra)
                foreach (var (hash, text) in extraHashes.OrderBy(x => x.Key))
                    Console.WriteLine($"Property,{hash:x8},{text}");
        }

        private static void ProcessOBJDB(Span<byte> buffer, Dictionary<KTIDReference, string> ndbFiles, Dictionary<KTIDReference, string> filelist, Dictionary<KTIDReference, string> propertyList, HashSet<KTIDReference> filters, DatabaseFlags flags)
        {
            var db = new OBJDB(buffer);
            var ndb = new NDB();
            if (ndbFiles.TryGetValue(db.Header.NameKTID, out var ndbPath)) ndb = new NDB(File.ReadAllBytes(ndbPath));

            foreach (var (ktid, (entry, properties)) in db.Entries)
            {
                if (filters.Count != 0 && !filters.Contains(entry.TypeInfoKTID)) continue;
                var lines = new List<string>
                {
                    $"KTID: {GetKTIDNameValue(ktid, flags.ShowKTIDs, ndb, filelist)}",
                    $"TypeInfo: {GetKTIDNameValue(entry.TypeInfoKTID, flags.ShowKTIDs, ndb, filelist)}",
                    $"Parent: {GetKTIDNameValue(entry.ParentKTID, flags.ShowKTIDs, ndb, filelist)}"
                };

                foreach (var (property, values) in properties) lines.Add($"{property.TypeId} {GetKTIDNameValue(property.PropertyKTID, flags.ShowKTIDs, ndb, propertyList)}: {(values.Length == 0 ? "NULL" : string.Join(", ", values.Select(x => property.TypeId == OBJDBPropertyType.UInt32 && x != null ? GetKTIDNameValue((uint) x, flags.ShowKTIDs, ndb, filelist) : x?.ToString() ?? "NULL")))}");

                foreach (var line in lines) Console.Out.WriteLine(line);

                Console.Out.WriteLine();
            }
        }

        private static string GetKTIDNameValue(KTIDReference ktid, bool ignoreNames, NDB ndb, Dictionary<KTIDReference, string> filelist)
        {
            var name = $"{ktid:x8}";
            return ignoreNames ? name : $"{ktid.GetName(ndb, filelist) ?? name}";
        }

        private static void ProcessNDB(Span<byte> buffer, DatabaseFlags flags)
        {
            var name = new NDB(buffer);
            foreach (var (entry, strings) in name.Entries)
            {
                var filename = name.NameMap[entry.KTID];
                var text = $"{entry.KTID:x8},{RDB.Hash(strings[0]):x8},{strings.ElementAt(0)},{filename},{RDB.Hash(strings[1]):x8},{strings[1]}";
                if (strings.Length > 2) text += string.Join(string.Empty, strings.Skip(2));
                Console.WriteLine(text);
            }
        }

        private static void HashNDB(Span<byte> buffer, Dictionary<KTIDReference, string> typeInfo, Dictionary<KTIDReference, string> extra, DatabaseFlags flags)
        {
            var name = new NDB(buffer);
            foreach (var (_, strings) in name.Entries)
            {
                typeInfo[RDB.Hash(strings[1])] = strings[1];
                foreach (var str in strings.Skip(2)) extra[RDB.Hash(str)] = str;
            }
        }
    }
}

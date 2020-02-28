﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cethleann.Archive;
using Cethleann.Structure.KTID;

namespace Cethleann.KTID
{
    /// <summary>
    ///     Wrapper for tuple to provide some extra helpers
    /// </summary>
    public class OBJDBStructure : Tuple<OBJDBRecord, Dictionary<OBJDBProperty, object?[]>>
    {
        /// <summary>
        ///     Initialize
        /// </summary>
        /// <param name="record"></param>
        /// <param name="properties"></param>
        public OBJDBStructure(OBJDBRecord record, Dictionary<OBJDBProperty, object?[]> properties) : base(record, properties)
        {
        }

        /// <summary>
        ///     Proxy for Item1
        /// </summary>
        public OBJDBRecord Record => Item1;

        /// <summary>
        ///     Proxy for Item2
        /// </summary>
        public Dictionary<OBJDBProperty, object?[]> Properties => Item2;

        /// <summary>
        ///     Get property values by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public (OBJDBProperty info, object?[]? values) GetProperty(string name)
        {
            return GetProperty(RDB.Hash(name));
        }

        /// <summary>
        ///     Get property values by KTID
        /// </summary>
        /// <param name="ktid"></param>
        /// <returns></returns>
        public (OBJDBProperty info, object?[]? values) GetProperty(KTIDReference ktid)
        {
            var (key, value) = Properties.FirstOrDefault(x => x.Key.PropertyKTID == ktid);
            return (key, value);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Duplicati.Library.Interface;
using System.IO;

namespace Duplicati.Library.Main.ForestHash.Volumes
{
    public class BlockVolumeReader : VolumeReaderBase
    {
        public BlockVolumeReader(ICompression compression, FhOptions options)
            : base(compression, options)
        {
        }

        public BlockVolumeReader(string compressor, string file, FhOptions options)
            : base(compressor, file, options)
        {
        }

        public int ReadBlock(string hash, byte[] m_blockbuffer)
        {
            using (var fs = m_compression.OpenRead(Utility.Utility.Base64PlainToBase64Url(hash)))
                return Utility.Utility.ForceStreamRead(fs, m_blockbuffer, m_blockbuffer.Length);
        }

        public IEnumerable<string> ReadBlocklist(string hash, long hashsize)
        {
            return new BlocklistEnumerable(m_compression, Utility.Utility.Base64PlainToBase64Url(hash), hashsize);
        }

        //Filenames are encoded with "modified Base64 for URL" https://en.wikipedia.org/wiki/Base64#URL_applications, 
        // to prevent clashes with filename paths where forward slash has special meaning
        private readonly System.Text.RegularExpressions.Regex m_base64_urlsafe_detector = new System.Text.RegularExpressions.Regex("[a-zA-Z0-9-_]+={0,2}");
        public IEnumerable<KeyValuePair<string, long>> Blocks
        {
            get
            {
                return
                    from n in m_compression.ListFilesWithSize(null)
                    let valid = m_base64_urlsafe_detector.Match(n.Key)
                    where valid.Success && valid.Length == n.Key.Length
                    select new KeyValuePair<string, long>(Utility.Utility.Base64UrlToBase64Plain(n.Key), n.Value);
            }
        }
    }
}

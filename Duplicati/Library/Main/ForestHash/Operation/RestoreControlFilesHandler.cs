﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duplicati.Library.Main.ForestHash.Operation
{
    internal class RestoreControlFilesHandler : IDisposable
    {
        private FhOptions m_options;
        private RestoreStatistics m_stat;
        private string m_backendurl;
        private string m_target;

        public RestoreControlFilesHandler(string backendurl, FhOptions options, RestoreStatistics stat, string target)
        {
            m_options = options;
            m_stat = stat;
            m_target = target;
        }

        public void Run()
        {
            using (var tmpdb = new Utility.TempFile())
            using (var db = new Database.Localdatabase(System.IO.File.Exists(m_options.Fhdbpath) ? m_options.Fhdbpath : (string)tmpdb, "RestoreControlFiles"))
            using (var backend = new FhBackend(m_backendurl, m_options, db, m_stat))
            {
                var files = from file in backend.List()
                            let p = Volumes.VolumeBase.ParseFilename(file)
                            where p != null && p.FileType == RemoteVolumeType.Files
                            orderby p.Time
                            select file;

                Exception lastEx = new Exception("No suitable files found on remote target");

                foreach(var file in files)
                    try
                    {
                        long size;
                        string hash;
                        RemoteVolumeType type;
                        RemoteVolumeState state;
                        if (!db.GetRemoteVolume(file.Name, out hash, out size, out type, out state))
                            size = file.Size;

                        using (var tmp = new Volumes.FilesetVolumeReader(RestoreHandler.GetCompressionModule(file.Name), backend.Get(file.Name, size, hash), m_options))
                            foreach (var cf in tmp.ControlFiles)
                                using (var ts = System.IO.File.Create(System.IO.Path.Combine(m_target, cf.Key)))
                                    Utility.Utility.CopyStream(cf.Value, ts);
                        
                        lastEx = null;
                        break;
                    }
                    catch(Exception ex)
                    {
                        lastEx = ex;
                    }

                if (lastEx != null)
                    throw lastEx;
            }
        }

        public void Dispose()
        {
        }
    }
}

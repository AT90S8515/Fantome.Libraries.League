﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fantome.Libraries.League.Helpers.Compression;

namespace Fantome.Libraries.League.IO.RMAN
{
    public class RMANFile
    {
        public byte[] Signature { get; private set; }
        public ulong ReleaseID { get; private set; }
        public List<RMANBundle> Bundles { get; private set; } = new List<RMANBundle>();
        public List<RMANLanguage> Languages { get; private set; } = new List<RMANLanguage>();
        public List<RMANFileEntry> Files { get; private set; } = new List<RMANFileEntry>();
        public List<RMANDirectory> Directories { get; private set; } = new List<RMANDirectory>();
        public List<RMANFileEntry> LocalizedFiles { get; private set; } = new List<RMANFileEntry>();
        public List<RMANDirectory> LocalizedDirectories { get; private set; } = new List<RMANDirectory>();

        public RMANFile(string fileLocation) : this(File.OpenRead(fileLocation)) { }

        public RMANFile(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if(magic != "RMAN")
                {
                    throw new Exception("This is not a valid RMAN file");
                }

                byte major = br.ReadByte();
                byte minor = br.ReadByte();

                if(major != 2 || minor != 0)
                {
                    throw new Exception("This RMAN file is of an unsupported version: " + major + "." + minor);
                }

                //Could possibly be Compression Type
                byte unknown = br.ReadByte();
                byte signatureType = br.ReadByte();

                uint contentOffset = br.ReadUInt32();
                uint compressedContentSize = br.ReadUInt32();
                this.ReleaseID = br.ReadUInt64();
                uint uncompressedContentSize = br.ReadUInt32();

                br.BaseStream.Seek(contentOffset, SeekOrigin.Begin);
                byte[] uncompressedFile = Compression.DecompressZStandard(br.ReadBytes((int)compressedContentSize));
                ReadContent(uncompressedFile);

                if(signatureType != 0)
                {
                    this.Signature = br.ReadBytes(256);
                }
            }
        }

        private void ReadContent(byte[] data)
        {
            //File.WriteAllBytes("content1", data);

            using (BinaryReader br = new BinaryReader(new MemoryStream(data)))
            {
                uint headerOffset = br.ReadUInt32();

                br.BaseStream.Seek(headerOffset, SeekOrigin.Begin);
                uint headerOffsetTableOffset = br.ReadUInt32();

                uint bundlesOffset = (uint)br.BaseStream.Position + br.ReadUInt32();
                uint languagesOffset = (uint)br.BaseStream.Position + br.ReadUInt32();
                uint filesOffset = (uint)br.BaseStream.Position + br.ReadUInt32();
                uint directoriesOffset = (uint)br.BaseStream.Position + br.ReadUInt32();
                uint keyHeaderOffset = (uint)br.BaseStream.Position + br.ReadUInt32();
                uint localizedFilesOffset = (uint)br.BaseStream.Position + br.ReadUInt32();

                br.BaseStream.Seek(bundlesOffset, SeekOrigin.Begin);
                uint bundleCount = br.ReadUInt32();
                for(int i = 0; i < bundleCount; i++)
                {
                    uint bundleOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(bundleOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.Bundles.Add(new RMANBundle(br));
                    br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                }

                br.BaseStream.Seek(languagesOffset, SeekOrigin.Begin);
                uint languageCount = br.ReadUInt32();
                for(int i = 0; i < languageCount; i++)
                {
                    uint languageOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(languageOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.Languages.Add(new RMANLanguage(br));
                    br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                }

                br.BaseStream.Seek(filesOffset, SeekOrigin.Begin);
                uint fileCount = br.ReadUInt32();
                for (int i = 0; i < fileCount; i++)
                {
                    uint fileOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(fileOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.Files.Add(new RMANFileEntry(br));
                    br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                }

                br.BaseStream.Seek(directoriesOffset, SeekOrigin.Begin);
                uint directoryCount = br.ReadUInt32();
                for (int i = 0; i < directoryCount; i++)
                {
                    uint directoryOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(directoryOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.Directories.Add(new RMANDirectory(br));
                    br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                }

                br.BaseStream.Seek(keyHeaderOffset, SeekOrigin.Begin);
                uint unknownKeyHeader = br.ReadUInt32();
                if (unknownKeyHeader != 0) throw new Exception("unknownKeyHeader: " + unknownKeyHeader);

                br.BaseStream.Seek(localizedFilesOffset, SeekOrigin.Begin);
                byte[] unknown = br.ReadBytes(44);
                uint localizedFilesCount = br.ReadUInt32();
                for(int i = 0; i < localizedFilesCount; i++)
                {
                    uint localizedFileOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(localizedFileOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.LocalizedFiles.Add(new RMANFileEntry(br));

                    if(i != localizedFilesCount - 1)
                    {
                        br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                    }
                }

                uint localizedDirectoryCount = br.ReadUInt32();
                for(int i = 0; i < localizedDirectoryCount; i++)
                {
                    uint localizedDirectoryOffset = br.ReadUInt32();
                    long returnOffset = br.BaseStream.Position;

                    br.BaseStream.Seek(localizedDirectoryOffset + returnOffset - 4, SeekOrigin.Begin);
                    this.LocalizedDirectories.Add(new RMANDirectory(br));
                    br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
                }
            }
        }
    }
}

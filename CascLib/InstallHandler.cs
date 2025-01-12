﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CASCLib
{
    public class InstallEntry
    {
        public string Name;
        public ulong Hash;
        public MD5Hash MD5;
        public int Size;

        public List<InstallTag> Tags;

        public bool HasTag(string tag) => Tags.Any(t => t.Name == tag);
        public bool HasAllTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                if (!HasTag(tag))
                    return false;
            }
            return true;
        }
    }

    public class InstallTag
    {
        public string Name;
        public short Type;
        public BitArray Bits;

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

    public class InstallHandler
    {
        private List<InstallEntry> InstallData = new List<InstallEntry>();
        private static readonly Jenkins96 Hasher = new Jenkins96();

        public int Count => InstallData.Count;

        public InstallHandler(BinaryReader stream, BackgroundWorkerEx worker)
        {
            worker?.ReportProgress(0, "Loading \"install\"...");

            stream.ReadBytes(2); // IN

            byte b1 = stream.ReadByte();
            byte b2 = stream.ReadByte();
            short numTags = stream.ReadInt16BE();
            int numFiles = stream.ReadInt32BE();

            int numMaskBytes = (numFiles + 7) / 8;

            List<InstallTag> Tags = new List<InstallTag>();

            for (int i = 0; i < numTags; i++)
            {
                InstallTag tag = new InstallTag()
                {
                    Name = stream.ReadCString(),
                    Type = stream.ReadInt16BE()
                };
                byte[] bits = stream.ReadBytes(numMaskBytes);

                for (int j = 0; j < numMaskBytes; j++)
                    bits[j] = (byte)((bits[j] * 0x0202020202 & 0x010884422010) % 1023);

                tag.Bits = new BitArray(bits);

                Tags.Add(tag);
            }

            for (int i = 0; i < numFiles; i++)
            {
                string name = stream.ReadCString();
                InstallEntry entry = new InstallEntry()
                {
                    Name = name,
                    Hash = Hasher.ComputeHash(name),
                    MD5 = stream.Read<MD5Hash>(),
                    Size = stream.ReadInt32BE()
                };
                InstallData.Add(entry);

                entry.Tags = Tags.FindAll(tag => tag.Bits[i]);

                worker?.ReportProgress((int)((i + 1) / (float)numFiles * 100));
            }
        }

        public InstallEntry GetEntry(string name)
        {
            return InstallData.Where(i => i.Name.ToLower() == name.ToLower()).FirstOrDefault();
        }

        public InstallEntry GetEntry(ulong hash)
        {
            return InstallData.Where(i => i.Hash == hash).FirstOrDefault();
        }

        public IEnumerable<InstallEntry> GetEntriesByName(string name)
        {
            return InstallData.Where(i => i.Name.ToLower() == name.ToLower());
        }

        public IEnumerable<InstallEntry> GetEntriesByTag(string tag)
        {
            foreach (var entry in InstallData)
                if (entry.HasTag(tag))
                    yield return entry;
        }

        public IEnumerable<InstallEntry> GetEntriesByTags(params string[] tags)
        {
            foreach (var entry in InstallData)
            {
                if (entry.HasAllTags(tags))
                    yield return entry;
            }
        }

        public IEnumerable<InstallEntry> GetEntries(ulong hash)
        {
            foreach (var entry in InstallData)
                if (entry.Hash == hash)
                    yield return entry;
        }

        public IEnumerable<InstallEntry> GetEntries()
        {
            foreach (var entry in InstallData)
                yield return entry;
        }

        public void Print()
        {
            for (int i = 0; i < InstallData.Count; ++i)
            {
                var data = InstallData[i];

                Logger.WriteLine($"{i:D4}: {data.Hash:X16} {data.MD5.ToHexString()} {data.Name}");
                Logger.WriteLine($"    Tags: {string.Join(",", data.Tags)}");
            }
        }

        public void Clear()
        {
            InstallData.Clear();
            InstallData = null;
        }
    }
}

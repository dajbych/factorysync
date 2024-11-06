using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Dajbych.FactorySync.Core {

    public static class Extensions {

        internal static bool IsZlib(this ZipArchiveEntry entry) {
            using (var stream = entry.Open()) {
                var b1 = stream.ReadByte();
                var b2 = stream.ReadByte();
                if (b1 == 0x78 && (b2 == 0x01 || b2 == 0x5E || b2 == 0x9C || b2 == 0xDA || b2 == 0x20 || b2 == 0x7D || b2 == 0xBB || b2 == 0xF9)) {
                    return true;
                }
                return false;
            }
        }

        internal static byte[] ReadAllBytes(this Stream stream) {
            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        internal static Version ReadFullVersion(this BinaryReader reader) {
            var major = reader.ReadInt16();
            var minor = reader.ReadInt16();
            var build = reader.ReadInt16();
            var rev = reader.ReadInt16();
            return new Version(major, minor, build, rev);
        }

        internal static string ReadString4BL(this BinaryReader reader) {
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        internal static string ReadString1BL(this BinaryReader reader) {
            var length = reader.ReadByte();
            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        internal static string ReadImmutableString(this BinaryReader reader) {
            var b = reader.ReadByte();
            if (b == 0) {
                var length = reader.ReadInt32Optim();
                var bytes = reader.ReadBytes(length);
                _ = reader.ReadByte();
                return Encoding.UTF8.GetString(bytes);
            } else {
                return string.Empty;
            }
        }

        internal static Version ReadShortVersion(this BinaryReader reader) {
            var major = reader.ReadInt16Optim();
            var minor = reader.ReadInt16Optim();
            var build = reader.ReadInt16Optim();
            return new Version(major, minor, build);
        }

        internal static ushort ReadInt16Optim(this BinaryReader reader) {
            var b = reader.ReadByte();
            if (b != 0xFF) {
                return b;
            } else {
                return reader.ReadUInt16();
            }
        }

        internal static int ReadInt32Optim(this BinaryReader reader) {
            var b = reader.ReadByte();
            if (b != 0xFF) {
                return b;
            } else {
                return reader.ReadInt32();
            }
        }

        internal static object ReadTreeNode(this BinaryReader reader) {
            var type = reader.ReadByte();
            _ = reader.ReadBoolean();
            switch (type) {
                case 0:
                    return null;
                case 1:
                    return reader.ReadBoolean();
                case 2:
                    return reader.ReadInt32();
                case 3:
                    return reader.ReadImmutableString();
                case 4: {
                        var items = new List<object>();
                        var count = reader.ReadInt32();
                        for (var i = 0; i < count; i++) {
                            _ = reader.ReadImmutableString();
                            var val = reader.ReadTreeNode();
                            items.Add(val);
                        }
                        return items;
                    }
                case 5: {
                        var res = new Dictionary<string, object>();
                        var count = reader.ReadInt32();
                        for (var i = 0; i < count; i++) {
                            var key = reader.ReadImmutableString();
                            var val = reader.ReadTreeNode();
                            res.Add(key, val);
                        }
                        return res;
                    }
                default:
                    throw new InvalidDataException("invalid data type");
            }
        }

        internal static void ReadUntilEmptySequence(this BinaryReader reader, int count) {
            var i = 0;
            do {
                if (reader.ReadByte() == 0x00) {
                    i++;
                } else {
                    i = 0;
                }
            } while (i < count);
        }

        public static string FileSize(this FileInfo fi) {
            long bytes = fi.Length;
            string[] sizes = new string[] { "Bytes", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0 Bytes";
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            if (i >= sizes.Length) return "Too large";
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        public static bool IsOlderButRevision(this Version a, Version b) {
            if (a.Major < b.Major) {
                return true;
            } else if (a.Major == b.Major) {
                if (a.Minor < b.Minor) {
                    return true;
                } else if (a.Minor == b.Minor) {
                    if (a.Build < b.Build) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string ToGameTime(this TimeSpan time) {
            return (time.Days * 24 + time.Hours).ToString() + ':' + time.Minutes.ToString("00") + ':' + time.Seconds.ToString("00");
        }

        public static TimeSpan ToGameTime(this string str) {
            var parts = str.Split(':');
            if (parts.Length != 3) throw new ArgumentException("Expected HH:mm:ss", nameof(str));
            var days = int.Parse(parts[0]) / 24;
            var hours = int.Parse(parts[0]) % 24;
            var minutes = int.Parse(parts[1]);
            var seconds = int.Parse(parts[2]);
            return new TimeSpan(days, hours, minutes, seconds);
        }

        public static string ToGameVersion(this Version ver) => $"{ver.Major}.{ver.Minor}.{ver.Build}-{ver.Revision}";

    }

}
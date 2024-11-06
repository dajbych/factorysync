using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Dajbych.FactorySync.Core {

    /// <summary>
    /// Factorio save game binary parser.
    /// </summary>
    /// <seealso cref="https://github.com/flameSla/factorio-blueprint-decoder/blob/win-without-bash-scripts/get_ticks_from_savefile.py"/>
    /// <seealso cref="https://github.com/circlesabound/factorio-file-parser/blob/master/src/schema.rs"/>
    public class FactorioSaveGame {

        public static FactorioSaveGame Open(string filepath) {
            using (var archive = ZipFile.OpenRead(filepath)) {
                return Open(archive);
            }
        }

        public static FactorioSaveGame Open(ZipArchive archive) {
            var level = archive.Entries.Where(e => e.Name == "level.dat0").SingleOrDefault() ?? archive.Entries.Where(e => e.Name == "level.dat").SingleOrDefault() ?? throw new FactorioUnsupportedException("level.dat not found");
            if (level.IsZlib()) {
                using (var zlibStream = level.Open()) {
                    using (var levelStream = new GZipStream(zlibStream, CompressionMode.Decompress)) {
                        return new FactorioSaveGame(levelStream);
                    }
                }
            } else {
                using (var levelStream = level.Open()) {
                    return new FactorioSaveGame(levelStream);
                }
            }
        }

        public FactorioSaveGame(Stream levelStream) {
            //File.WriteAllBytes("game00.bin", levelStream.ReadAllBytes());

            var reader = new BinaryReader(levelStream);

            var version = Version = reader.ReadFullVersion();
            if (version.Major < 2) throw new FactorioParsingException(version, "Unexpected game version");

            var qualityVer = reader.ReadByte();
            var campaign = reader.ReadString1BL();
            var scenario = Scenario = reader.ReadString1BL();
            var baseMod = version.Major >= 2 ? reader.ReadString1BL() : "base";
            if (baseMod != "base") throw new FactorioParsingException(version, "Unexpected base mod");

            var difficulty = reader.ReadByte();
            var finished = reader.ReadBoolean();
            var won = reader.ReadBoolean();
            var nextLevel = reader.ReadString1BL();
            var canContinue = reader.ReadBoolean();
            var finished_but_continuing = reader.ReadBoolean();
            var saving_replay = reader.ReadBoolean();
            var allow_non_admin_debug_options = reader.ReadBoolean();
            var loaded_from = reader.ReadShortVersion();
            var loaded_from_build = reader.ReadUInt32();
            var allowed_commands = reader.ReadByte();

            _ = reader.ReadBytes(4);

            var modsCount = reader.ReadInt32Optim();
            var mods = new List<(string, Version)>();
            for (var i = 0; i < modsCount; i++) {
                var mod = reader.ReadString1BL();
                var ver = reader.ReadShortVersion();
                _ = reader.ReadUInt32();
                mods.Add((mod, ver));
            }

            _ = reader.ReadInt32();

            var tree = reader.ReadTreeNode();

            reader.ReadUntilEmptySequence(4);
            var defeat = reader.ReadString1BL();
            if (defeat != "__core__/graphics/defeat.png") throw new FactorioParsingException(version, "Unexpected game version");
            _ = reader.ReadByte();

            var updateTick = version.Major >= 2 ? reader.ReadUInt64() : reader.ReadUInt32();
            var entityTick = version.Major >= 2 ? reader.ReadUInt64() : reader.ReadUInt32();
            var ticksPlayed = version.Major >= 2 ? reader.ReadUInt64() : reader.ReadUInt32();

            if (updateTick != entityTick || entityTick != ticksPlayed || ticksPlayed != updateTick) throw new InvalidDataException("The game is in a desync state");

            GameTime = TimeSpan.FromSeconds(ticksPlayed / 60);
        }

        public Version Version { get; }

        public string Scenario { get; }

        public TimeSpan GameTime { get; }

    }
}
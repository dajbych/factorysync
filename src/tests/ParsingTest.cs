using Dajbych.FactorySync.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IniParser;
using System;
using System.IO;

namespace Dajbych.FactorySync.Tests {

    [TestClass]
    public class ParsingTest {

        [TestMethod]
        public void ExtractFromBinary() {
            var parser = new FileIniDataParser();
            var config = parser.ReadFile("Test.ini");
            foreach (var file in Directory.GetFiles("saves")) {
                var name = Path.GetFileNameWithoutExtension(file);
                using (var stream = File.OpenRead(file)) {
                    var save = new FactorioSaveGame(stream);
                    Assert.AreEqual(config[name]["version"], save.Version.ToGameVersion());
                    Assert.AreEqual(config[name]["playtime"].ToGameTime(), save.GameTime);
                }
            }
        }

        [TestMethod]
        public void FindTimeInBinary() {
            var parser = new FileIniDataParser();
            var config = parser.ReadFile("Test.ini");
            foreach (var section in config.Sections) {
                var file = Path.Combine("saves", section.SectionName + ".bin");
                var bytes = File.ReadAllBytes(file);
                var playtime = section.Keys["playtime"];
                var expected = playtime.ToGameTime();
                if (!FindDataInBinary(expected, bytes)) {
                    Assert.Fail($"provided playtime {playtime} was not found in {section.SectionName}.bin");
                }
            }
        }

        private bool FindDataInBinary(TimeSpan value, byte[] bytes) {
            for (var i = 0; i < bytes.Length - 8; i++) {
                var dur = BitConverter.ToUInt64(bytes, i);
                var seconds = dur / 60;
                if (seconds < TimeSpan.MaxValue.TotalSeconds) {
                    var actual = TimeSpan.FromSeconds(seconds);
                    if (value == actual) {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
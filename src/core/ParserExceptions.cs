using System;

namespace Dajbych.FactorySync.Core {

    public class FactorioUnsupportedException : Exception {

        internal FactorioUnsupportedException(string message) : base(message) { }

    }

    public class FactorioParsingException : FactorioUnsupportedException {

        internal FactorioParsingException(Version version, string message) : base(message) {
            Version = version;
        }

        public Version Version { get; }

    }

}
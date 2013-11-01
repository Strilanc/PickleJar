using System;
using System.Collections.Generic;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class SequencedJar<T> : IJar<IReadOnlyList<T>> {
        private readonly IJar<T>[] _jars;

        public bool CanBeFollowed { get; private set; }

        public SequencedJar(IEnumerable<IJar<T>> jars) {
            if (jars == null) throw new ArgumentNullException("jars");
            this._jars = jars.ToArray();

            if (_jars.Take(_jars.Length - 1).Any(e => !e.CanBeFollowed)) throw new ArgumentException("!jars.SkipLast(1).Any(jar => !jar.CanBeFollowed)");
            if (_jars.Any(jar => jar == null)) throw new ArgumentException("!jars.Any(jar => jar == null)");

            this.CanBeFollowed = _jars.Length == 0 || _jars.Last().CanBeFollowed;
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var result = new List<T>(capacity: _jars.Length);
            var consumed = 0;
            foreach (var jar in _jars) {
                var p = jar.Parse(data.Skip(consumed));
                result.Add(p.Value);
                consumed += p.Consumed;
            }
            return result.AsReadOnly().AsParsed<IReadOnlyList<T>>(consumed);
        }
        public byte[] Pack(IReadOnlyList<T> value) {
            if (value.Count != _jars.Length) throw new ArgumentException("value.Count != jars.Count");
            return _jars.Zip(value, (jar, item) => jar.Pack(item)).SelectMany(e => e).ToArray();
        }

        public override string ToString() {
            return string.Format(
                "[{0}].ToListJar()",
                string.Join(", ", _jars.AsEnumerable()));
        }
    }
}

/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.TestUtilities
{
    public class Verifier : IVerifier
    {
        public Verifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        public Verifier(ImmutableStack<string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected ImmutableStack<string> Context { get; }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.That(collection, Is.Empty, CreateMessage($"Expected '{collectionName}' to be empty, contains '{collection?.Count()}' elements"));
        }

        public virtual void Equal<T>(T expected, T actual, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.That(actual, Is.EqualTo(expected));
            }
            else
            {
                Assert.That(actual, Is.EqualTo(expected), CreateMessage(message!));
            }
        }

        public virtual void True([DoesNotReturnIf(false)] bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.That(assert);
            }
            else
            {
                Assert.That(assert, CreateMessage(message!));
            }
        }

        public virtual void False([DoesNotReturnIf(true)] bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.That(assert, Is.False);
            }
            else
            {
                Assert.That(assert, Is.False, CreateMessage(message!));
            }
        }

        [DoesNotReturn]
        public virtual void Fail(string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.Fail();
            }
            else
            {
                Assert.Fail(CreateMessage(message!));
            }

            throw new InvalidOperationException("This program location is thought to be unreachable.");
        }

        public virtual void LanguageIsSupported(string language)
        {
            Assert.That(language != LanguageNames.CSharp && language != LanguageNames.VisualBasic, Is.False, CreateMessage($"Unsupported Language: '{language}'"));
        }

        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.That(collection, Is.Not.Empty, CreateMessage($"expected '{collectionName}' to be non-empty, contains"));
        }

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                Assert.Fail(CreateMessage(message!));
            }
        }

        public virtual IVerifier PushContext(string context)
        {
            Assert.That(GetType(), Is.EqualTo(typeof(Verifier)));
            return new Verifier(Context.Push(context));
        }

        protected virtual string CreateMessage(string? message)
        {
            foreach (var frame in Context)
            {
                message = "Context: " + frame + Environment.NewLine + message;
            }

            return message ?? string.Empty;
        }

        private sealed class SequenceEqualEnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            private readonly IEqualityComparer<T> _itemEqualityComparer;

            public SequenceEqualEnumerableEqualityComparer(IEqualityComparer<T>? itemEqualityComparer)
            {
                _itemEqualityComparer = itemEqualityComparer ?? EqualityComparer<T>.Default;
            }

            public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
            {
                if (ReferenceEquals(x, y)) { return true; }
                if (x is null || y is null) { return false; }

                return x.SequenceEqual(y, _itemEqualityComparer);
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // From System.Tuple
                //
                // The suppression is required due to an invalid contract in IEqualityComparer<T>
                // https://github.com/dotnet/runtime/issues/30998
                return obj
                    .Select(item => _itemEqualityComparer.GetHashCode(item!))
                    .Aggregate(
                        0,
                        (aggHash, nextHash) => (aggHash << 5) + aggHash ^ nextHash);
            }
        }
    }
}

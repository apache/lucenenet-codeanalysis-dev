/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev6xxx
{
    /// <summary>
    /// Sample code demonstrating LuceneDev6002 analyzer rules for Span types.
    /// Rule: Span types should not use StringComparison.Ordinal (redundant)
    ///       and must only use Ordinal or OrdinalIgnoreCase.
    /// </summary>
    public class LuceneDev6002_SpanComparisonSample
    {
        // public void BadExamples_RedundantOrdinal()
        // {
        //     ReadOnlySpan<char> span = "Hello World".AsSpan();

        //     // Redundant StringComparison.Ordinal
        //     int index1 = span.IndexOf("Hello".AsSpan(), StringComparison.Ordinal);
        //     int index2 = span.LastIndexOf("World".AsSpan(), StringComparison.Ordinal);
        //     bool starts = span.StartsWith("Hello".AsSpan(), StringComparison.Ordinal);
        //     bool ends = span.EndsWith("World".AsSpan(), StringComparison.Ordinal);
        // }

        // public void BadExamples_InvalidComparison()
        // {
        //     ReadOnlySpan<char> span = "Hello World".AsSpan();

        //     // Culture-sensitive comparisons are not allowed on Span types
        //     int index1 = span.IndexOf("Hello", StringComparison.CurrentCulture);
        //     int index2 = span.LastIndexOf("World", StringComparison.CurrentCultureIgnoreCase);
        //     bool starts = span.StartsWith("Hello", StringComparison.InvariantCulture);
        //     bool ends = span.EndsWith("World", StringComparison.InvariantCultureIgnoreCase);
        // }

        public void GoodExamples_NoStringComparison()
        {
            ReadOnlySpan<char> span = "Hello World".AsSpan();

            // Correct: defaults to Ordinal
            int index1 = span.IndexOf("Hello".AsSpan());
            int index2 = span.LastIndexOf("World".AsSpan());
            bool starts = span.StartsWith("Hello".AsSpan());
            bool ends = span.EndsWith("World".AsSpan());

            // Single char operations
            int charIndex = span.IndexOf('H');
            bool startsWithChar = span[0] == 'H';
        }

        public void GoodExamples_WithOrdinalIgnoreCase()
        {
            ReadOnlySpan<char> span = "Hello World".AsSpan();

            // Correct: case-insensitive search
            int index = span.IndexOf("hello", StringComparison.OrdinalIgnoreCase);
            int lastIndex = span.LastIndexOf("WORLD", StringComparison.OrdinalIgnoreCase);
            bool starts = span.StartsWith("HELLO", StringComparison.OrdinalIgnoreCase);
            bool ends = span.EndsWith("world", StringComparison.OrdinalIgnoreCase);
        }

        public void RealWorldExamples()
        {
            string path = @"C:\Users\Documents\file.txt";
            ReadOnlySpan<char> pathSpan = path.AsSpan();

            // Correct: OrdinalIgnoreCase allowed
            bool isTxtFile = pathSpan.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);

            // Correct: No StringComparison needed
            ReadOnlySpan<char> url = "https://example.com".AsSpan();
            bool isHttps = url.StartsWith("https://");

            ReadOnlySpan<char> token = "Bearer:abc123".AsSpan();
            int separatorIndex = token.IndexOf(':');
        }

        public void StringTypeComparison()
        {
            // Analyzer applies only to Span types
            string text = "Hello World";

            // String types require StringComparison
            int index = text.IndexOf("Hello", StringComparison.Ordinal);

            // Span types should not specify Ordinal
            ReadOnlySpan<char> span = text.AsSpan();
            int index2 = span.IndexOf("Hello");
        }
    }
}

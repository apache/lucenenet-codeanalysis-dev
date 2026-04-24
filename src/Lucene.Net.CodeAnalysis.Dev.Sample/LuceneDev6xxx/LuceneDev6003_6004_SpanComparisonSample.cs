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

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev6xxx;

public class LuceneDev6003_6004_SpanComparisonSample
{
    public void MyMethod()
    {
        ReadOnlySpan<char> span = "Hello World".AsSpan();

        // Redundant StringComparison.Ordinal on span: triggers LuceneDev6003 (Warning).
        int index1 = span.IndexOf("Hello".AsSpan(), StringComparison.Ordinal);
        int lastIndex1 = span.LastIndexOf("World".AsSpan(), StringComparison.Ordinal);
        bool starts1 = span.StartsWith("Hello".AsSpan(), StringComparison.Ordinal);
        bool ends1 = span.EndsWith("World".AsSpan(), StringComparison.Ordinal);

        // Invalid comparison on span: triggers LuceneDev6004 (Error).
        int index2 = span.IndexOf("Hello".AsSpan(), StringComparison.CurrentCulture);
        int lastIndex2 = span.LastIndexOf("World".AsSpan(), StringComparison.CurrentCultureIgnoreCase);
        bool starts2 = span.StartsWith("Hello".AsSpan(), StringComparison.InvariantCulture);
        bool ends2 = span.EndsWith("World".AsSpan(), StringComparison.InvariantCultureIgnoreCase);
    }
}

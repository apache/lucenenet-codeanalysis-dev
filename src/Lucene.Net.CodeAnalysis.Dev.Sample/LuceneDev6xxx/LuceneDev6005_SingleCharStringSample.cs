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

public class LuceneDev6005_SingleCharStringSample
{
    public void MyMethod()
    {
        string text = "Hello World";

        // Single-character string literal: triggers LuceneDev6003 (Info).
        int index1 = text.IndexOf("H", StringComparison.Ordinal);
        int lastIndex1 = text.LastIndexOf("d", StringComparison.Ordinal);
        bool starts1 = text.StartsWith("H", StringComparison.Ordinal);
        bool ends1 = text.EndsWith("d", StringComparison.Ordinal);

        // Escaped single-character string literal: also triggers LuceneDev6003.
        int newlineIndex = text.IndexOf("\n", StringComparison.Ordinal);

        // IndexOf/LastIndexOf have a char overload on ReadOnlySpan<char>: triggers LuceneDev6003.
        ReadOnlySpan<char> span = text.AsSpan();
        int index2 = span.IndexOf("H");
        int lastIndex2 = span.LastIndexOf("d");
    }
}

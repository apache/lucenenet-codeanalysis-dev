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

namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev6001_6002_StringComparisonSample
{
    public void MyMethod()
    {
        string text = "Hello World";

        // Missing StringComparison argument: triggers LuceneDev6001 (Error).
        int index1 = text.IndexOf("Hello");
        bool starts1 = text.StartsWith("Hello");
        bool ends1 = text.EndsWith("World");
        int lastIndex1 = text.LastIndexOf("World");

        // Invalid StringComparison value: triggers LuceneDev6002 (Error).
        int index2 = text.IndexOf("Hello", StringComparison.CurrentCulture);
        bool starts2 = text.StartsWith("hello", StringComparison.CurrentCultureIgnoreCase);
        bool ends2 = text.EndsWith("World", StringComparison.InvariantCulture);
        int lastIndex2 = text.LastIndexOf("world", StringComparison.InvariantCultureIgnoreCase);
    }
}

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

namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev6001_StringComparisonSample
{
    // public void BadExample_MissingStringComparison()
    // {
    //     string text = "Hello World";

    //     //Missing StringComparison parameter
    //     int index = text.IndexOf("Hello");
    //     bool starts = text.StartsWith("Hello");
    //     bool ends = text.EndsWith("World");
    // }

    public void GoodExample_Ordinal()
    {
        string text = "Hello World";

        //Correct usage with StringComparison.Ordinal
        int index = text.IndexOf("Hello", System.StringComparison.Ordinal);
        bool starts = text.StartsWith("Hello", System.StringComparison.Ordinal);
        bool ends = text.EndsWith("World", System.StringComparison.Ordinal);
    }

    public void GoodExample_OrdinalIgnoreCase()
    {
        string text = "Hello World";

        // Correct usage with StringComparison.OrdinalIgnoreCase
        int index = text.IndexOf("hello", System.StringComparison.OrdinalIgnoreCase);
        bool starts = text.StartsWith("HELLO", System.StringComparison.OrdinalIgnoreCase);
        bool ends = text.EndsWith("world", System.StringComparison.OrdinalIgnoreCase);
    }
}

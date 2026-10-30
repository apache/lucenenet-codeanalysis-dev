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

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev4xxx
{
    public enum LuceneDev4003_LegacyDocValuesType
    {
        NONE,
        BYTES_FIXED_DEREF,
        DEFAULT = NONE
    }

    public class LuceneDev4003_EnumMemberToStringSample
    {
        // Triggers LuceneDev4003 (Warning): the enum member is known at compile time,
        // so nameof(LuceneDev4003_LegacyDocValuesType.BYTES_FIXED_DEREF) avoids the
        // runtime name lookup.
        public string Direct() => LuceneDev4003_LegacyDocValuesType.BYTES_FIXED_DEREF.ToString();

        // Preferred form; no diagnostic.
        public string Nameof() => nameof(LuceneDev4003_LegacyDocValuesType.BYTES_FIXED_DEREF);

        // No diagnostic: the value of a variable is not known at compile time.
        public string Variable(LuceneDev4003_LegacyDocValuesType type) => type.ToString();

        // No diagnostic: DEFAULT shares its value with NONE, so ToString() may return "NONE".
        public string Alias() => LuceneDev4003_LegacyDocValuesType.DEFAULT.ToString();
    }
}

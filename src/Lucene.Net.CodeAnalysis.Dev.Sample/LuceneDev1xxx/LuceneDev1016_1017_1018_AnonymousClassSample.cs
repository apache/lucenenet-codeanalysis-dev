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

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev1xxx
{
    public class LuceneDev1016_1017_1018_Sample
    {
        // Triggers LuceneDev1016 (Warning): should be private.
        internal sealed class InternalAnonymousClass
        {
        }

        // Triggers LuceneDev1017 (Warning): should be sealed.
        private class UnsealedAnonymousClass
        {
        }

        private sealed class FieldsAnonymousClass
        {
            // Triggers LuceneDev1018 (Warning): instance fields should be private.
            internal int value = 1;

            // No diagnostic: private instance field.
            private int total = 2;

            // No diagnostic: static and const fields are not checked.
            public static readonly FieldsAnonymousClass Default = new FieldsAnonymousClass();
            public const int Max = 10;

            public int Sum() => value + total + Max;
        }
    }
}

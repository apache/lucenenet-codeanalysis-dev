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

using System.Runtime.CompilerServices;
using Lucene.Net.Util;

// Stub mirroring Lucene.Net.Util.StackTraceHelper so the sample compiles in
// isolation. The analyzer matches by full type name.
namespace Lucene.Net.Util
{
    public static class StackTraceHelper
    {
        public static bool DoesStackTraceContainMethod(string methodName) => false;
        public static bool DoesStackTraceContainMethod(string className, string methodName) => false;
    }
}

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev4xxx
{
    public class LuceneDev4002_TargetWithoutNoInlining
    {
        // Triggers LuceneDev4002 (Warning): this method is referenced by the
        // StackTraceHelper.DoesStackTraceContainMethod calls below but is missing
        // [MethodImpl(MethodImplOptions.NoInlining)]. The JIT may inline it out of
        // the stack trace, silently breaking the check.
        public void Merge()
        {
            System.Console.WriteLine(1 + 2);
        }
    }

    public class LuceneDev4002_TargetWithNoInlining
    {
        // No diagnostic: the attribute is already applied.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Merge()
        {
            System.Console.WriteLine(1 + 2);
        }
    }

    public class LuceneDev4002_Caller
    {
        public void Check()
        {
            // Triggers LuceneDev4002 on the referenced method.
            if (StackTraceHelper.DoesStackTraceContainMethod(
                    nameof(LuceneDev4002_TargetWithoutNoInlining),
                    nameof(LuceneDev4002_TargetWithoutNoInlining.Merge)))
            {
            }

            // The single-argument overload also triggers it.
            if (StackTraceHelper.DoesStackTraceContainMethod(
                    nameof(LuceneDev4002_TargetWithoutNoInlining.Merge)))
            {
            }

            // No diagnostic for this target — already has NoInlining.
            if (StackTraceHelper.DoesStackTraceContainMethod(
                    nameof(LuceneDev4002_TargetWithNoInlining),
                    nameof(LuceneDev4002_TargetWithNoInlining.Merge)))
            {
            }
        }
    }
}

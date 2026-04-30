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

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev4xxx;

public interface ILuceneDev4000Sample
{
    // Triggers LuceneDev4000 (Warning): MethodImpl is not inherited, so NoInlining
    // on an interface member has no effect on the implementation.
    [MethodImpl(MethodImplOptions.NoInlining)]
    void DoWork();
}

public abstract class LuceneDev4000Sample
{
    // Triggers LuceneDev4000 (Warning): same reason — abstract methods are not
    // bodies that the JIT can inline.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public abstract void DoWork();
}

public class LuceneDev4001Sample
{
    // Triggers LuceneDev4001 (Warning): empty-bodied methods cannot appear above
    // any frame in a stack trace, so preventing inlining provides no benefit and
    // only harms performance.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EmptyMethod()
    {
    }

    // No diagnostic: regular method with a non-empty body.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int RealWork()
    {
        return 42;
    }
}

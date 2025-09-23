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

using Microsoft.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    internal static class FloatingPoint
    {
        public static bool IsFloatingPointType(SymbolInfo symbolInfo)
        {
            if (symbolInfo.Symbol is Microsoft.CodeAnalysis.IFieldSymbol fieldInfo)
                return IsSpecialTypeFloatingPoint(fieldInfo.Type.SpecialType);
            if (symbolInfo.Symbol is Microsoft.CodeAnalysis.ILocalSymbol localInfo)
                return IsSpecialTypeFloatingPoint(localInfo.Type.SpecialType);
            if (symbolInfo.Symbol is Microsoft.CodeAnalysis.IPropertySymbol propertyInfo)
                return IsSpecialTypeFloatingPoint(propertyInfo.Type.SpecialType);
            if (symbolInfo.Symbol is Microsoft.CodeAnalysis.IParameterSymbol parameterInfo)
                return IsSpecialTypeFloatingPoint(parameterInfo.Type.SpecialType);
            //if (symbolInfo.Symbol is Microsoft.CodeAnalysis.ITypeParameterSymbol typeParameterInfo)
            //    return IsSpecialTypeFloatingPoint(typeParameterInfo.Type.SpecialType);
            //if (symbolInfo.Symbol.Name == "Equals")
            return false;
        }

        private static bool IsSpecialTypeFloatingPoint(SpecialType specialType)
        {
            return specialType == SpecialType.System_Single || specialType == SpecialType.System_Double;
        }
    }
}

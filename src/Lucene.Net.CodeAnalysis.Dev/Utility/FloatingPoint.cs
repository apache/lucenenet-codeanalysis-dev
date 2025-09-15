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

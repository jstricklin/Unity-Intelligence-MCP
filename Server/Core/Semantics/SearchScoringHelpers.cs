using System;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public static class SearchScoringHelpers
    {
        public static double CalculateTermFrequency(string text, string term)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term)) 
                return 0;

            int termCount = 0;
            int pos = 0;
            while ((pos = text.IndexOf(term, pos, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                termCount++;
                pos += term.Length;
            }
            
            return termCount / (double)text.Length;
        }
    }
}

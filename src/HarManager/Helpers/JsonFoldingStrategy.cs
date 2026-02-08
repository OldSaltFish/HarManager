using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace HarManager.Helpers
{
    public class JsonFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var newFoldings = CreateNewFoldings(document);
            manager.UpdateFoldings(newFoldings, -1);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var startOffsets = new Stack<int>();
            
            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == '{' || c == '[')
                {
                    startOffsets.Push(i);
                }
                else if ((c == '}' || c == ']') && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // Check if matching
                    char startChar = document.GetCharAt(startOffset);
                    if ((startChar == '{' && c == '}') || (startChar == '[' && c == ']'))
                    {
                        // Ensure multiline
                        var startLine = document.GetLineByOffset(startOffset);
                        var endLine = document.GetLineByOffset(i);
                        if (startLine.LineNumber != endLine.LineNumber)
                        {
                            newFoldings.Add(new NewFolding(startOffset, i + 1));
                        }
                    }
                }
            }
            
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}


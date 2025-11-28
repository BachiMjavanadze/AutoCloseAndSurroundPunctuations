#nullable enable
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoSurround;

[Export(typeof(ICommandHandler))]
[Name(nameof(AutoSurroundCommandHandler))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
public class AutoSurroundCommandHandler :
    ICommandHandler<TypeCharCommandArgs>,
    ICommandHandler<ReturnKeyCommandArgs>
{
    private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly LanguageConfiguration _configuration;

    [ImportingConstructor]
    public AutoSurroundCommandHandler(
        ITextUndoHistoryRegistry textUndoHistoryRegistry,
        ITextDocumentFactoryService textDocumentFactoryService,
        LanguageConfiguration configuration
    ) {
        _textUndoHistoryRegistry = textUndoHistoryRegistry;
        _textDocumentFactoryService = textDocumentFactoryService;
        _configuration = configuration;
    }

    public string DisplayName => nameof(AutoSurroundCommandHandler);

    public CommandState GetCommandState(TypeCharCommandArgs args) {
        if (_configuration.IsPossiblyOpeningChar(args.TypedChar)) {
            return CommandState.Available;
        } else {
            return CommandState.Unavailable;
        }
    }

    public CommandState GetCommandState(ReturnKeyCommandArgs args) {
        return CommandState.Unspecified;
    }


    public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext) {
        if (args.TypedChar == '<') {
            if (!args.TextView.Selection.IsEmpty) {
                // If text is selected and the typed character is '<', surround with '<' and '>'
                if (SurroundWith('<', '>', args.TextView)) {
                    return true;
                }
            } else {
                // If no text is selected and the typed character is '<', just insert '<'
                ITextUndoHistory history = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
                using (ITextUndoTransaction transaction = history.CreateTransaction($"<Insert")) {
                    int position = args.TextView.Caret.Position.BufferPosition.Position;
                    args.TextView.TextBuffer.Insert(position, "<");
                    transaction.Complete();
                }
                return true;
            }
        } else if (args.TypedChar == '"' && args.TextView.Selection.IsEmpty) {
            // Handle the double quote character when no text is selected
            int position = args.TextView.Caret.Position.BufferPosition.Position;
            ITextSnapshot snapshot = args.TextView.TextBuffer.CurrentSnapshot;

            // Get the previous and next characters
            char prevChar = position > 0 ? snapshot[position - 1] : '\0';
            char nextChar = position < snapshot.Length ? snapshot[position] : '\0';

            // Define the conditions under which only a single quote should be inserted
            bool doubleQuoteCondition =
                char.IsLetterOrDigit(nextChar) ||
                char.IsLetterOrDigit(prevChar) ||
                nextChar == '"' ||
                prevChar == '"' ||
                nextChar == '{' ||
                prevChar == '}' ||
                nextChar == '(' ||
                prevChar == ')';

            if (doubleQuoteCondition) {
                // Just insert a single quote
                ITextUndoHistory subHistory = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
                using (ITextUndoTransaction transaction = subHistory.CreateTransaction($"Insert \"")) {
                    args.TextView.TextBuffer.Insert(position, "\"");
                    transaction.Complete();
                }
                return true;
            }

            // If no text is selected and conditions do not match, handle insertion of double quotes
            ITextUndoHistory history = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
            using (ITextUndoTransaction transaction = history.CreateTransaction($"Insert \"")) {
                args.TextView.TextBuffer.Insert(position, "\"\"");
                // Move the caret between the double quotes
                SnapshotPoint caretPosition = new SnapshotPoint(args.TextView.TextBuffer.CurrentSnapshot, position + 1);
                args.TextView.Caret.MoveTo(caretPosition);
                transaction.Complete();
            }
            return true;
        } else if (args.TypedChar == '\'' && args.TextView.Selection.IsEmpty) {
            // Handle the single quote character when no text is selected
            int position = args.TextView.Caret.Position.BufferPosition.Position;
            ITextSnapshot snapshot = args.TextView.TextBuffer.CurrentSnapshot;

            // Get the previous and next characters
            char prevChar = position > 0 ? snapshot[position - 1] : '\0';
            char nextChar = position < snapshot.Length ? snapshot[position] : '\0';

            // Define the conditions under which only a single quote should be inserted
            bool singleQuoteCondition =
                char.IsLetterOrDigit(nextChar) ||
                char.IsLetterOrDigit(prevChar) ||
                nextChar == '\'' ||
                prevChar == '\'' ||
                nextChar == '{' ||
                prevChar == '}' ||
                nextChar == '(' ||
                prevChar == ')';

            if (singleQuoteCondition) {
                // Just insert a single quote
                ITextUndoHistory subHistory = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
                using (ITextUndoTransaction transaction = subHistory.CreateTransaction($"Insert '")) {
                    args.TextView.TextBuffer.Insert(position, "'");
                    transaction.Complete();
                }
                return true;
            }

            // If no text is selected and conditions do not match, handle insertion of single quotes
            ITextUndoHistory history = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
            using (ITextUndoTransaction transaction = history.CreateTransaction($"Insert '")) {
                args.TextView.TextBuffer.Insert(position, "''");
                // Move the caret between the single quotes
                SnapshotPoint caretPosition = new SnapshotPoint(args.TextView.TextBuffer.CurrentSnapshot, position + 1);
                args.TextView.Caret.MoveTo(caretPosition);
                transaction.Complete();
            }
            return true;
        } else {
            // For other characters, handle as before
            if (!args.TextView.Selection.IsEmpty) {
                if (_configuration.TryGetClosingChar(GetFileName(args.SubjectBuffer), args.TypedChar, out char closing)) {
                    if (SurroundWith(args.TypedChar, closing, args.TextView)) {
                        return true;
                    }
                }
            } else {
                // If no text is selected, insert the opening and closing characters
                if (_configuration.TryGetClosingChar(GetFileName(args.SubjectBuffer), args.TypedChar, out char closing)) {
                    ITextUndoHistory history = _textUndoHistoryRegistry.GetHistory(args.TextView.TextBuffer);
                    using (ITextUndoTransaction transaction = history.CreateTransaction($"{args.TypedChar}Auto Surround{closing}")) {
                        int position = args.TextView.Caret.Position.BufferPosition.Position;
                        args.TextView.TextBuffer.Insert(position, args.TypedChar.ToString());
                        args.TextView.TextBuffer.Insert(position + 1, closing.ToString());

                        // Move the caret between the opening and closing characters
                        SnapshotPoint caretPosition = new SnapshotPoint(args.TextView.TextBuffer.CurrentSnapshot, position + 1);
                        args.TextView.Caret.MoveTo(caretPosition);

                        transaction.Complete();
                    }
                    return true;
                }
            }
        }

        return false;
    }

    // handle Enter when caret is between an opening/closing pair (e.g. {<>})
    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext) {
        var view = args.TextView;
        var buffer = view.TextBuffer;

        SnapshotPoint caret = view.Caret.Position.BufferPosition;
        ITextSnapshot snapshot = caret.Snapshot;

        // Need a character on both sides of the caret.
        if (caret.Position == 0 || caret.Position >= snapshot.Length) {
            return false;
        }

        char left = snapshot[caret.Position - 1];
        char right = snapshot[caret.Position];

        // Only act when the caret is exactly between a known opening/closing pair.
        if (!_configuration.TryGetClosingChar(GetFileName(args.SubjectBuffer), left, out char closing) ||
            closing != right) {
            return false;
        }

        ITextSnapshotLine line = snapshot.GetLineFromPosition(caret.Position);
        string lineText = line.GetText();
        int lineStart = line.Start.Position;
        int caretInLine = caret.Position - lineStart;

        int openIndex = caretInLine - 1;
        int closeIndex = caretInLine;

        if (openIndex < 0 || closeIndex >= lineText.Length) {
            return false;
        }

        // Leading indent of the line
        int firstNonWs = 0;
        while (firstNonWs < lineText.Length && char.IsWhiteSpace(lineText[firstNonWs])) {
            firstNonWs++;
        }
        string baseIndent = lineText.Substring(0, firstNonWs);

        // Text before '{'
        string before = lineText.Substring(0, openIndex).TrimEnd();

        // Text after '}' (e.g. comments)
        string after = closeIndex + 1 < lineText.Length
            ? lineText.Substring(closeIndex + 1)
            : string.Empty;
        after = after.TrimStart();

        var options = view.Options;
        string newLine = options.GetNewLineCharacter();
        int indentSize = options.GetIndentSize();
        bool useSpaces = options.IsConvertTabsToSpacesEnabled();
        string indentUnit = useSpaces ? new string(' ', indentSize) : "\t";
        string innerIndent = baseIndent + indentUnit;

        var sb = new StringBuilder();
        sb.Append(before);
        sb.Append(newLine);
        sb.Append(baseIndent);
        sb.Append(left);
        sb.Append(newLine);
        sb.Append(innerIndent);
        int caretOffsetInReplacement = sb.Length; // caret after inner indent
        sb.Append(newLine);
        sb.Append(baseIndent);
        sb.Append(right);
        if (!string.IsNullOrEmpty(after)) {
            sb.Append(' ');
            sb.Append(after);
        }

        string replacement = sb.ToString();

        ITextUndoHistory history = _textUndoHistoryRegistry.GetHistory(buffer);
        using (ITextUndoTransaction transaction =
               history.CreateTransaction("AutoSurround newline inside brackets")) {
            buffer.Replace(line.Extent, replacement);

            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            SnapshotPoint newCaret = new SnapshotPoint(newSnapshot, lineStart + caretOffsetInReplacement);
            view.Caret.MoveTo(newCaret);

            transaction.Complete();
        }

        return true;
    }

    private string GetFileName(ITextBuffer buffer) {
        if (_textDocumentFactoryService.TryGetTextDocument(buffer, out var document)) {
            if (!string.IsNullOrEmpty(document.FilePath)) {
                return Path.GetFileName(document.FilePath);
            }
        }

        return "";
    }

    private bool SurroundWith(char opening, char closing, ITextView textView) {
        List<(int Position, char Character)> edits;
        ITextUndoHistory history;

        edits = textView
            .Selection
            .SelectedSpans
            .Where((x) => !x.IsEmpty)
            .SelectMany((x) => new[] { (x.Start.Position, Character: opening), (x.End.Position, Character: closing) })
            .OrderByDescending((x) => x.Position)
            .ToList();

        if (edits.Count == 0) {
            return false;
        }

        history = _textUndoHistoryRegistry.GetHistory(textView.TextBuffer);

        using (ITextUndoTransaction transaction = history.CreateTransaction($"{opening}Auto Surround{closing}")) {
            foreach ((int Position, char Character) edit in edits) {
                textView.TextBuffer.Insert(edit.Position, edit.Character.ToString());
            }

            textView.GetMultiSelectionBroker().PerformActionOnAllSelections((transformer) => {
                Selection selection = transformer.Selection;

                if (!selection.IsEmpty) {
                    VirtualSnapshotPoint activePoint;
                    VirtualSnapshotPoint anchorPoint;
                    VirtualSnapshotPoint insertionPoint;

                    if (selection.IsReversed) {
                        anchorPoint = new VirtualSnapshotPoint(selection.AnchorPoint.Position - 1);
                        activePoint = new VirtualSnapshotPoint(selection.ActivePoint.Position);

                    } else {
                        anchorPoint = new VirtualSnapshotPoint(selection.AnchorPoint.Position);
                        activePoint = new VirtualSnapshotPoint(selection.ActivePoint.Position - 1);
                    }

                    if (selection.InsertionPoint == selection.AnchorPoint) {
                        insertionPoint = anchorPoint;

                    } else if (selection.InsertionPoint == selection.ActivePoint) {
                        insertionPoint = activePoint;

                    } else {
                        insertionPoint = transformer.Selection.InsertionPoint;
                    }

                    transformer.MoveTo(anchorPoint, activePoint, insertionPoint, selection.InsertionPointAffinity);
                }
            });

            transaction.Complete();
        }

        return true;
    }
}

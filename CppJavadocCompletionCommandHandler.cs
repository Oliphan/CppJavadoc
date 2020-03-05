namespace CppJavadoc
{
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public class CppJavadocCompletionCommandHandler : IOleCommandTarget
    {
        public const string CppTypeName = "C/C++";
        private IOleCommandTarget m_nextCommandHandler;
        private IWpfTextView m_textView;
        private CppJavadocCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        DTE m_dte;

        public CppJavadocCompletionCommandHandler(
            IVsTextView textViewAdapter,
            IWpfTextView textView,
            CppJavadocCompletionHandlerProvider provider,
            DTE dte)
        {
            this.m_textView = textView;
            this.m_provider = provider;
            this.m_dte = dte;

            // add the command to the command chain
            if (textViewAdapter != null &&
                textView != null &&
                textView.TextBuffer != null &&
                textView.TextBuffer.ContentType.TypeName == CppTypeName)
            {
                textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
                {
                    return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                // make a copy of this so we can look at it after forwarding some commands 
                uint commandID = nCmdID;
                char typedChar = char.MinValue;

                // make sure the input is a char before getting it 
                if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                {
                    typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
                }
                if(m_dte != null)
                {
                    string currentLine = m_textView.TextSnapshot.GetLineFromPosition(
                            m_textView.Caret.Position.BufferPosition.Position).GetText();

                    // check for the Javadoc slash and two asterisk pattern while compensating for visual studio's block comment closing generation
                    if (typedChar == '*' && currentLine.Trim() == "/**/")
                    {
                        // Calculate how many spaces
                        string spaces = currentLine.Replace(currentLine.TrimStart(), "");
                        TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                        //Remember where the cursor was when command was triggered
                        int oldLine = ts.ActivePoint.Line;
                        int oldOffset = ts.ActivePoint.LineCharOffset;
                        ts.LineDown();
                        ts.EndOfLine();
                        ts.SelectLine();
                        
                        //Detect and skip over Unreal Engine Function Macros
                        string trimmedFuncLine = ts.Text.Trim();
                        if (trimmedFuncLine.StartsWith("UFUNCTION("))
                        {
                            ts.EndOfLine();
                        }
                        else
                        {
                            ts.MoveToLineAndOffset(oldLine, oldOffset);
                            ts.LineDown();
                            ts.EndOfLine();
                        }

                        CodeElement codeElement = null;
                        FileCodeModel fcm = m_dte.ActiveDocument.ProjectItem.FileCodeModel;
                        if (fcm != null)
                        {
                            codeElement = fcm.CodeElementFromPoint(ts.ActivePoint, vsCMElement.vsCMElementFunction);
                        }
                        
                        if (codeElement != null && codeElement is CodeFunction)
                        {
                            CodeFunction function = codeElement as CodeFunction;
                            StringBuilder sb = new StringBuilder("*");
                            bool isNoArgsNoReturn = true;
                            foreach (CodeElement child in codeElement.Children)
                            {
                                CodeParameter parameter = child as CodeParameter;
                                if (parameter != null)
                                {
                                    sb.AppendFormat("\r\n" + spaces + " * @param {0} ", parameter.Name);
                                    isNoArgsNoReturn = false;
                                }
                            }

                            if (function.Type.AsString != "void")
                            {
                                isNoArgsNoReturn = false;
                                if (function.Type.AsString == "bool")
                                {
                                    sb.AppendFormat("\r\n" + spaces + " * @return true \r\n" + spaces + " * @return false ");
                                }
                                else
                                {
                                    sb.AppendFormat("\r\n" + spaces + " * @return ");
                                }
                            }
                            
                            //If function has a return type or parameters then we generate them and return, otherwise we skip to generate a single line comment
                            if(!isNoArgsNoReturn)
                            {
                                sb.Insert(1, "\r\n" + spaces + " * ");
                                sb.AppendFormat("\r\n" + spaces + " ");
                                ts.MoveToLineAndOffset(oldLine, oldOffset);
                                ts.Insert(sb.ToString());
                                ts.MoveToLineAndOffset(oldLine, oldOffset);
                                ts.LineDown();
                                ts.EndOfLine();
                                return VSConstants.S_OK;
                            }
                        }
                        //For variables and void functions with no parameters we can do a single line comment
                        ts.MoveToLineAndOffset(oldLine, oldOffset);
                        ts.Insert("*  ");
                        ts.MoveToLineAndOffset(oldLine, oldOffset + 2);
                        return VSConstants.S_OK;
                    }
                    else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                    {
                        bool startsWithAsterisk = currentLine.TrimStart().StartsWith("* ");
                        bool startsWithSlashAsterisk = currentLine.TrimStart().StartsWith("/*");
                        if (startsWithAsterisk || startsWithSlashAsterisk)
                        {
                            // Calculate how many spaces
                            string spaces = currentLine.Replace(currentLine.TrimStart(), "");
                            TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                            if (startsWithSlashAsterisk)
                            {
                                //If there is a slash then we need one more space for correct spacing
                                ts.Insert("\r\n" + spaces + " * ");
                            }
                            else
                            {
                                //Otherwise the spacing we saved before is enough
                                ts.Insert("\r\n" + spaces + "* ");
                            }
                            return VSConstants.S_OK;
                        }
                    }
                }
                // pass along the command so the char is added to the buffer
                return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            catch
            {
            }

            return VSConstants.E_FAIL;
        }

        private bool TriggerCompletion()
        {
            try
            {
                if (m_session != null)
                {
                    return false;
                }

                // the caret must be in a non-projection location 
                SnapshotPoint? caretPoint =
                m_textView.Caret.Position.Point.GetPoint(
                    textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
                if (!caretPoint.HasValue)
                {
                    return false;
                }

                m_session = m_provider.CompletionBroker.CreateCompletionSession(
                    m_textView,
                    caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                    true);

                // subscribe to the Dismissed event on the session 
                m_session.Dismissed += this.OnSessionDismissed;
                m_session.Start();
                return true;
            }
            catch
            {
            }

            return false;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            if (m_session != null)
            {
                m_session.Dismissed -= this.OnSessionDismissed;
                m_session = null;
            }
        }
    }
}
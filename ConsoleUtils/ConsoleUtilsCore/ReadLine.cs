using Pastel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class ReadLine
    {
        private static List<string> _history;

        static ReadLine()
        {
            _history = new List<string>();
            RollingComplete = true;
            RollingShowSuggestions = true;
        }

        public static void AddHistory(params string[] text) => _history.AddRange(text);
        public static List<string> GetHistory() => _history;
        public static void ClearHistory() => _history = new List<string>();
        public static bool HistoryEnabled { get; set; }
        public static IAutoCompleteHandler AutoCompletionHandler { private get; set; }
        public static bool RollingComplete { get; set; }
        public static bool RollingShowSuggestions { get; set; }

        public static string Read(string prompt = "", string @default = "")
        {
            Console.Write(prompt);
            KeyHandler keyHandler = new KeyHandler(new Console2(), _history, AutoCompletionHandler);
            keyHandler.RollingAutoComplete = RollingComplete;
            keyHandler.RollingShowSuggestions = RollingShowSuggestions;
            keyHandler.Prompt = prompt;

            string text = GetText(keyHandler);

            if (String.IsNullOrWhiteSpace(text) && !String.IsNullOrWhiteSpace(@default))
            {
                text = @default;
            }
            else
            {
                if (HistoryEnabled)
                    _history.Add(text);
            }

            return text;
        }

        public static string ReadPassword(string prompt = "")
        {
            Console.Write(prompt);
            KeyHandler keyHandler = new KeyHandler(new Console2() { PasswordMode = true }, null, null);
            return GetText(keyHandler);
        }

        public static event Action<ConsoleKey, string> BufferChanged;

        private static string GetText(KeyHandler keyHandler)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            while (keyInfo.Key != ConsoleKey.Enter)
            {
                keyHandler.Handle(keyInfo);
                BufferChanged?.Invoke(keyInfo.Key, keyHandler.Text);
                keyInfo = Console.ReadKey(true);
            }
            keyHandler.Done();
            BufferChanged?.Invoke(keyInfo.Key, keyHandler.Text);
            Console.WriteLine();

            return keyHandler.Text;
        }
    }
    internal class KeyHandler
    {
        private const int AutoCompleteColumns = 5;
        private const int AutoCompleteColumnLength = 15;
        public bool RollingAutoComplete { get; set; } = true;
        public bool RollingShowSuggestions { get; set; } = true;

        public string Prompt { get; set; }

        private int _cursorPos;
        private int _cursorLimit;
        private StringBuilder _text;
        private List<string> _history;
        private int _historyIndex;
        private ConsoleKeyInfo _keyInfo;
        private Dictionary<string, Action> _keyActions;
        private string[] _completions;
        private int _completionStart;
        private int _completionsIndex;
        private IConsole Console2;
        private int _tabCounter = 0;

        private bool IsStartOfLine() => _cursorPos == 0;

        private bool IsEndOfLine() => _cursorPos == _cursorLimit;

        private bool IsStartOfBuffer() => Console2.CursorLeft == 0;

        private bool IsEndOfBuffer() => Console2.CursorLeft == Console2.BufferWidth - 1;
        private bool IsInAutoCompleteMode() => _completions != null;

        private void Return()
        {
            _tabCounter = 0;
        }

        private void MoveCursorLeft()
        {
            MoveCursorLeft(1);
        }

        private void MoveCursorLeft(int count)
        {
            if (count > _cursorPos)
                count = _cursorPos;

            if (count > Console2.CursorLeft)
                Console2.SetCursorPosition(Console2.BufferWidth - 1, Console2.CursorTop - 1);
            else
                Console2.SetCursorPosition(Console2.CursorLeft - count, Console2.CursorTop);

            _cursorPos -= count;
        }

        private void MoveCursorHome()
        {
            while (!IsStartOfLine())
                MoveCursorLeft();
        }

        private string BuildKeyInput()
        {
            return (_keyInfo.Modifiers != ConsoleModifiers.Control && _keyInfo.Modifiers != ConsoleModifiers.Shift) ?
                _keyInfo.Key.ToString() : _keyInfo.Modifiers.ToString() + _keyInfo.Key.ToString();
        }

        private void MoveCursorRight()
        {
            if (IsEndOfLine())
                return;

            if (IsEndOfBuffer())
                Console2.SetCursorPosition(0, Console2.CursorTop + 1);
            else
                Console2.SetCursorPosition(Console2.CursorLeft + 1, Console2.CursorTop);

            _cursorPos++;
        }

        private void MoveCursorEnd()
        {
            while (!IsEndOfLine())
                MoveCursorRight();
        }

        private void ClearLine()
        {
            MoveCursorEnd();
            Backspace(_cursorPos);
        }

        private void WriteNewString(string str)
        {
            ClearLine();
            foreach (char character in str)
                WriteChar(character);
        }

        private void WriteString(string str)
        {
            foreach (char character in str)
                WriteChar(character);
        }

        private void WriteChar() => WriteChar(_keyInfo.KeyChar);

        private void WriteChar(char c)
        {
            if (IsEndOfLine())
            {
                _text.Append(c);
                Console2.Write(c.ToString());
                _cursorPos++;
            }
            else
            {
                int left = Console2.CursorLeft;
                int top = Console2.CursorTop;
                string str = _text.ToString().Substring(_cursorPos);
                _text.Insert(_cursorPos, c);
                Console2.Write(c.ToString() + str);
                Console2.SetCursorPosition(left, top);
                MoveCursorRight();
            }

            _cursorLimit++;
        }

        /*
        private void Backspace()
        {
            if (IsStartOfLine())
                return;

            MoveCursorLeft();
            int index = _cursorPos;
            _text.Remove(index, 1);
            string replacement = _text.ToString().Substring(index);
            int left = Console2.CursorLeft;
            int top = Console2.CursorTop;
            Console2.Write(string.Format("{0} ", replacement));
            Console2.SetCursorPosition(left, top);
            _cursorLimit--;
        }*/
        private void Backspace()
        {
            Backspace(1);
        }

        private void Backspace(int count)
        {
            if (count > _cursorPos)
                count = _cursorPos;

            MoveCursorLeft(count);
            int index = _cursorPos;
            _text.Remove(index, count);
            string replacement = _text.ToString().Substring(index);
            int left = Console2.CursorLeft;
            int top = Console2.CursorTop;
            string spaces = new string(' ', count);
            Console2.Write(string.Format("{0}{1}", replacement, spaces));
            Console2.SetCursorPosition(left, top);
            _cursorLimit -= count;
        }

        private void Delete()
        {
            if (IsEndOfLine())
                return;

            int index = _cursorPos;
            _text.Remove(index, 1);
            string replacement = _text.ToString().Substring(index);
            int left = Console2.CursorLeft;
            int top = Console2.CursorTop;
            Console2.Write(string.Format("{0} ", replacement));
            Console2.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        private void TransposeChars()
        {
            // local helper functions
            bool almostEndOfLine() => (_cursorLimit - _cursorPos) == 1;
            int incrementIf(Func<bool> expression, int index) => expression() ? index + 1 : index;
            int decrementIf(Func<bool> expression, int index) => expression() ? index - 1 : index;

            if (IsStartOfLine()) { return; }

            var firstIdx = decrementIf(IsEndOfLine, _cursorPos - 1);
            var secondIdx = decrementIf(IsEndOfLine, _cursorPos);

            var secondChar = _text[secondIdx];
            _text[secondIdx] = _text[firstIdx];
            _text[firstIdx] = secondChar;

            var left = incrementIf(almostEndOfLine, Console2.CursorLeft);
            var cursorPosition = incrementIf(almostEndOfLine, _cursorPos);

            WriteNewString(_text.ToString());

            Console2.SetCursorPosition(left, Console2.CursorTop);
            _cursorPos = cursorPosition;

            MoveCursorRight();
        }


        private void StartAutoComplete()
        {
            _completionsIndex = 0;
            if (RollingShowSuggestions)
                ShowSuggestions();
            Backspace(_cursorPos - _completionStart);
            WriteString(_completions[_completionsIndex]);
        }


        private void NextAutoComplete()
        {

            _completionsIndex++;
            if (_completionsIndex == _completions.Length)
                _completionsIndex = 0;
            if (RollingShowSuggestions)
                ShowSuggestions();
            Backspace(_cursorPos - _completionStart);
            WriteString(_completions[_completionsIndex]);
        }

        private void PreviousAutoComplete()
        {

            _completionsIndex--;
            if (_completionsIndex == -1)
                _completionsIndex = _completions.Length - 1;
            if (RollingShowSuggestions)
                ShowSuggestions();
            Backspace(_cursorPos - _completionStart);
            WriteString(_completions[_completionsIndex]);
        }

        private void SuggestionAutoComplete()
        {
            if (_completions.Length == 1)
            {
                Backspace(_cursorPos - _completionStart);
                WriteString(_completions[0]);
                _tabCounter = 0;
                //Console.WriteLine();
                return;
            }

            string commonStart = GetCommonStart(_completions);
            string currentWord = _text.ToString().Substring(_completionStart);

            _tabCounter++;

            if (_tabCounter > 1 || (_cursorPos - _completionStart) == 0 || commonStart == currentWord)
            {
                ShowSuggestions();
            }
            else
            {
                Backspace(_cursorPos - _completionStart);
            }

            WriteString(commonStart);
            //_completions = null;


        }

        private void ShowSuggestions()
        {
            int curX = Console.CursorLeft;
            int curY = Console.CursorTop;
            
            // max length

            int maxLen = 0;
            for (int i = 0; i < _completions.Length; i++)
            {
                if (maxLen < _completions[i].Length)
                {
                    maxLen = _completions[i].Length;
                }
            }
            Console.WriteLine();
            // display in columns
            if (maxLen < AutoCompleteColumnLength)
            {
                StringBuilder line = new StringBuilder();
                for (int i = 0; i < _completions.Length; i++)
                {
                    if (this.RollingAutoComplete && i == _completionsIndex)
                        line.Append( _completions[i].Pastel(ColorTheme.HighLight1)); // current suggestion
                    else
                        line.Append(_completions[i].Pastel(ColorTheme.Default2));

                    int fill = AutoCompleteColumnLength - (line.Length % AutoCompleteColumnLength);
                    line.Append(' ', fill);

                    if (line.Length >= AutoCompleteColumns * AutoCompleteColumnLength)
                    {
                        Console.WriteLine(line.ToString());
                        line.Clear();
                    }
                }

                if (line.Length > 0)
                {
                    Console.WriteLine(line.ToString());
                    line.Clear();
                }
            }
            // display in lines
            else
            {
                for (int i = 0; i < _completions.Length; i++)
                {
                    Console.WriteLine(_completions[i]);
                }
            }

            //Console.SetCursorPosition(curX,curY);

            
            string prevText = _text.ToString();

            ClearLine();
            Console.WriteLine();
            Console.Write(this.Prompt);
            _cursorPos = 0;
            _cursorLimit = 0;

            // write previous string

            WriteString(prevText.Substring(0, _completionStart));
            
        }

        private void PrevHistory()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                WriteNewString(_history[_historyIndex]);
            }
        }

        private void NextHistory()
        {
            if (_historyIndex < _history.Count)
            {
                _historyIndex++;
                if (_historyIndex == _history.Count)
                    ClearLine();
                else
                    WriteNewString(_history[_historyIndex]);
            }
        }

        private void ResetAutoComplete()
        {
            _completions = null;
            _completionsIndex = 0;
        }

        private string GetCommonStart(string[] Completions)
        {
            string shortest = Completions.OrderBy(s => s.Length).First();
            int i = 0;
            string result = string.Empty;
            do
            {
                string match = shortest.Substring(0, ++i);
                if (Completions.All(s => s.StartsWith(match)))
                    result = match;
                else
                    break;

            } while (i < shortest.Length);
            return result;
        }

        public string Text
        {
            get
            {
                return _text.ToString();
            }
        }



        public KeyHandler(IConsole console, List<string> history, IAutoCompleteHandler autoCompleteHandler)
        {
            Console2 = console;

            _history = history ?? new List<string>();
            _historyIndex = _history.Count;
            _text = new StringBuilder();
            _keyActions = new Dictionary<string, Action>();


            _keyActions["LeftArrow"] = MoveCursorLeft;
            _keyActions["Home"] = MoveCursorHome;
            _keyActions["End"] = MoveCursorEnd;
            _keyActions["ControlA"] = MoveCursorHome;
            _keyActions["ControlB"] = MoveCursorLeft;
            _keyActions["RightArrow"] = MoveCursorRight;
            _keyActions["ControlF"] = MoveCursorRight;
            _keyActions["ControlE"] = MoveCursorEnd;
            _keyActions["Backspace"] = Backspace;
            _keyActions["Delete"] = Delete;
            _keyActions["ControlD"] = Delete;
            _keyActions["ControlH"] = Backspace;
            _keyActions["ControlL"] = ClearLine;
            _keyActions["Escape"] = ClearLine;
            _keyActions["UpArrow"] = PrevHistory;
            _keyActions["ControlP"] = PrevHistory;
            _keyActions["DownArrow"] = NextHistory;
            _keyActions["ControlN"] = NextHistory;
            _keyActions["ControlU"] = () =>
            {
                while (!IsStartOfLine())
                    Backspace();
            };
            _keyActions["ControlK"] = () =>
            {
                int pos = _cursorPos;
                MoveCursorEnd();
                while (_cursorPos > pos)
                    Backspace();
            };
            _keyActions["ControlW"] = () =>
            {
                while (!IsStartOfLine() && _text[_cursorPos - 1] != ' ')
                    Backspace();
            };
            _keyActions["ControlT"] = TransposeChars;

            _keyActions["Tab"] = () =>
            {
                if (this.RollingAutoComplete && IsInAutoCompleteMode())
                {
                    NextAutoComplete();
                }
                else
                {
                    if (autoCompleteHandler == null || !IsEndOfLine())
                        return;

                    string text = _text.ToString();
                    //if (!string.IsNullOrEmpty(_currentText))
                    //    text = _currentText;

                    _completionStart = text.LastIndexOfAny(autoCompleteHandler.Separators);
                    _completionStart = _completionStart == -1 ? 0 : _completionStart + 1;

                    _completions = autoCompleteHandler.GetSuggestions(text, _completionStart);
                    _completions = _completions?.Length == 0 ? null : _completions;

                    if (_completions == null)
                        return;

                    //StartAutoComplete();
                    if (this.RollingAutoComplete)
                    {
                        StartAutoComplete();
                    }
                    else
                    {
                        SuggestionAutoComplete();
                    }
                }
            };

            _keyActions["ShiftTab"] = () =>
            {
                if (IsInAutoCompleteMode())
                {
                    PreviousAutoComplete();
                }
            };
        }

        public void Done()
        {

        }

        public void Handle(ConsoleKeyInfo keyInfo)
        {
            _keyInfo = keyInfo;

            if (_keyInfo.Key != ConsoleKey.Tab)
                _tabCounter = 0;

            // If in auto complete mode and Tab wasn't pressed
            if (IsInAutoCompleteMode() && _keyInfo.Key != ConsoleKey.Tab)
                ResetAutoComplete();

            Action action;
            _keyActions.TryGetValue(BuildKeyInput(), out action);
            action = action ?? WriteChar;
            action.Invoke();
        }
    }
    public interface IAutoCompleteHandler
    {
        char[] Separators { get; set; }
        string[] GetSuggestions(string text, int index);
    }
    internal interface IConsole
    {
        int CursorLeft { get; }
        int CursorTop { get; }
        int BufferWidth { get; }
        int BufferHeight { get; }
        void SetCursorPosition(int left, int top);
        void SetBufferSize(int width, int height);
        void Write(string value);
        void WriteLine(string value);
    }

    internal class Console2 : IConsole
    {
        public int CursorLeft => Console.CursorLeft;

        public int CursorTop => Console.CursorTop;

        public int BufferWidth => Console.BufferWidth;

        public int BufferHeight => Console.BufferHeight;

        public bool PasswordMode { get; set; }

        public void SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);

        public void SetCursorPosition(int left, int top)
        {
            if (!PasswordMode && left >= 0 && top >= 0)
                Console.SetCursorPosition(left, top);
        }

        public void Write(string value)
        {
            if (PasswordMode)
                value = new String(default(char), value.Length);
            Console.Write(value);
        }

        public void WriteLine(string value) => Console.WriteLine(value);
    }
}

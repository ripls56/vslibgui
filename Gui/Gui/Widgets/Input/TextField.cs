using System;
using Gui.Core.Framework;
using Gui.Core.Input;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Widgets.Input;

public class
    TextField(
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        BoxStyle? style = null,
        TextStyle? textStyle = null,
        Vector4? selectionColor = null,
        Action<string>? onSubmitted = null,
        Action<string>? onChanged = null,
        Action<KeyboardEvent>? onKeyDown = null
    )
    : StatefulWidget, IFocusable
{
    public TextEditingController? Controller { get; } = controller;
    public BoxStyle? Style { get; } = style;
    public TextStyle? TextStyle { get; } = textStyle;
    public Vector4? SelectionColor { get; } = selectionColor;
    public Action<string>? OnSubmitted { get; } = onSubmitted;
    public Action<string>? OnChanged { get; } = onChanged;
    public Action<KeyboardEvent>? OnKeyDown { get; } = onKeyDown;
    public FocusNode? FocusNode { get; } = focusNode;

    public override State CreateState() => new TextFieldState();
}

internal class TextFieldState : State<TextField>,
    IKeyDownHandler, IKeyCharHandler, IKeyUpHandler
{
    private const double CursorBlinkMs = 500;

    private float _currentScrollOffset;
    private DateTime _cursorLastToggle = DateTime.MinValue;

    private Ticker? _cursorTicker;
    private bool _cursorVisible = true;
    private TextEditingController? _internalController;
    private FocusNode? _internalFocusNode;

    private bool _isDragging;
    private int _lastClickCharIndex = -1;
    private DateTime _lastClickTime;
    private Vector4 _resolvedSelectionColor;

    // Resolved styles (theme-aware)
    private BoxStyle _resolvedStyle = null!;
    private TextStyle _resolvedTextStyle;
    private Ticker? _scrollTicker;
    private float _targetScrollOffset;

    private TextEditingController Controller => Widget.Controller ?? _internalController!;
    private FocusNode FocusNode => Widget.FocusNode ?? _internalFocusNode!;

    public void OnKeyChar(
        KeyboardEvent e
    )
    {
        if (!FocusNode.HasFocus || e.KeyChar == '\0'
                                || char.IsControl(e.KeyChar))
        {
            return;
        }

        e.Handled = true;

        var text = Controller.Text;
        var selection = Controller.Selection;
        var len = text.Length;
        var start = Math.Clamp(
            selection.Start,
            0,
            len
        );

        if (!selection.IsEmpty)
        {
            text = text.Remove(
                start,
                Math.Min(
                    selection.End - start,
                    len - start
                )
            );
        }

        Controller.Text = text.Insert(
            start,
            e.KeyChar.ToString()
        );
        Controller.Selection = TextSelection.Collapsed(start + 1);
    }

    public void OnKeyDown(
        KeyboardEvent e
    )
    {
        if (!FocusNode.HasFocus)
        {
            return;
        }

        // Allow parent to intercept keys before TextField handles them (e.g. CommandPicker)
        Widget.OnKeyDown?.Invoke(e);
        if (e.Handled)
        {
            return;
        }

        // Swallow all keyboard input to prevent game movement/actions
        // Exclude Alt so the user can still toggle the cursor via vanilla hotkey
        if (!e.Alt)
        {
            e.Handled = true;
        }

        var text = Controller.Text;
        var selection = Controller.Selection;
        var len = text.Length;

        switch (e.KeyCode)
        {
            case (int)GlKeys.A when e.Ctrl:
                SelectAll(len);
                break;
            case (int)GlKeys.C when e.Ctrl && !selection.IsEmpty:
                CopySelection(text, selection);
                break;
            case (int)GlKeys.X when e.Ctrl && !selection.IsEmpty:
                CutSelection(text, selection);
                break;
            case (int)GlKeys.V when e.Ctrl:
                Paste(text, selection);
                break;
            case (int)GlKeys.BackSpace when e.Ctrl && selection.IsEmpty:
            {
                var newPos = WordJumpLeft(text, selection.BaseOffset);
                Controller.Text = text.Remove(newPos, selection.BaseOffset - newPos);
                Controller.Selection = TextSelection.Collapsed(newPos);
                break;
            }
            case (int)GlKeys.Delete when e.Ctrl && selection.IsEmpty:
            {
                var newEnd = WordJumpRight(text, selection.BaseOffset);
                Controller.Text = text.Remove(selection.BaseOffset, newEnd - selection.BaseOffset);
                break;
            }
            case (int)GlKeys.BackSpace when !selection.IsEmpty:
            case (int)GlKeys.Delete when !selection.IsEmpty:
                DeleteSelection(text, selection, len);
                break;
            case (int)GlKeys.BackSpace:
                DeleteBackward(text, selection, len);
                break;
            case (int)GlKeys.Delete:
                DeleteForward(text, selection, len);
                break;
            case (int)GlKeys.Left when e.Ctrl && e.Shift:
                Controller.Selection = new TextSelection
                {
                    BaseOffset = selection.BaseOffset,
                    ExtentOffset = WordJumpLeft(text, selection.ExtentOffset)
                };
                break;
            case (int)GlKeys.Left when e.Ctrl:
                Controller.Selection =
                    TextSelection.Collapsed(WordJumpLeft(text, selection.ExtentOffset));
                break;
            case (int)GlKeys.Left when e.Shift:
                Controller.Selection = new TextSelection
                {
                    BaseOffset = selection.BaseOffset,
                    ExtentOffset = Math.Max(0, selection.ExtentOffset - 1)
                };
                break;
            case (int)GlKeys.Left when !selection.IsEmpty:
                Controller.Selection = TextSelection.Collapsed(selection.Start);
                break;
            case (int)GlKeys.Left:
                Controller.Selection =
                    TextSelection.Collapsed(Math.Max(0, selection.BaseOffset - 1));
                break;
            case (int)GlKeys.Right when e.Ctrl && e.Shift:
                Controller.Selection = new TextSelection
                {
                    BaseOffset = selection.BaseOffset,
                    ExtentOffset = WordJumpRight(text, selection.ExtentOffset)
                };
                break;
            case (int)GlKeys.Right when e.Ctrl:
                Controller.Selection =
                    TextSelection.Collapsed(WordJumpRight(text, selection.ExtentOffset));
                break;
            case (int)GlKeys.Right when e.Shift:
                Controller.Selection = new TextSelection
                {
                    BaseOffset = selection.BaseOffset,
                    ExtentOffset = Math.Min(len, selection.ExtentOffset + 1)
                };
                break;
            case (int)GlKeys.Right when !selection.IsEmpty:
                Controller.Selection = TextSelection.Collapsed(selection.End);
                break;
            case (int)GlKeys.Right:
                Controller.Selection =
                    TextSelection.Collapsed(Math.Min(len, selection.BaseOffset + 1));
                break;
            case (int)GlKeys.Home when e.Shift:
                Controller.Selection =
                    new TextSelection { BaseOffset = selection.BaseOffset, ExtentOffset = 0 };
                break;
            case (int)GlKeys.Home:
                Controller.Selection = TextSelection.Collapsed(0);
                break;
            case (int)GlKeys.End when e.Shift:
                Controller.Selection = new TextSelection
                {
                    BaseOffset = selection.BaseOffset, ExtentOffset = len
                };
                break;
            case (int)GlKeys.End:
                Controller.Selection = TextSelection.Collapsed(len);
                break;
            case (int)GlKeys.Enter:
                Widget.OnSubmitted?.Invoke(Controller.Text);
                break;
            case (int)GlKeys.Escape:
                FocusNode.Unfocus();
                break;
        }
    }

    public void OnKeyUp(
        KeyboardEvent e
    )
    {
        if (!FocusNode.HasFocus)
        {
            return;
        }

        if (!e.Alt)
        {
            e.Handled = true;
        }
    }

    private void SelectAll(int len) => Controller.Selection =
        new TextSelection { BaseOffset = 0, ExtentOffset = len };

    private void CopySelection(string text, TextSelection selection) => Element.Owner!
        .GetClipboard().SetText(text[selection.Start..selection.End]);

    private void CutSelection(string text, TextSelection selection)
    {
        Element.Owner!.GetClipboard().SetText(text[selection.Start..selection.End]);
        DeleteSelection(text, selection, text.Length);
    }

    private void Paste(string text, TextSelection selection)
    {
        var pasted = Element.Owner!.GetClipboard().GetText();
        if (pasted.Length == 0)
        {
            return;
        }

        var start = selection.IsEmpty ? selection.BaseOffset : selection.Start;
        var next = selection.IsEmpty
            ? text.Insert(start, pasted)
            : text.Remove(selection.Start, selection.End - selection.Start).Insert(start, pasted);
        Controller.Text = next;
        Controller.Selection = TextSelection.Collapsed(start + pasted.Length);
    }

    private void DeleteSelection(string text, TextSelection selection, int len)
    {
        var start = Math.Clamp(selection.Start, 0, len);
        Controller.Text = text.Remove(start, Math.Min(selection.End - start, len - start));
        Controller.Selection = TextSelection.Collapsed(start);
    }

    private void DeleteBackward(string text, TextSelection selection, int len)
    {
        if (selection.BaseOffset <= 0)
        {
            return;
        }

        var pos = Math.Clamp(selection.BaseOffset - 1, 0, len - 1);
        Controller.Text = text.Remove(pos, 1);
        Controller.Selection = TextSelection.Collapsed(pos);
    }

    private void DeleteForward(string text, TextSelection selection, int len)
    {
        if (selection.BaseOffset >= len)
        {
            return;
        }

        var pos = Math.Clamp(selection.BaseOffset, 0, len - 1);
        Controller.Text = text.Remove(pos, 1);
        Controller.Selection = TextSelection.Collapsed(pos);
    }

    public override void InitState()
    {
        base.InitState();
        if (Widget.Controller == null)
        {
            _internalController = new TextEditingController();
        }

        if (Widget.FocusNode == null)
        {
            _internalFocusNode = new FocusNode();
        }

        FocusNode.Owner = Element;
        _scrollTicker = Element.Owner!.GetTickerProvider().CreateTicker(OnScrollTick);
        _cursorTicker = Element.Owner!.GetTickerProvider().CreateTicker(OnCursorTick);

        Controller.AddListener(OnControllerChanged);
        FocusNode.AddListener(OnFocusChanged);
    }

    private void OnScrollTick(
        TimeSpan elapsed
    )
    {
        if (Math.Abs(_targetScrollOffset - _currentScrollOffset) < 0.1f)
        {
            _currentScrollOffset = _targetScrollOffset;
            _scrollTicker?.Stop();
        }
        else
        {
            _currentScrollOffset = MathHelper.Lerp(
                _currentScrollOffset,
                _targetScrollOffset,
                0.25f
            );
        }

        SetState(() => { });
    }

    public override void UpdateWidget(
        TextField oldWidget
    )
    {
        base.UpdateWidget(oldWidget);
        if (oldWidget.Controller != Widget.Controller)
        {
            (oldWidget.Controller ?? _internalController)?.RemoveListener(OnControllerChanged);
            if (Widget.Controller == null)
            {
                _internalController ??= new TextEditingController();
            }

            Controller.AddListener(OnControllerChanged);
        }

        if (oldWidget.FocusNode != Widget.FocusNode)
        {
            (oldWidget.FocusNode ?? _internalFocusNode)?.RemoveListener(OnFocusChanged);
            if (Widget.FocusNode == null)
            {
                _internalFocusNode ??= new FocusNode();
            }

            FocusNode.Owner = Element;
            FocusNode.AddListener(OnFocusChanged);
        }
    }

    private void OnControllerChanged()
    {
        var len = Controller.Text.Length;
        if (Controller.Selection.BaseOffset > len ||
            Controller.Selection.ExtentOffset > len)
        {
            Controller.Selection = TextSelection.Collapsed(len);
        }

        Widget.OnChanged?.Invoke(Controller.Text);
        UpdateScrollTarget();

        // Reset cursor to visible on any edit; stop blink while selection active
        _cursorVisible = true;
        _cursorLastToggle = DateTime.Now;
        if (FocusNode.HasFocus && Controller.Selection.IsEmpty)
        {
            if (!_cursorTicker!.IsTicking)
            {
                _cursorTicker.Start();
            }
        }
        else
        {
            _cursorTicker?.Stop();
        }

        SetState(() => { });
    }

    private void UpdateScrollTarget()
    {
        var ro = Element.RenderObject;
        if (ro == null)
        {
            return;
        }

        var font = TextLayoutHelper.GetFont(
            _resolvedTextStyle.FontFamily,
            _resolvedTextStyle.FontSize,
            _resolvedTextStyle.Weight
        );
        var viewportWidth = ro.Size.X - 20; // 10 padding on each side

        var text = Controller.Text;
        var totalWidth = font.MeasureText(text);
        var extentPos = Math.Clamp(
            Controller.Selection.ExtentOffset,
            0,
            text.Length
        );
        var cursorX = font.MeasureText(
            text.Substring(
                0,
                extentPos
            )
        );

        if (totalWidth <= viewportWidth)
        {
            _targetScrollOffset = 0;
        }
        else if (extentPos == text.Length)
        {
            _targetScrollOffset = totalWidth - viewportWidth + 5;
        }
        else
        {
            if (cursorX < _targetScrollOffset)
            {
                _targetScrollOffset = Math.Max(
                    0,
                    cursorX - 10
                );
            }
            else if (cursorX > _targetScrollOffset + viewportWidth - 5)
            {
                _targetScrollOffset = cursorX - viewportWidth + 5;
            }
        }

        _targetScrollOffset = Math.Clamp(
            _targetScrollOffset,
            0,
            Math.Max(
                0,
                totalWidth - viewportWidth + 10
            )
        );
        if (Math.Abs(_targetScrollOffset - _currentScrollOffset) > 0.5f
            && !_scrollTicker!.IsTicking)
        {
            _scrollTicker.Start();
        }
    }

    private void OnCursorTick(
        TimeSpan elapsed
    )
    {
        if (!FocusNode.HasFocus || !Controller.Selection.IsEmpty)
        {
            _cursorVisible = true;
            _cursorTicker?.Stop();
            return;
        }

        var now = DateTime.Now;
        if ((now - _cursorLastToggle).TotalMilliseconds >= CursorBlinkMs)
        {
            _cursorLastToggle = now;
            _cursorVisible = !_cursorVisible;
            SetState(() => { });
        }
    }

    private void OnFocusChanged()
    {
        if (FocusNode.HasFocus && Controller.Selection.IsEmpty)
        {
            _cursorVisible = true;
            _cursorLastToggle = DateTime.Now;
            if (!_cursorTicker!.IsTicking)
            {
                _cursorTicker.Start();
            }
        }
        else
        {
            _cursorTicker?.Stop();
            _cursorVisible = true;
        }

        SetState(() => { });
    }

    private int GetCharIndexAtX(
        float localX
    )
    {
        var font = TextLayoutHelper.GetFont(
            _resolvedTextStyle.FontFamily,
            _resolvedTextStyle.FontSize,
            _resolvedTextStyle.Weight
        );
        var text = Controller.Text;
        if (text.Length == 0)
        {
            return 0;
        }

        var textX = localX - 10 + _currentScrollOffset; // account for padding
        if (textX <= 0)
        {
            return 0;
        }

        var totalWidth = font.MeasureText(text);
        if (textX >= totalWidth)
        {
            return text.Length;
        }

        // Binary search for character index
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            var midWidth = font.MeasureText(
                text.Substring(
                    0,
                    mid + 1
                )
            );
            var prevWidth = mid > 0
                ? font.MeasureText(
                    text.Substring(
                        0,
                        mid
                    )
                )
                : 0;
            var charCenter = (prevWidth + midWidth) / 2;
            if (textX < charCenter)
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return lo;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static int WordJumpLeft(string text, int pos)
    {
        if (pos == 0)
        {
            return 0;
        }

        pos--;
        while (pos > 0 && !IsWordChar(text[pos]))
        {
            pos--;
        }

        while (pos > 0 && IsWordChar(text[pos - 1]))
        {
            pos--;
        }

        return pos;
    }

    private static int WordJumpRight(string text, int pos)
    {
        var len = text.Length;
        if (pos >= len)
        {
            return len;
        }

        while (pos < len && !IsWordChar(text[pos]))
        {
            pos++;
        }

        while (pos < len && IsWordChar(text[pos]))
        {
            pos++;
        }

        return pos;
    }

    private static (int start, int end) FindWordBoundary(
        string text,
        int pos
    )
    {
        if (text.Length == 0)
        {
            return (0, 0);
        }

        pos = Math.Clamp(
            pos,
            0,
            text.Length - 1
        );

        var start = pos;
        var end = pos;

        if (IsWordChar(text[pos]))
        {
            while (start > 0 && IsWordChar(text[start - 1]))
            {
                start--;
            }

            while (end < text.Length - 1 && IsWordChar(text[end + 1]))
            {
                end++;
            }

            end++;
        }
        else
        {
            // Select the single non-word character
            end = pos + 1;
        }

        return (start, end);
    }

    public void OnPointerDown(
        PointerEvent e
    )
    {
        FocusNode.RequestFocus();
        e.Handled = true;

        var ro = Element.RenderObject;
        if (ro == null)
        {
            return;
        }

        var localPos = ro.GlobalToLocal(
            new Vector2(
                e.X,
                e.Y
            )
        );
        var charIndex = GetCharIndexAtX(localPos.X);

        var now = DateTime.Now;
        var isDoubleClick = (now - _lastClickTime).TotalMilliseconds < 400
                            && _lastClickCharIndex == charIndex;
        _lastClickTime = now;
        _lastClickCharIndex = charIndex;

        if (isDoubleClick)
        {
            var (wordStart, wordEnd) = FindWordBoundary(
                Controller.Text,
                charIndex
            );
            Controller.Selection = new TextSelection
            {
                BaseOffset = wordStart, ExtentOffset = wordEnd
            };
            _isDragging = false;
        }
        else
        {
            Controller.Selection = TextSelection.Collapsed(charIndex);
            _isDragging = true;
        }
    }

    public void OnPointerMove(
        PointerEvent e
    )
    {
        if (!_isDragging)
        {
            return;
        }

        e.Handled = true;

        var ro = Element.RenderObject;
        if (ro == null)
        {
            return;
        }

        var localPos = ro.GlobalToLocal(
            new Vector2(
                e.X,
                e.Y
            )
        );
        var charIndex = GetCharIndexAtX(localPos.X);

        Controller.Selection = new TextSelection
        {
            BaseOffset = Controller.Selection.BaseOffset, ExtentOffset = charIndex
        };
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        _resolvedStyle = Widget.Style ?? new BoxStyle
        {
            Color = new Vector4(
                colors.Background.X,
                colors.Background.Y,
                colors.Background.Z,
                0.8f
            ),
            BorderThickness = 1,
            BorderColor = colors.Border,
            Height = 40
        };
        _resolvedTextStyle = Widget.TextStyle ?? new TextStyle
        {
            FontSize = theme.TextTheme.Body.FontSize, Color = colors.OnSurface
        };
        _resolvedSelectionColor = Widget.SelectionColor ?? new Vector4(
            colors.Primary.X,
            colors.Primary.Y,
            colors.Primary.Z,
            0.4f
        );

        var selection = Controller.Selection;
        return new GestureDetector(
            onPress: OnPointerDown,
            onMove: OnPointerMove,
            onRelease: _ => _isDragging = false,
            child: new TextFieldRenderWidget(
                Controller.Text,
                selection.ExtentOffset,
                selection.Start,
                selection.End,
                _resolvedSelectionColor,
                FocusNode.HasFocus,
                _resolvedStyle,
                _resolvedTextStyle,
                _currentScrollOffset,
                _cursorVisible,
                colors.Primary
            )
        );
    }

    public override void Dispose()
    {
        Controller.RemoveListener(OnControllerChanged);
        FocusNode.RemoveListener(OnFocusChanged);
        _scrollTicker?.Dispose();
        _cursorTicker?.Dispose();
        _internalController?.Dispose();
        _internalFocusNode?.Dispose();
        base.Dispose();
    }
}

internal class TextFieldRenderWidget : RenderObjectWidget
{
    public TextFieldRenderWidget(
        string text,
        int cursorPosition,
        int selectionStart,
        int selectionEnd,
        Vector4 selectionColor,
        bool hasFocus,
        BoxStyle style,
        TextStyle textStyle,
        float scrollOffset,
        bool cursorVisible = true,
        Vector4? focusBorderColor = null
    )
    {
        Text = text;
        CursorPosition = cursorPosition;
        SelectionStart = selectionStart;
        SelectionEnd = selectionEnd;
        SelectionColor = selectionColor;
        HasFocus = hasFocus;
        Style = style;
        TextStyle = textStyle;
        ScrollOffset = scrollOffset;
        CursorVisible = cursorVisible;
        FocusBorderColor = focusBorderColor ?? new Vector4(
            1,
            0.8f,
            0,
            1
        );
    }

    public string Text { get; }
    public int CursorPosition { get; }
    public int SelectionStart { get; }
    public int SelectionEnd { get; }
    public Vector4 SelectionColor { get; }
    public bool HasFocus { get; }
    public BoxStyle Style { get; }
    public TextStyle TextStyle { get; }
    public float ScrollOffset { get; }
    public Vector4 FocusBorderColor { get; }
    public bool CursorVisible { get; }

    public override RenderObject CreateRenderObject() => new RenderTextField();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderTextField)renderObject;
        ro.Text = Text;
        ro.CursorPosition = CursorPosition;
        ro.SelectionStart = SelectionStart;
        ro.SelectionEnd = SelectionEnd;
        ro.SelectionColor = SelectionColor;
        ro.HasFocus = HasFocus;
        ro.Color = Style.Color;
        ro.BorderThickness = Style.BorderThickness;
        ro.BorderColor = HasFocus
            ? FocusBorderColor
            : Style.BorderColor;
        ro.CornerRadii = Style.CornerRadius;
        ro.TextStyle = TextStyle;
        ro.MinWidth = Style.Width;
        ro.MaxWidth = Style.Width;
        ro.MinHeight = Style.Height;
        ro.MaxHeight = Style.Height;
        ro.ScrollOffset = ScrollOffset;
        ro.CursorVisible = CursorVisible;
    }
}

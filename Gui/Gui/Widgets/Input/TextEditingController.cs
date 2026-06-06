using System;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

public struct TextSelection
{
    public int BaseOffset;
    public int ExtentOffset;

    public bool IsEmpty => BaseOffset == ExtentOffset;

    public int Start => Math.Min(
        BaseOffset,
        ExtentOffset
    );

    public int End => Math.Max(
        BaseOffset,
        ExtentOffset
    );

    public static TextSelection Collapsed(int offset) =>
        new() { BaseOffset = offset, ExtentOffset = offset };
}

public struct TextEditingValue
{
    public string Text;
    public TextSelection Selection;

    public TextEditingValue(
        string text = "",
        TextSelection? selection = null
    )
    {
        Text = text;
        Selection = selection ?? TextSelection.Collapsed(0);
    }

    public static readonly TextEditingValue Empty = new("");
}

public class TextEditingController : ValueNotifier<TextEditingValue>
{
    public TextEditingController(
        string text = ""
    ) : base(new TextEditingValue(text))
    {
    }

    public string Text
    {
        get => Value.Text;
        set => Value = new TextEditingValue(
            value,
            TextSelection.Collapsed(value.Length)
        );
    }

    public TextSelection Selection
    {
        get => Value.Selection;
        set => Value = new TextEditingValue(
            Value.Text,
            value
        );
    }

    public void Clear() => Value = TextEditingValue.Empty;
}

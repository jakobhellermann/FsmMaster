// SPDX-License-Identifier: EUPL-1.2
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FsmMaster;

// Small clickable circular on/off indicator - unfilled ring when off, filled disc when on. Not a
// CanvasImage subclass: CanvasImage.Build() hardcodes image.sprite to UICommon's single shared
// SolidSprite, and this widget needs to swap between two different procedurally-generated sprites
// (UICommon.DotRingSprite/DotFilledSprite) instead.
internal sealed class CanvasToggleDot : CanvasNode
{
    private readonly UICommon _ui;
    private Image? _image;
    private bool _on;
    private Color _tint;

    public event Action? OnClicked;

    protected override bool Interactable => true;

    public CanvasToggleDot(string name, UICommon ui) : base(name)
    {
        _ui = ui;
        _tint = ui.AccentColor;
    }

    public bool On
    {
        get => _on;
        set
        {
            _on = value;
            if (_image != null)
            {
                _image.sprite = _on ? _ui.DotFilledSprite : _ui.DotRingSprite;
            }
        }
    }

    // Defaults to AccentColor - overridden per-instance (ActionDisabledTextColor, for a dot belonging
    // to a right-click-disabled action's own field row - see FsmActiveStatePanel.AddToggleDot) rather
    // than baked into a shared static color, so only that action's own dots grey out and every other
    // dot in the panel (Variables tab, an enabled action) keeps the normal look.
    public Color Tint
    {
        get => _tint;
        set
        {
            _tint = value;
            if (_image != null)
            {
                _image.color = _tint;
            }
        }
    }

    public override void Build(Transform? rootParent = null)
    {
        base.Build(rootParent);

        _image = GameObject!.AddComponent<Image>();
        _image.sprite = _on ? _ui.DotFilledSprite : _ui.DotRingSprite;
        _image.color = _tint;

        AddEventTrigger(EventTriggerType.PointerClick, (PointerEventData _) => OnClicked?.Invoke());
    }
}

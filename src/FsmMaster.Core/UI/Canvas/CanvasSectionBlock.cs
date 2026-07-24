// SPDX-License-Identifier: EUPL-1.2
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FsmMaster;

// Outlined, clickable background block FsmActiveStatePanel draws behind one action's fields (or one
// variable-type group's rows) to visually separate it from its neighbors - see
// FsmActiveStatePanel.BeginBlock/EndBlock. Not a CanvasButton: a button forces a single centered
// label, but this widget's "content" is whichever rows the caller positions as later-added (and so
// frontmost, per CanvasButton's own convention) siblings on top of it. Clicking anywhere on the block
// that isn't already covered by one of those rows (a plain label/value CanvasText is non-interactable
// and lets the click fall through - see CanvasText.Interactable) hits this block instead and selects it.
internal sealed class CanvasSectionBlock : CanvasImage
{
    private readonly UICommon _ui;
    private bool _selected;
    private bool _disabled;
    private bool _dragging;

    public event Action? OnClicked;

    // Fired instead of OnClicked for a right-button PointerClick - FsmActiveStatePanel.BuildActionsRows
    // wires this to toggle the action's Enabled flag, mirroring the graph overlay's right-click-to-
    // disable gesture for states/transitions (see FsmGraphOverlay.HandleNodeRightClick).
    public event Action? OnRightClicked;

    protected override bool Interactable => true;

    public CanvasSectionBlock(string name, UICommon ui) : base(name, ui)
    {
        _ui = ui;
        Tint = ui.SectionBlockColor;
        AddBorder(ui.PanelBorder);
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            RefreshTint();
        }
    }

    // True for an action right-click-disabled via FsmEditManager.SetActionEnabled - greys out this
    // block's background (SectionBlockDisabledColor unselected, SectionBlockSelectedDisabledColor's
    // dark grey-navy once selected too - see FsmActiveStatePanel.BuildActionsRows), so a disabled
    // action still reads as "off" even while focused. Unused (stays false) for any other block kind
    // this widget backs (variable-type groups, sequencer blocks).
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            RefreshTint();
        }
    }

    // Currently being drag-reordered (FsmActiveStatePanel.BeginActionDrag/EndActionDrag) - takes priority
    // over Selected/Disabled below so the block reads as solid the instant a drag starts, regardless of
    // whichever of those it also happens to be.
    public bool Dragging
    {
        get => _dragging;
        set
        {
            _dragging = value;
            RefreshTint();
        }
    }

    private void RefreshTint() =>
        Tint = _dragging
            ? _ui.SectionBlockDraggingColor
            : _disabled
                ? (_selected ? _ui.SectionBlockSelectedDisabledColor : _ui.SectionBlockDisabledColor)
                : (_selected ? _ui.SectionBlockSelectedColor : _ui.SectionBlockColor);

    public override void Build(Transform? rootParent = null)
    {
        base.Build(rootParent);
        AddEventTrigger(EventTriggerType.PointerClick, (PointerEventData e) =>
        {
            if (e.button == PointerEventData.InputButton.Right)
            {
                OnRightClicked?.Invoke();
            }
            else
            {
                OnClicked?.Invoke();
            }
        });
    }
}

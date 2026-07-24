// SPDX-License-Identifier: EUPL-1.2
using System;
using System.Collections.Generic;

namespace FsmMaster;

// Plain, flat DTOs for UnityEngine.JsonUtility, the only JSON serializer available given this mod's
// dependencies (no Newtonsoft/System.Text.Json package reference).
// JsonUtility cannot serialize Dictionary<> or polymorphic fields, so every value - regardless of the
// underlying PlayMaker variable/field CLR type - is round-tripped as an invariant-culture string and parsed
// back against the recorded type tag on load, instead of modelling one field per possible PlayMaker type.

// The JSON root - one of these is stored per scene (see FsmSaveDataStore), rather than one file covering
// every scene, so a scene's edits can be loaded/cleared without touching data for any other scene.
[Serializable]
internal sealed class SceneEdits
{
    public string SceneName = "";
    public List<FsmEditSet> FsmEdits = new();
}

// One instance of this is applied to every live PlayMakerFSM instance sharing FsmKey (see FsmIdentity), and
// also doubles as the in-memory shape of a pristine snapshot (see FsmPristineSnapshot) - "the values to
// restore" has the exact same shape as "the values to apply".
[Serializable]
internal sealed class FsmEditSet
{
    public string FsmKey = "";
    public List<VariableOverride> VariableOverrides = new();
    public List<ActionFieldOverride> ActionFieldOverrides = new();
    public List<ActionEnabledOverride> ActionEnabledOverrides = new();
    public List<ActionReorder> ActionReorders = new();
    public List<string> DisabledStates = new();
    public List<TransitionRetarget> TransitionRetargets = new();
    public List<SequencerOverride> SequencerOverrides = new();

    // True once every override list has been emptied out again (EnableState/EnableTransition/
    // RemoveSequencer undoing the only edit that was ever made for this FsmKey) - FsmEditManager prunes
    // its _activeEdits entry once this goes true, rather than leaving a stale empty set that would keep
    // reporting this FsmKey as "edited" (GetEditedFsmKeys) forever.
    public bool IsEmpty =>
        VariableOverrides.Count == 0
        && ActionFieldOverrides.Count == 0
        && ActionEnabledOverrides.Count == 0
        && ActionReorders.Count == 0
        && DisabledStates.Count == 0
        && TransitionRetargets.Count == 0
        && SequencerOverrides.Count == 0;
}

// A live-edited value for a single FSM variable (or one array element of it).
[Serializable]
internal sealed class VariableOverride
{
    public string VariableType = "";
    public string Name = "";

    // -1 = the whole variable's value (the original, non-array shape of this override); >= 0 = this
    // one element of an Array-typed variable, leaving every other element untouched.
    public int ArrayIndex = -1;
    public string StringValue = "";
}

// A live-edited value for a single field on a specific state's action (or one array element of it).
[Serializable]
internal sealed class ActionFieldOverride
{
    public string StateName = "";
    public int ActionIndex;
    public string ExpectedActionTypeName = "";
    public string FieldName = "";

    // -1 = the whole field's value; >= 0 = this one element of a Fsm<Type>[]-typed field (PlayMaker
    // action fields expose arrays this way - see FsmEditManager's array-element handling - never as a
    // raw primitive array or an Array-typed FsmVariable, which is a distinct, variable-only shape).
    public int ArrayIndex = -1;
    public string StringValue = "";
}

// A live-edited Enabled flag for a single state's action - separate from ActionFieldOverride because
// FsmStateAction.Enabled is a base-class property with a private backing field, invisible to the
// reflection ActionFieldOverride relies on (GetField only walks a derived action type's own declared
// fields), not because it needs array-element or per-type-tag handling of its own.
[Serializable]
internal sealed class ActionEnabledOverride
{
    public string StateName = "";
    public int ActionIndex;
    public string ExpectedActionTypeName = "";
    public bool Enabled;
}

// KNOWN BROKEN - see FsmEditManager.MoveAction/ApplyActionReorder's own comments for the full diagnosis
// (physically reassigning FsmState.Actions fights PlayMaker's own activeActionIndex bookkeeping) and the
// recommended custom-action-based redesign. Also not yet wire-serialized at all - FsmSaveDataStore's
// FsmEditSetWire/ToWire/FromWire predates this type and was never updated for it, so a reorder silently
// doesn't survive Save/Load even setting the crash aside. Left in place as-is rather than reworked now -
// see feature_todo.md's "Rearrange actions" entry.
//
// A state's action order once at least one action has been moved from its original position - absolute
// rather than a delta, same reasoning as VariableOverride storing a final value instead of a diff.
// OriginalIndices[i] is the index this action held in the state's original (pristine) action array
// before any reorder; the action now displayed/live at position i is original[OriginalIndices[i]]. One
// entry per StateName - a state with no ActionReorder entry is still in its original order.
[Serializable]
internal sealed class ActionReorder
{
    public string StateName = "";
    public List<int> OriginalIndices = new();
}

// Covers both retargeting where a transition leads (its to-state) and relocating which state it leads
// from - a transition can be moved from one origin state to another (or to/from being a global transition,
// via "" = global) and stays relocated until undone. Disabling an event is expressed as a TransitionRetarget
// with NewToState == DisabledMarker rather than as its own edit kind - a disable, a pure retarget, and a
// full relocation all reduce to the same "this (state, event) transition now originates at NewStateName and
// leads to NewToState, and originally originated at StateName leading to Y" shape, so reset can restore any
// of them the same way.
[Serializable]
internal sealed class TransitionRetarget
{
    public const string DisabledMarker = "\0DISABLED";

    public string StateName = "";    // where the transition originates before this edit ("" = global)
    public string EventName = "";
    public string NewStateName = ""; // where it should originate after this edit ("" = global); same as
                                      // StateName for a pure retarget/disable that doesn't relocate it
    public string NewToState = "";

    // Which event this transition fires under after this edit - blank means "same as EventName"
    // (a plain ToState retarget/relocate/disable, never touching the event identity). Only a
    // drag-onto-an-existing-event-node rebind (see FsmEditManager.RetargetTransitionEvent) ever sets
    // this to something different from EventName.
    public string NewEventName = "";
}

// A custom ordered event sequence installed in place of a RandomEvent-family action on a specific
// state's action (see FsmActionSequencer). Pattern is the flattened list of event names to cycle through.
[Serializable]
internal sealed class SequencerOverride
{
    public string StateName = "";
    public int ActionIndex;
    public List<string> Pattern = new();
    public int RepeatCount; // 0 = unlimited
}

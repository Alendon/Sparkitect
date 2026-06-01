package sparkitect.riderplugin.actions

import com.jetbrains.rider.actions.base.RiderAnAction

/**
 * Frontend Go To submenu action. The first constructor argument is the backend action id; it MUST match
 * the backend [Action] id "GoToRegistration" exactly, which is how the invocation routes over the existing
 * RD protocol to the backend handler without an rdgen model. From an ID-tree leaf usage it lands on the
 * authoritative registration identifier (the C# marker id literal or the resource entry coordinate).
 */
class GoToRegistrationAction : RiderAnAction(
    "GoToRegistration",
    "Registration",
    "Navigate from a generated identification to its registration site")

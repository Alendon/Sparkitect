package sparkitect.riderplugin.actions

import com.jetbrains.rider.actions.base.RiderAnAction

/**
 * Frontend Go To submenu action. The first constructor argument is the backend action id; it MUST match
 * the backend [Action] id "GoToOrderTarget" exactly, which is how the invocation routes over the existing
 * RD protocol to the backend handler without an rdgen model. From an OrderAfter/OrderBefore type argument
 * it lands on the authored method the generated Func-wrapper was emitted from.
 */
class GoToOrderTargetAction : RiderAnAction(
    "GoToOrderTarget",
    "Order Target",
    "Navigate from an OrderAfter/OrderBefore argument to the authored method")

package sparkitect.riderplugin.actions

import com.jetbrains.rider.actions.base.RiderAnAction

/**
 * Frontend no-op spike action. The first constructor argument is the backend action id; it MUST match
 * the backend [Action] id "SparkitectNavSpike", which is how the invocation routes over the existing
 * RD protocol to the backend handler without an rdgen model.
 */
class SparkitectNavSpikeAction : RiderAnAction(
    "SparkitectNavSpike",
    "Sparkitect Nav Spike",
    "No-op spike confirming frontend-to-backend action-id routing")

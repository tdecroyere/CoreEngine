import CoreEngineCommonInterop

public protocol InputsServiceProtocol {
    func associateWindow(_ windowPointer: UnsafeMutableRawPointer?)
    func getInputsState() -> InputsState
    func sendVibrationCommand(_ playerId: UInt, _ leftTriggerMotor: Float, _ rightTriggerMotor: Float, _ leftStickMotor: Float, _ rightStickMotor: Float, _ duration10ms: UInt)
}

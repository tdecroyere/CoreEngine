import CoreEngineCommonInterop

public protocol InputsServiceProtocol {
    func getInputsState() -> InputsState
    func sendVibrationCommand(_ playerId: UInt, _ leftTriggerMotor: Float, _ rightTriggerMotor: Float, _ leftStickMotor: Float, _ rightStickMotor: Float, _ duration10ms: UInt)
}

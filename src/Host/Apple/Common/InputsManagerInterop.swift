import CoreEngineInterop

func getInputsState(inputsContext: UnsafeMutableRawPointer?) -> InputsState {
    let inputsManager = Unmanaged<InputsManager>.fromOpaque(inputsContext!).takeUnretainedValue()
    let result = inputsManager.inputsState
    inputsManager.inputsState.Mouse.DeltaX.Value = 0
    inputsManager.inputsState.Mouse.DeltaY.Value = 0
    return result
}

func sendVibrationCommand(inputsContext: UnsafeMutableRawPointer?, playerId: UInt8, leftTriggerMotor: Float, rightTriggerMotor: Float, leftStickMotor: Float, rightStickMotor: Float, duration10ms: UInt8) {
    let inputsManager = Unmanaged<InputsManager>.fromOpaque(inputsContext!).takeUnretainedValue()

    if (inputsManager.gamepadManager.registeredGamepads.count > playerId) {
        inputsManager.gamepadManager.registeredGamepads[Int(playerId) - 1].sendVibrationCommand(leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms)
    }
}
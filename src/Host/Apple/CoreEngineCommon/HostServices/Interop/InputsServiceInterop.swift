import CoreEngineCommonInterop

func InputsService_getInputsStateInterop(context: UnsafeMutableRawPointer?) -> InputsState {
    let contextObject = Unmanaged<InputsManager>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getInputsState()
}

func InputsService_sendVibrationCommandInterop(context: UnsafeMutableRawPointer?, _ playerId: UInt32, _ leftTriggerMotor: Float, _ rightTriggerMotor: Float, _ leftStickMotor: Float, _ rightStickMotor: Float, _ duration10ms: UInt32) {
    let contextObject = Unmanaged<InputsManager>.fromOpaque(context!).takeUnretainedValue()
    contextObject.sendVibrationCommand(UInt(playerId), leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, UInt(duration10ms))
}

func initInputsService(_ context: InputsManager, _ service: inout InputsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.InputsService_GetInputsState = InputsService_getInputsStateInterop
    service.InputsService_SendVibrationCommand = InputsService_sendVibrationCommandInterop
}

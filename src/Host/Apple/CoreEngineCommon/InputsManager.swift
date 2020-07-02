import Cocoa
import CoreEngineCommonInterop
import GameController

public class InputsManager: InputsServiceProtocol {
    var inputsState: InputsState
    var keyboardManager: KeyboardManager
    var gamepadManager: MacOSGamepadManager

    public init() {
        self.inputsState = InputsState()
        self.keyboardManager = KeyboardManager()
        self.gamepadManager = MacOSGamepadManager()

        let controllers = GCController.controllers()

        print("test")
        for controller in controllers {
            print("ok")
        }

        print(GCKeyboard.coalesced);
    }

    public func getInputsState() -> InputsState {
        let result = self.inputsState
        self.inputsState.Mouse.DeltaX.Value = 0
        self.inputsState.Mouse.DeltaY.Value = 0
        resetInputState()
        return result
    }

    public func sendVibrationCommand(_ playerId: UInt, _ leftTriggerMotor: Float, _ rightTriggerMotor: Float, _ leftStickMotor: Float, _ rightStickMotor: Float, _ duration10ms: UInt) {
        if (self.gamepadManager.registeredGamepads.count > playerId) {
            self.gamepadManager.registeredGamepads[Int(playerId) - 1].sendVibrationCommand(leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, UInt8(duration10ms))
        }
    }

    private func resetInputState() {
        self.inputsState.Keyboard.Space.TransitionCount = 0
    }

    public func processKeyboardEvent(_ event: NSEvent) {
        // TODO: Fill in transition count in case a key state change multiple times per frame
        guard let keyChar = event.characters else {
            return
        }

        switch (keyChar) {
            case "a":
                self.inputsState.Keyboard.KeyA.Value = computeInputObjectValue(event)
            case "b":
                self.inputsState.Keyboard.KeyB.Value = computeInputObjectValue(event)
            case "c":
                self.inputsState.Keyboard.KeyC.Value = computeInputObjectValue(event)
            case "d":
                self.inputsState.Keyboard.KeyD.Value = computeInputObjectValue(event)
            case "e":
                self.inputsState.Keyboard.KeyE.Value = computeInputObjectValue(event)
            case "f":
                self.inputsState.Keyboard.KeyF.Value = computeInputObjectValue(event)
            case "g":
                self.inputsState.Keyboard.KeyG.Value = computeInputObjectValue(event)
            case "h":
                self.inputsState.Keyboard.KeyH.Value = computeInputObjectValue(event)
            case "i":
                self.inputsState.Keyboard.KeyI.Value = computeInputObjectValue(event)
            case "j":
                self.inputsState.Keyboard.KeyJ.Value = computeInputObjectValue(event)
            case "k":
                self.inputsState.Keyboard.KeyK.Value = computeInputObjectValue(event)
            case "l":
                self.inputsState.Keyboard.KeyL.Value = computeInputObjectValue(event)
            case "m":
                self.inputsState.Keyboard.KeyM.Value = computeInputObjectValue(event)
            case "n":
                self.inputsState.Keyboard.KeyN.Value = computeInputObjectValue(event)
            case "o":
                self.inputsState.Keyboard.KeyO.Value = computeInputObjectValue(event)
            case "p":
                self.inputsState.Keyboard.KeyP.Value = computeInputObjectValue(event)
            case "q":
                self.inputsState.Keyboard.KeyQ.Value = computeInputObjectValue(event)
            case "r":
                self.inputsState.Keyboard.KeyR.Value = computeInputObjectValue(event)
            case "s":
                self.inputsState.Keyboard.KeyS.Value = computeInputObjectValue(event)
            case "t":
                self.inputsState.Keyboard.KeyT.Value = computeInputObjectValue(event)
            case "u":
                self.inputsState.Keyboard.KeyU.Value = computeInputObjectValue(event)
            case "v":
                self.inputsState.Keyboard.KeyV.Value = computeInputObjectValue(event)
            case "w":
                self.inputsState.Keyboard.KeyW.Value = computeInputObjectValue(event)
            case "x":
                self.inputsState.Keyboard.KeyX.Value = computeInputObjectValue(event)
            case "y":
                self.inputsState.Keyboard.KeyY.Value = computeInputObjectValue(event)
            case "z":
                self.inputsState.Keyboard.KeyZ.Value = computeInputObjectValue(event)
            default:
                processSpecialKeyboardKeys(event)
        }
    }

    public func processMouseMovedEvent(_ event: NSEvent) {
        self.inputsState.Mouse.DeltaX.Value = -((event.deltaX != CGFloat.nan) ? Float(event.deltaX * 0.5) : 0)
        self.inputsState.Mouse.DeltaY.Value = -((event.deltaY != CGFloat.nan) ? Float(event.deltaY * 0.5) : 0)
    }

    public func processMouseLeftButtonEvent(_ event: NSEvent) {
        self.inputsState.Mouse.LeftButton.Value = computeInputObjectValue(event)
        self.inputsState.Mouse.LeftButton.TransitionCount = 1
    }

    public func processGamepadControllers() {
        // TODO: Process connect events
        let controllers = self.gamepadManager.registeredGamepads

        if (controllers.count > 0)
        {
            //print(controllers[0].productName)
            setGamepadState(controllers[0], &self.inputsState.Gamepad1)
        }

        // TODO: Process other gamepads
    }

    private func setGamepadState(_ controller: MacOSGamepad, _ gamepad: inout InputsGamepad) {
        gamepad.PlayerId = 1
        gamepad.Button1.Value = controller.button1
        gamepad.Button2.Value = controller.button2
        gamepad.Button3.Value = controller.button3
        gamepad.Button4.Value = controller.button4
        gamepad.LeftShoulder.Value = controller.leftShoulder
        gamepad.RightShoulder.Value = controller.rightShoulder
        gamepad.ButtonStart.Value = controller.buttonStart
        gamepad.ButtonBack.Value = controller.buttonBack
        gamepad.ButtonSystem.Value = controller.buttonSystem
        gamepad.ButtonLeftStick.Value = controller.buttonLeftStick
        gamepad.ButtonRightStick.Value = controller.buttonRightStick
        gamepad.LeftTrigger.Value = controller.leftTrigger
        gamepad.RightTrigger.Value = controller.rightTrigger
        gamepad.DPadUp.Value = controller.dpadUp
        gamepad.DPadRight.Value = controller.dpadRight
        gamepad.DPadDown.Value = controller.dpadDown
        gamepad.DPadLeft.Value = controller.dpadLeft

        gamepad.LeftStickLeft.Value = (controller.leftStickX < 0.0) ? -controller.leftStickX : 0.0
        gamepad.LeftStickRight.Value = (controller.leftStickX > 0.0) ? controller.leftStickX : 0.0
        gamepad.LeftStickUp.Value = (controller.leftStickY < 0.0) ? -controller.leftStickY : 0.0
        gamepad.LeftStickDown.Value = (controller.leftStickY > 0.0) ? controller.leftStickY : 0.0

        gamepad.RightStickLeft.Value = (controller.rightStickX < 0.0) ? -controller.rightStickX : 0.0
        gamepad.RightStickRight.Value = (controller.rightStickX > 0.0) ? controller.rightStickX : 0.0
        gamepad.RightStickUp.Value = (controller.rightStickY < 0.0) ? -controller.rightStickY : 0.0
        gamepad.RightStickDown.Value = (controller.rightStickY > 0.0) ? controller.rightStickY : 0.0
    }

    private func processSpecialKeyboardKeys(_ event: NSEvent) {
        let keyCode = event.keyCode
        
        if (keyCode == 123) { // Left Arrow
            self.inputsState.Keyboard.LeftArrow.Value = computeInputObjectValue(event)
        } else if (keyCode == 124) { // Right Arrow
            self.inputsState.Keyboard.RightArrow.Value = computeInputObjectValue(event)
        } else if (keyCode == 126) { // Up Arrow
            self.inputsState.Keyboard.UpArrow.Value = computeInputObjectValue(event)
        } else if (keyCode == 125) { // Down Arrow
            self.inputsState.Keyboard.DownArrow.Value = computeInputObjectValue(event)
        } else if (keyCode == 49) { // Space
            self.inputsState.Keyboard.Space.Value = computeInputObjectValue(event)
            self.inputsState.Keyboard.Space.TransitionCount += 1;
        } else {
            NSApplication.shared.sendEvent(event)
        }
    }

    private func computeInputObjectValue(_ event: NSEvent) -> Float {
        return (event.type == .keyDown || event.type == .leftMouseDown) ? 1.0 : 0.0
    }
}

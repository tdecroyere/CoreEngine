import Cocoa
import CoreEngineInterop

func getInputsState(inputsContext: UnsafeMutableRawPointer?) -> InputsState {
    let inputsManager = Unmanaged<MacOSInputsManager>.fromOpaque(inputsContext!).takeUnretainedValue()
    let result = inputsManager.inputsState
    inputsManager.inputsState.Mouse.DeltaX.Value = 0
    inputsManager.inputsState.Mouse.DeltaY.Value = 0
    return result
}

class MacOSInputsManager {
    var inputsState: InputsState
    var gamepadManager: MacOSGamepadManager

    init() {
        self.inputsState = InputsState()
        self.gamepadManager = MacOSGamepadManager()
    }

    func processKeyboardEvent(_ event: NSEvent) {
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

    func processMouseMovedEvent(_ event: NSEvent) {
        self.inputsState.Mouse.DeltaX.Value = -((event.deltaX != CGFloat.nan) ? Float(event.deltaX * 0.5) : 0)
        self.inputsState.Mouse.DeltaY.Value = -((event.deltaY != CGFloat.nan) ? Float(event.deltaY * 0.5) : 0)
    }

    func processMouseLeftButtonEvent(_ event: NSEvent) {
        self.inputsState.Mouse.LeftButton.Value = computeInputObjectValue(event)
        self.inputsState.Mouse.LeftButton.TransitionCount = 1
    }

    func processGamepadControllers() {
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
        gamepad.Button1.Value = controller.button1
        gamepad.Button2.Value = controller.button2
        gamepad.Button3.Value = controller.button3
        gamepad.Button4.Value = controller.button4
        gamepad.LeftShoulder.Value = controller.leftShoulder
        gamepad.RightShoulder.Value = controller.rightShoulder
        gamepad.ButtonStart.Value = controller.buttonStart
        gamepad.ButtonBack.Value = controller.buttonBack
        gamepad.LeftTrigger.Value = controller.leftTrigger
        gamepad.RightTrigger.Value = controller.rightTrigger

        gamepad.LeftStickLeft.Value = (controller.leftThumbX < 0.0) ? -controller.leftThumbX : 0.0
        gamepad.LeftStickRight.Value = (controller.leftThumbX > 0.0) ? controller.leftThumbX : 0.0

        gamepad.LeftStickUp.Value = (controller.leftThumbY < 0.0) ? -controller.leftThumbY : 0.0
        gamepad.LeftStickDown.Value = (controller.leftThumbY > 0.0) ? controller.leftThumbY : 0.0
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
        } else {
            NSApplication.shared.sendEvent(event)
        }
    }

    private func computeInputObjectValue(_ event: NSEvent) -> Float {
        return (event.type == .keyDown || event.type == .leftMouseDown) ? 1.0 : 0.0
    }
}

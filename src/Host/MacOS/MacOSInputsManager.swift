import Cocoa
import GameController
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

    init() {
        self.inputsState = InputsState()
        print(GCController.controllers().count)
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
        
        let controllers = GCController.controllers()

        if (controllers.count > 0)
        {
            setGamepadState(controllers[0], &self.inputsState.Gamepad1)
        }

        // TODO: Process other gamepads
    }

    private func setGamepadState(_ controller: GCController, _ gamepad: inout InputsGamepad) {
        guard let connectedGamepad = controller.gamepad else {
            return
        }
        let data = connectedGamepad.saveSnapshot().snapshotData
        print(data)
        gamepad.ButtonA.Value = connectedGamepad.buttonA.value
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
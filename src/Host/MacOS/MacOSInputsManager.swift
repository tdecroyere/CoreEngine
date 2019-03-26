import Cocoa
import CoreEngineInterop

func getInputsState(inputsContext: UnsafeMutableRawPointer?) -> InputsState {
    let inputsManager = Unmanaged<MacOSInputsManager>.fromOpaque(inputsContext!).takeUnretainedValue()
    return inputsManager.inputsState
}

class MacOSInputsManager {
    var inputsState: InputsState

    init() {
        self.inputsState = InputsState()
    }

    func processKeyboardEvent(_ event: NSEvent) {
        // TODO: Fill in transition count in case a key state change multiple times per frame

        guard let keyChar = event.characters else {
            return
        }

        switch (keyChar) {
            case "a":
                self.inputsState.Keyboard.KeyA.Value = computeKeyboardInputObjectValue(event)
            case "b":
                self.inputsState.Keyboard.KeyB.Value = computeKeyboardInputObjectValue(event)
            case "c":
                self.inputsState.Keyboard.KeyC.Value = computeKeyboardInputObjectValue(event)
            case "d":
                self.inputsState.Keyboard.KeyD.Value = computeKeyboardInputObjectValue(event)
            case "e":
                self.inputsState.Keyboard.KeyE.Value = computeKeyboardInputObjectValue(event)
            case "f":
                self.inputsState.Keyboard.KeyF.Value = computeKeyboardInputObjectValue(event)
            case "g":
                self.inputsState.Keyboard.KeyG.Value = computeKeyboardInputObjectValue(event)
            case "h":
                self.inputsState.Keyboard.KeyH.Value = computeKeyboardInputObjectValue(event)
            case "i":
                self.inputsState.Keyboard.KeyI.Value = computeKeyboardInputObjectValue(event)
            case "j":
                self.inputsState.Keyboard.KeyJ.Value = computeKeyboardInputObjectValue(event)
            case "k":
                self.inputsState.Keyboard.KeyK.Value = computeKeyboardInputObjectValue(event)
            case "l":
                self.inputsState.Keyboard.KeyL.Value = computeKeyboardInputObjectValue(event)
            case "m":
                self.inputsState.Keyboard.KeyM.Value = computeKeyboardInputObjectValue(event)
            case "n":
                self.inputsState.Keyboard.KeyN.Value = computeKeyboardInputObjectValue(event)
            case "o":
                self.inputsState.Keyboard.KeyO.Value = computeKeyboardInputObjectValue(event)
            case "p":
                self.inputsState.Keyboard.KeyP.Value = computeKeyboardInputObjectValue(event)
            case "q":
                self.inputsState.Keyboard.KeyQ.Value = computeKeyboardInputObjectValue(event)
            case "r":
                self.inputsState.Keyboard.KeyR.Value = computeKeyboardInputObjectValue(event)
            case "s":
                self.inputsState.Keyboard.KeyS.Value = computeKeyboardInputObjectValue(event)
            case "t":
                self.inputsState.Keyboard.KeyT.Value = computeKeyboardInputObjectValue(event)
            case "u":
                self.inputsState.Keyboard.KeyU.Value = computeKeyboardInputObjectValue(event)
            case "v":
                self.inputsState.Keyboard.KeyV.Value = computeKeyboardInputObjectValue(event)
            case "w":
                self.inputsState.Keyboard.KeyW.Value = computeKeyboardInputObjectValue(event)
            case "x":
                self.inputsState.Keyboard.KeyX.Value = computeKeyboardInputObjectValue(event)
            case "y":
                self.inputsState.Keyboard.KeyY.Value = computeKeyboardInputObjectValue(event)
            case "z":
                self.inputsState.Keyboard.KeyZ.Value = computeKeyboardInputObjectValue(event)
            default:
                processSpecialKeyboardKeys(event)
        }
    }

    private func processSpecialKeyboardKeys(_ event: NSEvent) {
        let keyCode = event.keyCode
        
        if (keyCode == 123) { // Left Arrow
            self.inputsState.Keyboard.LeftArrow.Value = computeKeyboardInputObjectValue(event)
        } else if (keyCode == 124) { // Right Arrow
            self.inputsState.Keyboard.RightArrow.Value = computeKeyboardInputObjectValue(event)
        } else if (keyCode == 126) { // Up Arrow
            self.inputsState.Keyboard.UpArrow.Value = computeKeyboardInputObjectValue(event)
        } else if (keyCode == 125) { // Down Arrow
            self.inputsState.Keyboard.DownArrow.Value = computeKeyboardInputObjectValue(event)
        } else {
            NSApplication.shared.sendEvent(event)
        }
    }

    private func computeKeyboardInputObjectValue(_ event: NSEvent) -> Float {
        return (event.type == .keyDown) ? 1.0 : 0.0
    }
}

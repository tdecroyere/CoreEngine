using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    // TODO: Use MemoryHandle ?
    public readonly struct MemoryBuffer
    {
        public uint Id { get; }
        public IntPtr MemoryPointer { get; }
        public int Length { get; }

        public unsafe Span<byte> AsSpan()
        {
            return new Span<byte>(this.MemoryPointer.ToPointer(), this.Length);
        }
    }

    // TODO: Add parameters to specify global or per frame allocation
    public delegate MemoryBuffer CreateMemoryBufferDelegate(IntPtr memoryManagerContext, int length);
    public delegate void DestroyMemoryBufferDelegate(IntPtr memoryManagerContext, uint memoryBufferId);

    public readonly struct MemoryService
    {
        private IntPtr memoryManagerContext { get; } 
        private CreateMemoryBufferDelegate createMemoryBufferDelegate { get; } 
        private DestroyMemoryBufferDelegate destroyMemoryBufferDelegate { get; } 

        public MemoryBuffer CreateMemoryBuffer(int length)
        {
            return createMemoryBufferDelegate(memoryManagerContext, length);
        }

        public void DestroyMemoryBuffer(uint memoryBufferId)
        {
            destroyMemoryBufferDelegate(memoryManagerContext, memoryBufferId);
        }
    }

    public delegate Vector2 GetRenderSizeDelegate(IntPtr graphicsContext);
    public delegate uint CreateShaderDelegate(IntPtr graphicsContext, MemoryBuffer shaderByteCode);
    public delegate uint CreateGraphicsBufferDelegate(IntPtr graphicsContext, MemoryBuffer data);
    public delegate void SetRenderPassConstantsDelegate(IntPtr graphicsContext, MemoryBuffer data);
    public delegate void DrawPrimitivesDelegate(IntPtr graphicsContext, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, Matrix4x4 worldMatrix);

    public readonly struct GraphicsService
    {
        private IntPtr graphicsContext { get; } 
        private GetRenderSizeDelegate getRenderSizeDelegate { get; } 
        private CreateShaderDelegate createShaderDelegate { get; } 
        private CreateGraphicsBufferDelegate createGraphicsBufferDelegate { get; } 
        private SetRenderPassConstantsDelegate setRenderPassConstantsDelegate { get; } 
        private DrawPrimitivesDelegate drawPrimitivesDelegate { get; } 

        public Vector2 GetRenderSize()
        {
            return getRenderSizeDelegate(graphicsContext);
        }

        public uint CreateShader(MemoryBuffer shaderByteCode)
        {
            return createShaderDelegate(graphicsContext, shaderByteCode);
        }

        public uint CreateGraphicsBuffer(MemoryBuffer data)
        {
            return createGraphicsBufferDelegate(graphicsContext, data);
        }

        public void SetRenderPassConstants(MemoryBuffer data)
        {
            setRenderPassConstantsDelegate(graphicsContext, data);
        }

        public void DrawPrimitives(uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, Matrix4x4 worldMatrix)
        {
            drawPrimitivesDelegate(graphicsContext, startIndex, indexCount, vertexBufferId, indexBufferId, worldMatrix);
        }
    }

    public enum InputsObjectType
    {
        Digital,
        Analog,
        Relative
    }

    public readonly struct InputsObject
    {
        public InputsObjectType ObjectType { get; }
        public int TransitionCount { get; }
        public float Value { get; }
    }

    public readonly struct InputsKeyboard
    {
        public InputsObject KeyA { get; }
        public InputsObject KeyB { get; }
        public InputsObject KeyC { get; }
        public InputsObject KeyD { get; }
        public InputsObject KeyE { get; }
        public InputsObject KeyF { get; }
        public InputsObject KeyG { get; }
        public InputsObject KeyH { get; }
        public InputsObject KeyI { get; }
        public InputsObject KeyJ { get; }
        public InputsObject KeyK { get; }
        public InputsObject KeyL { get; }
        public InputsObject KeyM { get; }
        public InputsObject KeyN { get; }
        public InputsObject KeyO { get; }
        public InputsObject KeyP { get; }
        public InputsObject KeyQ { get; }
        public InputsObject KeyR { get; }
        public InputsObject KeyS { get; }
        public InputsObject KeyT { get; }
        public InputsObject KeyU { get; }
        public InputsObject KeyV { get; }
        public InputsObject KeyW { get; }
        public InputsObject KeyX { get; }
        public InputsObject KeyY { get; }
        public InputsObject KeyZ { get; }
        public InputsObject Space { get; }
        public InputsObject AlternateKey { get; }
        public InputsObject Enter { get; }
        public InputsObject F1 { get; }
        public InputsObject F2 { get; }
        public InputsObject F3 { get; }
        public InputsObject F4 { get; }
        public InputsObject F5 { get; }
        public InputsObject F6 { get; }
        public InputsObject F7 { get; }
        public InputsObject F8 { get; }
        public InputsObject F9 { get; }
        public InputsObject F10 { get; }
        public InputsObject F11 { get; }
        public InputsObject F12 { get; }
        public InputsObject Shift { get; }
        public InputsObject LeftArrow { get; }
        public InputsObject RightArrow { get; }
        public InputsObject UpArrow { get; }
        public InputsObject DownArrow { get; }
    }

    public readonly struct InputsMouse
    {
        public uint PositionX { get; }
        public uint PositionY { get; }

        public InputsObject DeltaX { get; }
        public InputsObject DeltaY { get; }
        public InputsObject LeftButton { get; }
        public InputsObject RightButton { get; }
        public InputsObject MiddleButton { get; }

        // TODO: Handle wheel
    }

    public readonly struct InputsTouch
    {
        public InputsObject DeltaX { get; }
        public InputsObject DeltaY { get; }
    }

    public readonly struct InputsGamepad
    {
        public uint PlayerId { get; }
        public bool IsConnected { get; }
        public InputsObject LeftStickUp { get; }
        public InputsObject LeftStickDown { get; }
        public InputsObject LeftStickLeft { get; }
        public InputsObject LeftStickRight { get; }
        public InputsObject RightStickUp { get; }
        public InputsObject RightStickDown { get; }
        public InputsObject RightStickLeft { get; }
        public InputsObject RightStickRight { get; }
        public InputsObject LeftTrigger { get; }
        public InputsObject RightTrigger { get; }
        public InputsObject ButtonA { get; }
        public InputsObject ButtonB { get; }
        public InputsObject ButtonX { get; }
        public InputsObject ButtonY { get; }
        public InputsObject ButtonStart { get; }
        public InputsObject ButtonBack { get; }
        public InputsObject ButtonSystem { get; }
        public InputsObject ButtonLeftStick { get; }
        public InputsObject ButtonRightStick { get; }
        public InputsObject LeftShoulder { get; }
        public InputsObject RightShoulder { get; }
        public InputsObject DPadUp { get; }
        public InputsObject DPadDown { get; }
        public InputsObject DPadLeft { get; }
        public InputsObject DPadRight { get; }
    }

    public readonly struct InputsState
    {
        public InputsKeyboard Keyboard { get; }
        public InputsMouse Mouse { get; }
        public InputsTouch Touch { get; }
        public InputsGamepad Gamepad1 { get; }
        public InputsGamepad Gamepad2 { get; }
        public InputsGamepad Gamepad3 { get; }
        public InputsGamepad Gamepad4 { get; }
    }

    public delegate InputsState GetInputsStateDelegate(IntPtr inputsContext);
    public delegate void SendVibrationCommandDelegate(IntPtr inputsContext, uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms);

    public readonly struct InputsService
    {
        private IntPtr inputsContext { get; } 
        private GetInputsStateDelegate getInputsStateDelegate { get; } 
        private SendVibrationCommandDelegate sendVibrationCommandDelegate { get; } 

        public InputsState GetInputsState()
        {
            return getInputsStateDelegate(inputsContext);
        }

        public void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            sendVibrationCommandDelegate(inputsContext, playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
        }
    }

    public delegate int AddTestHostMethodDelegate(int a, int b);
    public delegate MemoryBuffer GetTestBufferDelegate();

    public readonly struct HostPlatform
    {
        public int TestParameter { get; }
        public AddTestHostMethodDelegate AddTestHostMethod { get; }
        public GetTestBufferDelegate GetTestBuffer { get; }
        public MemoryService MemoryService { get; }
        public GraphicsService GraphicsService { get; }
        public InputsService InputsService { get; }
    }
}
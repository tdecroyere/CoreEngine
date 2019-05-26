using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    // Disable warning for private fields that are assigned by interop
    #pragma warning disable 649

    // TODO: Provide stubs for not implemented platform functionnalities to avoid craches
    // TODO: Use MemoryHandle ?
    public struct MemoryBuffer
    {
        public uint Id;
        public IntPtr Pointer;
        public int Length;

        public unsafe Span<byte> AsSpan()
        {
            return new Span<byte>(this.Pointer.ToPointer(), this.Length);
        }
    }

    // TODO: Add parameters to specify global or per frame allocation
    public delegate MemoryBuffer CreateMemoryBufferDelegate(IntPtr memoryManagerContext, int length);
    public delegate void DestroyMemoryBufferDelegate(IntPtr memoryManagerContext, uint memoryBufferId);

    public struct MemoryService
    {
        private IntPtr memoryManagerContext;
        private CreateMemoryBufferDelegate createMemoryBufferDelegate;
        private DestroyMemoryBufferDelegate destroyMemoryBufferDelegate;

        public MemoryBuffer CreateMemoryBuffer(int length)
        {
            return createMemoryBufferDelegate(memoryManagerContext, length);
        }

        public void DestroyMemoryBuffer(uint memoryBufferId)
        {
            destroyMemoryBufferDelegate(memoryManagerContext, memoryBufferId);
        }
    }

    public delegate uint CreateShaderDelegate(IntPtr graphicsContext, MemoryBuffer shaderByteCode);
    public delegate uint CreateGraphicsBufferDelegate(IntPtr graphicsContext, MemoryBuffer data);
    public delegate void SetRenderPassConstantsDelegate(IntPtr graphicsContext, MemoryBuffer data);
    public delegate void DrawPrimitivesDelegate(IntPtr graphicsContext, int primitiveCount, uint vertexBufferId, uint indexBufferId, Matrix4x4 worldMatrix);

    public struct GraphicsService
    {
        private IntPtr graphicsContext;
        private CreateShaderDelegate createShaderDelegate;
        private CreateGraphicsBufferDelegate createGraphicsBufferDelegate;
        private SetRenderPassConstantsDelegate setRenderPassConstantsDelegate;
        private DrawPrimitivesDelegate drawPrimitivesDelegate;

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

        public void DrawPrimitives(int primitiveCount, uint vertexBufferId, uint indexBufferId, Matrix4x4 worldMatrix)
        {
            drawPrimitivesDelegate(graphicsContext, primitiveCount, vertexBufferId, indexBufferId, worldMatrix);
        }
    }

    public enum InputsObjectType
    {
        Digital,
        Analog,
        Relative
    }

    // TODO: Optimize the struct based on its type? (Analog or Digital)
    public struct InputsObject
    {
        public InputsObjectType ObjectType;
        public int TransitionCount;
        public float Value;
    }

    public struct InputsKeyboard
    {
        public InputsObject KeyA;
        public InputsObject KeyB;
        public InputsObject KeyC;
        public InputsObject KeyD;
        public InputsObject KeyE;
        public InputsObject KeyF;
        public InputsObject KeyG;
        public InputsObject KeyH;
        public InputsObject KeyI;
        public InputsObject KeyJ;
        public InputsObject KeyK;
        public InputsObject KeyL;
        public InputsObject KeyM;
        public InputsObject KeyN;
        public InputsObject KeyO;
        public InputsObject KeyP;
        public InputsObject KeyQ;
        public InputsObject KeyR;
        public InputsObject KeyS;
        public InputsObject KeyT;
        public InputsObject KeyU;
        public InputsObject KeyV;
        public InputsObject KeyW;
        public InputsObject KeyX;
        public InputsObject KeyY;
        public InputsObject KeyZ;
        public InputsObject Space;
        public InputsObject AlternateKey;
        public InputsObject Enter;
        public InputsObject F1;
        public InputsObject F2;
        public InputsObject F3;
        public InputsObject F4;
        public InputsObject F5;
        public InputsObject F6;
        public InputsObject F7;
        public InputsObject F8;
        public InputsObject F9;
        public InputsObject F10;
        public InputsObject F11;
        public InputsObject F12;
        public InputsObject Shift;
        public InputsObject LeftArrow;
        public InputsObject RightArrow;
        public InputsObject UpArrow;
        public InputsObject DownArrow;
    }

    public struct InputsMouse
    {
        public uint PositionX;
        public uint PositionY;

        public InputsObject DeltaX;
        public InputsObject DeltaY;
        public InputsObject LeftButton;
        public InputsObject RightButton;
        public InputsObject MiddleButton;

        // TODO: Handle wheel
    }

    public struct InputsTouch
    {
        public InputsObject DeltaX;
        public InputsObject DeltaY;
    }

    public struct InputsGamepad
    {
        public uint PlayerId;
        public bool IsConnected;
        public InputsObject LeftStickUp;
        public InputsObject LeftStickDown;
        public InputsObject LeftStickLeft;
        public InputsObject LeftStickRight;
        public InputsObject RightStickUp;
        public InputsObject RightStickDown;
        public InputsObject RightStickLeft;
        public InputsObject RightStickRight;
        public InputsObject LeftTrigger;
        public InputsObject RightTrigger;
        public InputsObject ButtonA;
        public InputsObject ButtonB;
        public InputsObject ButtonX;
        public InputsObject ButtonY;
        public InputsObject ButtonStart;
        public InputsObject ButtonBack;
        public InputsObject ButtonSystem;
        public InputsObject ButtonLeftStick;
        public InputsObject ButtonRightStick;
        public InputsObject LeftShoulder;
        public InputsObject RightShoulder;
        public InputsObject DPadUp;
        public InputsObject DPadDown;
        public InputsObject DPadLeft;
        public InputsObject DPadRight;
    }

    public struct InputsState
    {
        public InputsKeyboard Keyboard;
        public InputsMouse Mouse;
        public InputsTouch Touch;
        public InputsGamepad Gamepad1;
        public InputsGamepad Gamepad2;
        public InputsGamepad Gamepad3;
        public InputsGamepad Gamepad4;
    }

    public delegate InputsState GetInputsStateDelegate(IntPtr inputsContext);
    public delegate void SendVibrationCommandDelegate(IntPtr inputsContext, uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms);

    public struct InputsService
    {
        private IntPtr inputsContext;
        private GetInputsStateDelegate getInputsStateDelegate;
        private SendVibrationCommandDelegate sendVibrationCommandDelegate;

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

    public struct HostPlatform
    {
        public int TestParameter;
        public AddTestHostMethodDelegate AddTestHostMethod;
        public GetTestBufferDelegate GetTestBuffer;
        public MemoryService MemoryService;
        public GraphicsService GraphicsService;
        public InputsService InputsService;
    }

    #pragma warning restore 649
}
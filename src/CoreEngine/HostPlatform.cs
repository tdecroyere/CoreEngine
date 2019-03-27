using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    // TODO: Find a way to hide that to external assemblies

    public struct ByteSpan
    {
        public IntPtr Pointer;
        public int Length;

        public unsafe static implicit operator Span<byte>(ByteSpan value)
        {
            return new Span<byte>(value.Pointer.ToPointer(), value.Length);
        }
    }

    public delegate int AddTestHostMethodDelegate(int a, int b);
    public delegate ByteSpan GetTestBufferDelegate();

    public struct HostPlatform
    {
        public int TestParameter;
        public AddTestHostMethodDelegate AddTestHostMethod;
        public GetTestBufferDelegate GetTestBuffer;
        public GraphicsService GraphicsService;
        public InputsService InputsService;
    }

    public delegate void DebugDrawTriangleDelegate(IntPtr graphicsContext, Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix);

    public struct GraphicsService
    {
        public IntPtr GraphicsContext;
        public DebugDrawTriangleDelegate DebugDrawTriange;
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

    public struct InputsService
    {
        public IntPtr InputsContext;
        public GetInputsStateDelegate GetInputsState;
    }
}
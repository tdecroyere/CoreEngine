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

    public enum GameInputObjectType
    {
        Digital,
        Analog,
        Relative
    }

    // TODO: Optimize the struct based on its type? (Analog or Digital)
    public struct GameInputObject
    {
        GameInputObjectType ObjectType;
        int TransitionCount;
        float Value;
    }

    public struct GameInputKeyboard
    {
        GameInputObject KeyA;
        GameInputObject KeyB;
        GameInputObject KeyC;
        GameInputObject KeyD;
        GameInputObject KeyE;
        GameInputObject KeyF;
        GameInputObject KeyG;
        GameInputObject KeyH;
        GameInputObject KeyI;
        GameInputObject KeyJ;
        GameInputObject KeyK;
        GameInputObject KeyL;
        GameInputObject KeyM;
        GameInputObject KeyN;
        GameInputObject KeyO;
        GameInputObject KeyP;
        GameInputObject KeyQ;
        GameInputObject KeyR;
        GameInputObject KeyS;
        GameInputObject KeyT;
        GameInputObject KeyU;
        GameInputObject KeyV;
        GameInputObject KeyW;
        GameInputObject KeyX;
        GameInputObject KeyY;
        GameInputObject KeyZ;
        GameInputObject Space;
        GameInputObject AlternateKey;
        GameInputObject Enter;
        GameInputObject F1;
        GameInputObject F2;
        GameInputObject F3;
        GameInputObject F4;
        GameInputObject F5;
        GameInputObject F6;
        GameInputObject F7;
        GameInputObject F8;
        GameInputObject F9;
        GameInputObject F10;
        GameInputObject F11;
        GameInputObject F12;
        GameInputObject Shift;
    }

    public struct GameInputMouse
    {
        uint PositionX;
        uint PositionY;

        GameInputObject DeltaX;
        GameInputObject DeltaY;
        GameInputObject LeftButton;
        GameInputObject RightButton;
        GameInputObject MiddleButton;

        // TODO: Handle wheel
    }

    public struct GameInputTouch
    {
        GameInputObject DeltaX;
        GameInputObject DeltaY;
    }

    public struct GameInputGamepad
    {
        bool IsConnected;
        GameInputObject LeftStickUp;
        GameInputObject LeftStickDown;
        GameInputObject LeftStickLeft;
        GameInputObject LeftStickRight;
        GameInputObject RightStickUp;
        GameInputObject RightStickDown;
        GameInputObject RightStickLeft;
        GameInputObject RightStickRight;
        GameInputObject LeftTrigger;
        GameInputObject RightTrigger;
        GameInputObject ButtonA;
        GameInputObject ButtonB;
        GameInputObject ButtonX;
        GameInputObject ButtonY;
        GameInputObject ButtonStart;
        GameInputObject ButtonBack;
        GameInputObject LeftShoulder;
        GameInputObject RightShoulder;
        GameInputObject DPadUp;
        GameInputObject DPadDown;
        GameInputObject DPadLeft;
        GameInputObject DPadRight;
    }

    public struct InputsService
    {
        GameInputKeyboard Keyboard;
        GameInputMouse Mouse;
        GameInputTouch Touch;
        GameInputGamepad GamePad1;
        GameInputGamepad GamePad2;
        GameInputGamepad GamePad3;
        GameInputGamepad GamePad4;
    }
}
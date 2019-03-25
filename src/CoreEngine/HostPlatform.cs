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
        public GameInputObjectType ObjectType;
        public int TransitionCount;
        public float Value;
    }

    public struct GameInputKeyboard
    {
        public GameInputObject KeyA;
        public GameInputObject KeyB;
        public GameInputObject KeyC;
        public GameInputObject KeyD;
        public GameInputObject KeyE;
        public GameInputObject KeyF;
        public GameInputObject KeyG;
        public GameInputObject KeyH;
        public GameInputObject KeyI;
        public GameInputObject KeyJ;
        public GameInputObject KeyK;
        public GameInputObject KeyL;
        public GameInputObject KeyM;
        public GameInputObject KeyN;
        public GameInputObject KeyO;
        public GameInputObject KeyP;
        public GameInputObject KeyQ;
        public GameInputObject KeyR;
        public GameInputObject KeyS;
        public GameInputObject KeyT;
        public GameInputObject KeyU;
        public GameInputObject KeyV;
        public GameInputObject KeyW;
        public GameInputObject KeyX;
        public GameInputObject KeyY;
        public GameInputObject KeyZ;
        public GameInputObject Space;
        public GameInputObject AlternateKey;
        public GameInputObject Enter;
        public GameInputObject F1;
        public GameInputObject F2;
        public GameInputObject F3;
        public GameInputObject F4;
        public GameInputObject F5;
        public GameInputObject F6;
        public GameInputObject F7;
        public GameInputObject F8;
        public GameInputObject F9;
        public GameInputObject F10;
        public GameInputObject F11;
        public GameInputObject F12;
        public GameInputObject Shift;
    }

    public struct GameInputMouse
    {
        public uint PositionX;
        public uint PositionY;

        public GameInputObject DeltaX;
        public GameInputObject DeltaY;
        public GameInputObject LeftButton;
        public GameInputObject RightButton;
        public GameInputObject MiddleButton;

        // TODO: Handle wheel
    }

    public struct GameInputTouch
    {
        public GameInputObject DeltaX;
        public GameInputObject DeltaY;
    }

    public struct GameInputGamepad
    {
        public bool IsConnected;
        public GameInputObject LeftStickUp;
        public GameInputObject LeftStickDown;
        public GameInputObject LeftStickLeft;
        public GameInputObject LeftStickRight;
        public GameInputObject RightStickUp;
        public GameInputObject RightStickDown;
        public GameInputObject RightStickLeft;
        public GameInputObject RightStickRight;
        public GameInputObject LeftTrigger;
        public GameInputObject RightTrigger;
        public GameInputObject ButtonA;
        public GameInputObject ButtonB;
        public GameInputObject ButtonX;
        public GameInputObject ButtonY;
        public GameInputObject ButtonStart;
        public GameInputObject ButtonBack;
        public GameInputObject LeftShoulder;
        public GameInputObject RightShoulder;
        public GameInputObject DPadUp;
        public GameInputObject DPadDown;
        public GameInputObject DPadLeft;
        public GameInputObject DPadRight;
    }

    public struct InputsService
    {
        public GameInputKeyboard Keyboard;
        public GameInputMouse Mouse;
        public GameInputTouch Touch;
        public GameInputGamepad GamePad1;
        public GameInputGamepad GamePad2;
        public GameInputGamepad GamePad3;
        public GameInputGamepad GamePad4;
    }
}
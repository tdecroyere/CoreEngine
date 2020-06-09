using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering
{
    public enum LightType
    {
        Point,
        Directional
    }

    public class Light : TrackedItem
    {
        private Vector3 worldPosition;
        private Vector3 color;
        private LightType lightType;

        public Light(Vector3 worldPosition, Vector3 color, LightType lightType)
        {
            this.worldPosition = worldPosition;
            this.color = color;
            this.lightType = lightType;
        }

        public Vector3 WorldPosition 
        { 
            get
            {
                return this.worldPosition;
            } 
            
            set
            {
                UpdateField(ref this.worldPosition, value);
            } 
        }

        public Vector3 Color 
        { 
            get
            {
                return this.color;
            } 
            
            set
            {
                UpdateField(ref this.color, value);
            } 
        }

        public LightType LightType 
        { 
            get
            {
                return this.lightType;
            } 
            
            set
            {
                // TODO: Problems with enums not IEquatable
                this.LightType = value;
            } 
        }
    }
}
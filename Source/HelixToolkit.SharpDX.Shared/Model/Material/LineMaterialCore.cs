﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
using System.Runtime.Serialization;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Model
    {
        using Core;
        [DataContract]
        public sealed class LineMaterialCore : MaterialCore, ILineRenderParams
        {
            #region Properties
            private float thickness = 0.5f;
            /// <summary>
            /// 
            /// </summary>
            public float Thickness
            {
                set
                {
                    Set(ref thickness, value);
                }
                get
                {
                    return thickness;
                }
            }

            private float smoothness;
            /// <summary>
            /// 
            /// </summary>
            public float Smoothness
            {
                set
                {
                    Set(ref smoothness, value);
                }
                get { return smoothness; }
            }

            private Color4 lineColor = Color.Blue;
            /// <summary>
            /// Final Line Color = LineColor * PerVertexLineColor
            /// </summary>
            public Color4 LineColor
            {
                set
                {
                    Set(ref lineColor, value);
                }
                get { return lineColor; }
            }

            private bool enableDistanceFading = false;
            public bool EnableDistanceFading
            {
                set
                {
                    Set(ref enableDistanceFading, value);
                }
                get { return enableDistanceFading; }
            }

            private float fadingNearDistance = 100;
            public float FadingNearDistance
            {
                set { Set(ref fadingNearDistance, value); }
                get { return fadingNearDistance; }
            }

            private float fadingFarDistance = 0;
            public float FadingFarDistance
            {
                set { Set(ref fadingFarDistance, value); }
                get { return fadingFarDistance; }
            }

            private bool fixedSize = true;
            /// <summary>
            /// Gets or sets a value indicating whether [fixed size].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [fixed size]; otherwise, <c>false</c>.
            /// </value>
            public bool FixedSize
            {
                set { Set(ref fixedSize, value); }
                get { return fixedSize; }
            }
            #endregion

            public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
            {
                return new LineMaterialVariable(manager, technique, this);
            }
        }
    }

}

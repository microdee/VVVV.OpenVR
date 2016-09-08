using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;
using Valve.VR;
using VVVV.Utils.VMath;
using SlimDX;
using VVVV.DX11;
using VVVV.PluginInterfaces.V1;

namespace VVVV.Nodes.ValveOpenVR
{
    /* For future reference vux said a proper method is to do direct passing of resources instead of texture copying
     * transcript:
     * "need to implement IDX11RenderWindow, IDX11RendererProvider, and submit in present, not in render"
     */
    [PluginInfo(Name = "Compositor", Category = "OpenVR", Tags = "vr, htc, vive, oculus, rift", Author = "tonfilm, microdee", AutoEvaluate = true)]
    public class VOpenVRCompositorNode : OpenVRConsumerBaseNode, IDX11ResourceDataRetriever
    {
        [Import]
        protected IPluginHost FHost;

        [Input("Texture In")]
        protected Pin<DX11Resource<DX11Texture2D>> FTextureIn;

        [Input("Is Top/Buttom")]
        protected ISpread<bool> FIsOUIn;

        [Input("Colorspace")]
        protected IDiffSpread<EColorSpace> FColorSpace;

        [Output("Texture Pointer")]
        protected ISpread<long> FPointer;

        long SrcPointer = -1;
        Texture_t FTexture;

        //side by side
        VRTextureBounds_t FSBSTexBoundsL = new VRTextureBounds_t() { uMin = 0, uMax = 0.5f, vMin = 0, vMax = 1 };
        VRTextureBounds_t FSBSTexBoundsR = new VRTextureBounds_t() { uMin = 0.5f, uMax = 1, vMin = 0, vMax = 1 };

        //over/under
        VRTextureBounds_t FOUTexBoundsL = new VRTextureBounds_t() { uMin = 0, uMax = 1, vMin = 0, vMax = 0.5f };
        VRTextureBounds_t FOUTexBoundsR = new VRTextureBounds_t() { uMin = 0, uMax = 1, vMin = 0.5f, vMax = 1 };

        public override void Evaluate(int SpreadMax, CVRSystem system)
        {
            if (FTextureIn.IsConnected)
            {
                RenderRequest?.Invoke(this, FHost);
                if(AssignedContext == null) return;
                try
                {
                    if (FTextureIn[0].Contains(AssignedContext))
                    {
                        long currpointer = FTextureIn[0][AssignedContext].Resource.ComPointer.ToInt64();
                        if ((FTextureIn.IsChanged || currpointer != SrcPointer) && currpointer > 0)
                        {
                            FTexture = new Texture_t
                            {
                                handle = new IntPtr(currpointer),
                                eType = EGraphicsAPIConvention.API_DirectX,
                                eColorSpace = FColorSpace[0]
                            };
                            SrcPointer = currpointer;
                        }
                        FPointer[0] = currpointer;

                        if (FColorSpace.IsChanged)
                            FTexture.eColorSpace = FColorSpace[0];

                        //set tex
                        VRTextureBounds_t boundsL;
                        VRTextureBounds_t boundsR;
                        if (FIsOUIn[0])
                        {
                            boundsL = FOUTexBoundsL;
                            boundsR = FOUTexBoundsR;
                        }
                        else
                        {
                            boundsL = FSBSTexBoundsL;
                            boundsR = FSBSTexBoundsR;
                        }

                        var compositor = OpenVR.Compositor;
                        var error = compositor.Submit(EVREye.Eye_Left, ref FTexture, ref boundsL,
                            EVRSubmitFlags.Submit_Default);
                        SetStatus(error);
                        if (error != EVRCompositorError.None) return;
                        error = compositor.Submit(EVREye.Eye_Right, ref FTexture, ref boundsR,
                            EVRSubmitFlags.Submit_Default);
                        SetStatus(error);
                        if (error != EVRCompositorError.None) return;
                    }
                    else FPointer[0] = -2;
                }
                catch (Exception e)
                {
                    SetStatus(e);
                }
            }
        }

        public DX11RenderContext AssignedContext { get; set; }
        public event DX11RenderRequestDelegate RenderRequest;
    }

}

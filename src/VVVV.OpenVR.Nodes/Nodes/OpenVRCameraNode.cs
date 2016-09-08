using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;
using Valve.VR;
using VVVV.Utils.VMath;
using SlimDX;
using SlimDX.Direct3D11;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.ValveOpenVR
{
    [PluginInfo(Name = "Camera", Category = "OpenVR", Tags = "vry, htc, vive, oculus, rift", Author = "tonfilm, microdee")]
    public class OpenVRCameraNode : OpenVRConsumerBaseNode
    {
        [Input("Reinitialize", IsBang = true)]
        ISpread<bool> FInit;

        [Output("View")]
        ISpread<Matrix> FViewOut;

        [Output("Projection")]
        ISpread<Matrix> FProjectionOut;

        [Output("Eye to Head")]
        ISpread<Matrix> FEyeToHeadOut;

        [Output("Vertices Left")]
        ISpread<float> FVerticesLeftOut;

        [Output("Vertices Right")]
        ISpread<float> FVerticesRightOut;

        [Output("Recommended Texture Size")]
        ISpread<Vector2D> FTexSizeOut;

        [Output("HMD Pose", IsSingle = true)]
        ISpread<Matrix> FHMDPoseOut;

        protected Matrix EyeL;
        protected Matrix EyeR;

        protected bool IsInit = true;

        public override void Evaluate(int SpreadMax, CVRSystem system)
        {
            if (IsInit || FInit[0])
            {
                FTexSizeOut[0] = OpenVRManager.RecommendedRenderTargetSize;

                var projL = system.GetProjectionMatrix(EVREye.Eye_Left, 0.05f, 100, EGraphicsAPIConvention.API_DirectX);
                var projR = system.GetProjectionMatrix(EVREye.Eye_Right, 0.05f, 100, EGraphicsAPIConvention.API_DirectX);
                FProjectionOut.SliceCount = 2;
                FProjectionOut[0] = projL.ToProjectionMatrix();
                FProjectionOut[1] = projR.ToProjectionMatrix();

                EyeL = system.GetEyeToHeadTransform(EVREye.Eye_Left).ToEyeMatrix();
                EyeR = system.GetEyeToHeadTransform(EVREye.Eye_Right).ToEyeMatrix();
                FEyeToHeadOut.SliceCount = 2;
                FEyeToHeadOut[0] = EyeL;
                FEyeToHeadOut[1] = EyeR;
                IsInit = false;

                var meshLeft = system.GetHiddenAreaMesh(EVREye.Eye_Left);
                var meshRight = system.GetHiddenAreaMesh(EVREye.Eye_Right);

                //hidden pixels mesh
                if (meshLeft.unTriangleCount > 0 && meshRight.unTriangleCount > 0)
                {
                    GetMeshData(meshLeft, FVerticesLeftOut);
                    GetMeshData(meshRight, FVerticesRightOut);
                }
                else
                {
                    FVerticesLeftOut.SliceCount = 0;
                    FVerticesRightOut.SliceCount = 0;
                }
            }

            //view
            FViewOut.SliceCount = 2;
            FViewOut[0] = Matrix.Invert(EyeL * OpenVRManager.RenderPoses[0].mDeviceToAbsoluteTracking.ToMatrix());
            FViewOut[1] = Matrix.Invert(EyeR * OpenVRManager.RenderPoses[0].mDeviceToAbsoluteTracking.ToMatrix());
            FHMDPoseOut[0] = OpenVRManager.GamePoses[0].mDeviceToAbsoluteTracking.ToMatrix();
        }

        void GetMeshData(HiddenAreaMesh_t meshData, ISpread<float> ret)
        {
            var floatCount = (int)meshData.unTriangleCount * 3 * 2;
            ret.SliceCount = floatCount;

            Marshal.Copy(meshData.pVertexData, ret.Stream.Buffer, 0, floatCount);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using Valve.VR;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.ValveOpenVR
{

    [PluginInfo(
        Name = "Producer",
        Category = "OpenVR",
        Tags = "vr, htc, vive, oculus, rift",
        Author = "microdee, tonfilm",
        AutoEvaluate = true)]
    public class ValveOpenVRServerNode : OpenVRProducerNode, IDisposable
    {

        [Input("Wait for Poses", IsSingle = true)]
        ISpread<bool> FWaitForPoses;

        public override void Evaluate(int SpreadMax, CVRSystem system)
        {
            var renderPoses = OpenVRManager.RenderPoses;
            var gamePoses = OpenVRManager.GamePoses;
            if (FWaitForPoses[0])
            {
                var error = OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
                SetStatus(error);
                if (error != EVRCompositorError.None) return;
            }
            else
            {
                var error = OpenVR.Compositor.GetLastPoses(renderPoses, gamePoses);
                SetStatus(error);
                if (error != EVRCompositorError.None) return;
            }
            OpenVRManager.RenderPoses = renderPoses;
            OpenVRManager.GamePoses = gamePoses;
        }

        public void Dispose()
        {
            OpenVRManager.ShutDownOpenVR();
        }
    }
}

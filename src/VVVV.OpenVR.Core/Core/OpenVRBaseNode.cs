﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.ValveOpenVR
{
    public abstract class OpenVRBaseNode
    {
        [Output("Error", Order = 1000)]
        ISpread<String> FErrorOut;

        public abstract void Evaluate(int SpreadMax, CVRSystem system);

        protected void SetStatus(object toString)
        {
            if (toString is EVRInitError)
                FErrorOut[0] = OpenVR.GetStringForHmdError((EVRInitError)toString);
            else if (toString is EVRCompositorError)
            {
                var error = (EVRCompositorError)toString;

                if (error == EVRCompositorError.TextureIsOnWrongDevice)
                    FErrorOut[0] = "Texture on wrong device. Set your graphics driver to use the same video card for vvvv as the headset is plugged into.";
                else if (error == EVRCompositorError.TextureUsesUnsupportedFormat)
                    FErrorOut[0] = "Unsupported texture format. Make sure texture uses RGBA, is not compressed and has no mipmaps.";
                else
                    FErrorOut[0] = error.ToString();
            }
            else
                FErrorOut[0] = toString.ToString();
        }
    }

    public abstract class OpenVRProducerNode : OpenVRBaseNode, IPluginEvaluate
    {
        [Output("System", IsSingle = true, Order = -100)]
        protected ISpread<CVRSystem> FSystemOut;

        [Input("Init", IsBang = true, IsSingle = true)]
        protected ISpread<bool> FInitIn;

        bool FFirstFrame = true;

        //the vr system
        private CVRSystem FOpenVRSystem;

        public virtual void OnFirstFrame(int SpreadMax, CVRSystem system)
        {
            
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInitIn[0] || FFirstFrame)
            {
                FOpenVRSystem = OpenVRManager.InitOpenVR();
                SetStatus(OpenVRManager.ErrorMessage);
                FSystemOut[0] = FOpenVRSystem;
                OnFirstFrame(SpreadMax, FOpenVRSystem);
            }

            if (FOpenVRSystem != null)
            {
                Evaluate(SpreadMax, FOpenVRSystem);
            }

            FFirstFrame = false;
        }
    }

    public abstract class OpenVRConsumerBaseNode : OpenVRBaseNode, IPluginEvaluate
    {

        [Input("System", IsSingle = true, Order = -100)]
        protected Pin<CVRSystem> FSystemIn;

        //the vr system
        private CVRSystem FOpenVRSystem;

        public void Evaluate(int SpreadMax)
        {
            if (FSystemIn.IsChanged)
            {
                FOpenVRSystem = FSystemIn[0];
            }

            if (FOpenVRSystem != null)
            {
                Evaluate(SpreadMax, FOpenVRSystem);
            }
            else
            {
                SetStatus("OpenVR is not initialized, please connect a Producer (OpenVR) node");
            }
        }
    }


}

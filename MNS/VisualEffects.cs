using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MNS
{
    public static class VisualEffects
    {
        public static void ApplyBlurEffect(Window window)
        {
            System.Windows.Media.Effects.BlurEffect objBlur = new System.Windows.Media.Effects.BlurEffect();
            objBlur.Radius = 8;
            window.Effect = objBlur;
        }

        public static void ClearBlurEffect(Window window)
        {
            window.Effect = null;
        }
    }
}
